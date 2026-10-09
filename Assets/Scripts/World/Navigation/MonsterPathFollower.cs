using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>A* owns the route; short capsule probes only steer within safe ground.</summary>
    public sealed class MonsterPathFollower
    {
        private readonly MushroomMonster owner;
        private readonly CharacterController motor;
        private readonly List<Vector3> route = new();
        private readonly List<Vector3> candidates = new();
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private WorldNavigationGrid grid;
        private NavigationProfile profile;
        private Vector3 requestedGoal, routeOrigin, previousFoot, previousDirection;
        private float clock, nextRequest, pendingSince, blockedSince, sideUntil, nextDirty;
        private int waypoint, generation, candidateIndex, revision = -1, side;
        private bool pending, initialized;
        private Transform victim;
        private MonsterCombatSettings settings;
        public string State { get; private set; } = "Idle";
        public IReadOnlyList<Vector3> Route => route;
        public Vector3 Steering { get; private set; }
        private static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);

        public MonsterPathFollower(MushroomMonster owner, CharacterController motor)
        { this.owner = owner; this.motor = motor; side = (owner.GetHashCode() & 1) == 0 ? 1 : -1; }

        public static NavigationProfile ProfileFor(CharacterController controller)
        {
            Vector3 scale = controller.transform.lossyScale;
            float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float height = Mathf.Max(radius * 2f, controller.height * Mathf.Abs(scale.y));
            Vector3 foot = controller.transform.TransformPoint(controller.center) - Vector3.up * height * .5f;
            return new NavigationProfile(radius, height, foot - controller.transform.position);
        }

        public void Reset()
        {
            generation++;
            grid?.Cancel(owner);
            pending = false; route.Clear(); candidates.Clear(); waypoint = 0;
            revision = -1; nextRequest = 0f; initialized = false;
            blockedSince = 0f; previousDirection = Steering = Vector3.zero; State = "Idle";
        }

        public Vector3 Tick(Vector3 goal, Transform target, float reach, MonsterCombatSettings config, float dt)
        {
            settings = config; victim = target;
            clock += Mathf.Max(0f, dt);
            grid = WorldNavigationGrid.Instance;
            if (grid == null || !grid.isActiveAndEnabled || !grid.HasBaked)
            { State = "Waiting for A* grid"; return Steering = Vector3.zero; }
            profile = ProfileFor(motor);
            Vector3 foot = profile.Foot(owner.transform.position);
            if (!grid.TryGetGroundPoint(foot, out foot))
            { State = "Outside navigable ground"; return Steering = Vector3.zero; }

            if (!initialized) { previousFoot = foot; initialized = true; blockedSince = clock; }
            float progress = Vector3.Dot(Flat(foot - previousFoot), previousDirection);
            if (progress > Mathf.Max(.001f, config.moveSpeed * dt * .1f) || previousDirection.sqrMagnitude < .01f)
                blockedSince = clock;
            previousFoot = foot;
            if (clock - blockedSince >= Mathf.Max(.1f, config.stuckRepathSeconds))
            {
                grid.MarkDirty(new Bounds(foot, Vector3.one * profile.Radius * 4f));
                CancelPending(); route.Clear(); waypoint = 0; nextRequest = clock;
                blockedSince = clock; State = "Repath after stalled movement";
            }
            if (pending && clock - pendingSince >= Mathf.Max(.5f, config.pathRequestTimeout))
            { CancelPending(); nextRequest = clock; State = "Retry timed-out path"; }

            bool movedGoal = Flat(goal - requestedGoal).sqrMagnitude >=
                Mathf.Pow(Mathf.Max(.05f, config.targetRepathDistance), 2);
            if (!pending && clock >= nextRequest &&
                (waypoint >= route.Count || movedGoal || revision != grid.Revision))
                BeginRequest(foot, goal, reach);

            Advance(foot, config.waypointTolerance);
            if (waypoint >= route.Count)
            {
                previousDirection = Vector3.zero;
                if (pending) State = "Waiting for A* path";
                return Steering = Vector3.zero;
            }
            Vector3 direction = Flat(route[waypoint] - foot).normalized;
            float distance = Mathf.Max(profile.Radius, config.avoidanceLookAhead);
            // Shorten near a destination: never step past a reachable attack position.
            distance = Mathf.Min(distance, Mathf.Max(config.waypointTolerance, Flat(route[waypoint] - foot).magnitude));
            Vector3 steered = Avoid(foot, direction, distance);
            float remaining = Flat(route[waypoint] - foot).magnitude;
            // Clamp every corner, not just the destination. At high speed/low FPS
            // overshooting a corner makes the next tick steer back toward it.
            float speedFactor = Mathf.Min(1f, remaining / Mathf.Max(.001f,
                config.moveSpeed * Mathf.Max(dt, .001f)));
            previousDirection = steered;
            return Steering = steered * speedFactor;
        }

        private void CancelPending()
        { generation++; grid?.Cancel(owner); pending = false; }

        private void BeginRequest(Vector3 foot, Vector3 goal, float reach)
        {
            requestedGoal = goal; candidates.Clear(); candidateIndex = 0;
            if (victim == null)
            {
                if (grid.TryProject(goal, grid.StandProjectionRadius, out var projected, profile.Radius, profile.Height))
                    candidates.Add(projected);
            }
            else
            {
                Vector3 toward = Flat(foot - goal).normalized;
                if (toward.sqrMagnitude < .001f) toward = -owner.transform.forward;
                int count = Mathf.Max(4, settings.chaseStandDirections);
                for (int i = 0; i < count; i++)
                {
                    // Alternate clockwise/counterclockwise; nearest approaches are tried first.
                    int ring = (i + 1) / 2;
                    float angle = ring * (i % 2 == 0 ? -1f : 1f) * 360f / count;
                    Vector3 candidate = goal + Quaternion.AngleAxis(angle, Vector3.up) * toward *
                        reach * Mathf.Clamp(settings.chaseStandRangeFraction, .1f, .95f);
                    if (!grid.TryProject(candidate, grid.StandProjectionRadius, out var projected, profile.Radius, profile.Height)) continue;
                    // Arrival tolerance must not leave the actor outside its attack range.
                    float maximumStandDistance = Mathf.Min(reach * settings.chaseStandMaximumRangeFraction,
                        reach - Mathf.Max(.05f, settings.waypointTolerance));
                    if (Flat(projected - goal).magnitude > maximumStandDistance) continue;
                    if (ApproachVisible(projected, goal)) candidates.Add(projected);
                }
            }
            nextRequest = clock + Mathf.Max(.05f, settings.chaseRepathSeconds);
            if (candidates.Count == 0) { State = "No clear approach to target"; return; }
            pending = true; pendingSince = clock; generation++;
            RequestCandidate(generation);
        }

        private void RequestCandidate(int token)
        {
            if (token != generation || !pending || owner == null || !owner.isActiveAndEnabled) return;
            Vector3 foot = profile.Foot(owner.transform.position);
            if (!grid.TryGetGroundPoint(foot, out foot)) { pending = false; return; }
            grid.RequestPath(owner, foot, candidates[candidateIndex], profile, result =>
            {
                if (token != generation || owner == null || !owner.isActiveAndEnabled) return;
                if (!result.Success)
                {
                    if (result.Status != PathStatus.Canceled && ++candidateIndex < candidates.Count)
                    { RequestCandidate(token); return; }
                    pending = false; State = result.Status.ToString(); return;
                }
                pending = false; route.Clear(); route.AddRange(result.Route);
                revision = result.Revision; waypoint = 0;
                routeOrigin = foot; blockedSince = clock;
                // The actor kept moving while A* searched. Skip already passed/visible corners.
                Vector3 current = profile.Foot(owner.transform.position);
                for (int i = 0; i < route.Count; i++)
                    if (i == 0 ? grid.IsSegmentClear(current, route[i], profile.Radius, profile.Height) :
                        grid.IsShortcutClear(current, route[i], profile.Radius, profile.Height)) waypoint = i;
                    else break;
                State = "Following A*";
            });
        }

        private void Advance(Vector3 foot, float tolerance)
        {
            while (waypoint < route.Count)
            {
                Vector3 end = route[waypoint], start = waypoint == 0 ? routeOrigin : route[waypoint - 1];
                Vector3 edge = Flat(end - start), offset = Flat(foot - start);
                float t = edge.sqrMagnitude > .0001f ? Vector3.Dot(offset, edge) / edge.sqrMagnitude : 1f;
                bool passed = t >= 1f && Flat(foot - end).magnitude <= Mathf.Max(tolerance, profile.Radius);
                if (!passed && Flat(foot - end).magnitude > Mathf.Max(.05f, tolerance)) break;
                // Arrival tolerance cannot authorize cutting the inside of a wall corner.
                if (waypoint + 1 < route.Count &&
                    !grid.IsSegmentClear(foot, route[waypoint + 1], profile.Radius, profile.Height)) break;
                waypoint++;
            }
        }

        private Vector3 Avoid(Vector3 foot, Vector3 preferred, float distance)
        {
            bool straight = ClearAhead(foot, preferred, distance, out Collider obstacle);
            if (straight) { State = "Following A*"; return preferred; }
            if (clock >= nextDirty)
            {
                // A moved/new obstacle must invalidate the planner's cached clearance.
                grid.MarkDirty(obstacle != null ? obstacle.bounds :
                    new Bounds(foot + preferred * distance * .5f, Vector3.one * (distance + profile.Radius * 2f)));
                nextDirty = clock + Mathf.Max(.05f, settings.chaseRepathSeconds);
                nextRequest = Mathf.Min(nextRequest, clock);
            }
            for (int pass = 0; pass < 2; pass++)
            {
                int sign = pass == 0 ? side : -side;
                if (pass == 1 && clock < sideUntil) continue;
                for (int i = 1; i <= 3; i++)
                {
                    Vector3 candidate = Quaternion.AngleAxis(sign * i * 30f, Vector3.up) * preferred;
                    if (!ClearAhead(foot, candidate, distance, out _)) continue;
                    side = sign; sideUntil = clock + Mathf.Max(.05f, settings.avoidanceHoldSeconds);
                    State = "Avoiding obstacle"; return candidate;
                }
            }
            State = "Blocked - waiting for new A* route"; return Vector3.zero;
        }

        private bool ClearAhead(Vector3 foot, Vector3 direction, float distance, out Collider obstacle)
        {
            obstacle = null;
            float clearance = .06f;
            Vector3 bottom = foot + Vector3.up * (profile.Radius + clearance);
            Vector3 top = foot + Vector3.up * Mathf.Max(profile.Radius + clearance, profile.Height - profile.Radius);
            int count = Physics.CapsuleCastNonAlloc(bottom, top, profile.Radius, direction, hits,
                distance, settings.navigationObstacleLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i]; var collider = hit.collider;
                if (collider == null || collider is TerrainCollider || collider.transform.IsChildOf(owner.transform) ||
                    (victim != null && collider.transform.IsChildOf(victim)) ||
                    collider.bounds.max.y <= foot.y + clearance) continue;
                if (hit.distance < nearest) { nearest = hit.distance; obstacle = collider; }
            }
            return obstacle == null && grid.IsSegmentClear(foot, foot + direction * distance, profile.Radius, profile.Height);
        }

        public void DrawGizmos()
        {
            if (owner == null) return;
            Gizmos.color = Color.cyan;
            Vector3 previous = owner.transform.position;
            for (int i = waypoint; i < route.Count; i++)
            { Gizmos.DrawLine(previous, route[i]); previous = route[i]; }
            Gizmos.color = Color.green;
            Gizmos.DrawRay(owner.transform.position + Vector3.up * .2f, Steering);
        }
    

        private bool ApproachVisible(Vector3 stand, Vector3 goal)
        {
            Vector3 origin = stand + Vector3.up * (profile.Height * .5f);
            Vector3 delta = goal + Vector3.up * .5f - origin;
            int count = Physics.RaycastNonAlloc(origin, delta.normalized, hits, delta.magnitude,
                settings.navigationObstacleLayers, QueryTriggerInteraction.Ignore);
            var obstructions = count == hits.Length
                ? Physics.RaycastAll(origin, delta.normalized, delta.magnitude,
                    settings.navigationObstacleLayers, QueryTriggerInteraction.Ignore) : hits;
            if (obstructions != hits) count = obstructions.Length;
            for (int i = 0; i < count; i++)
            {
                Transform hit = obstructions[i].transform;
                if (hit != null && hit.GetComponentInParent<MushroomMonster>() != null) continue;
                if (hit != null && !hit.IsChildOf(owner.transform) &&
                    (victim == null || !hit.IsChildOf(victim))) return false;
            }
            return true;
        }
}
}
