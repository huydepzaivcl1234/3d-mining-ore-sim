using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningNpc
    {
        private bool pathPending;
        private bool awaitingCrowdAccess;
        private float accessWaitStarted;
        private int requestGeneration;
        private Component approachTarget;
        private StandReservation standLease;
        private int firstStandCandidate;
        private float requestStarted, bestRemaining = float.PositiveInfinity, resumeAt;
        private Vector3 routeStart;
        private float steeringValidUntil;
        private int steeringWaypoint = -1, steeringRevision = -1, steeringVisibleWaypoint;
        private readonly Dictionary<Component, float> unreachableUntil = new();
        public NavigationState NavigationStatus { get; private set; }
        public string MovementRejection { get; private set; }
        public float PendingPathAge => pathPending ? Mathf.Max(0, Time.time - requestStarted) : 0;
        public int RecoveryAttempts => stuckRepathAttempts;
        public Vector3 PreferredVelocity { get; private set; }
        public Vector3 AcceptedVelocity { get; private set; }
        public NavigationProfile Profile => capsule != null && npcData != null ? NavigationProfile.From(capsule, npcData) : new NavigationProfile(.4f, 1.8f);
        public int NavigationWaitingRings => npcData != null ? npcData.WaitingRings : 4;
        public float ArrivalTolerance => npcData != null ? npcData.StoppingDistance + .025f : .145f;
        internal Vector3 RootPosition => body != null ? body.position : transform.position;
        internal Vector3 HorizontalVelocity => body != null ? Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up) : Vector3.zero;
        internal NpcData NavigationData => npcData;
        internal bool HasMiningApproach => hasMoveTarget && standLease != null && !standLease.IsWaiting;
        internal bool IsParkedForNavigation => IsActivelyMining || (standLease != null && standLease.IsWaiting &&
            Vector3.ProjectOnPlane(standLease.Position - Profile.Foot(RootPosition), Vector3.up).sqrMagnitude <= ArrivalTolerance * ArrivalTolerance);
        internal Vector3 MiningApproachDestination => standLease != null ? standLease.Position : Profile.Foot(RootPosition);
        internal float CrowdSpeedMultiplier => GetMoveSpeedMultiplier();
        internal Vector3 CrowdPreferredVelocity()
        {
            if (!hasMoveTarget || IsStunned || !MiningNavigation.PathfindingAvailable) return Vector3.zero;
            Vector3 offset = GetNavigationTarget(RootPosition, desiredMoveTarget) - RootPosition; offset.y = 0;
            if (offset.sqrMagnitude <= npcData.StoppingDistance * npcData.StoppingDistance) return Vector3.zero;
            float multiplier = GetMoveSpeedMultiplier();
            float braking = npcData.BrakingAcceleration * multiplier * multiplier;
            float speed = Mathf.Min(npcData.MoveSpeed * multiplier,
                Mathf.Sqrt(2 * braking * Mathf.Max(0, RemainingRouteDistance(RootPosition) - npcData.StoppingDistance)));
            return offset.normalized * speed;
        }
        public bool IsNavigationTargetCoolingDown(Component candidate)
        {
            if (candidate == null || !unreachableUntil.TryGetValue(candidate, out float until)) return false;
            if (Time.time < until) return true;
            unreachableUntil.Remove(candidate); return false;
        }
        private void ResetProgressTracking()
        {
            lastProgressPosition = RootPosition; lastProgressTime = Time.time;
            bestRemaining = RemainingRouteDistance(RootPosition);
        }
        private float GetObstacleProbeRadius() => Profile.Radius;
        private void TrackMovementProgress(Vector3 position)
        {
            if (!MiningNavigation.PathfindingAvailable)
            { NavigationStatus = NavigationState.WaitingForGrid; return; }
            if (pathPending && PendingPathAge > npcData.PendingPathTimeout &&
                !MiningNavGrid.Instance.IsRequestMakingProgress(this, npcData.PendingPathTimeout))
            { ResetGlobalPath(); MovementRejection = "Path request timed out; resubmitting"; return; }
            if (currentPath.Count == 0)
            {
                if (awaitingCrowdAccess)
                {
                    NavigationStatus = NavigationState.WaitingForCrowd;
                    if (Time.time - accessWaitStarted < npcData.CrowdAccessTimeout) return;
                    awaitingCrowdAccess = false;
                    RejectStandPoint(); ResetGlobalPath(); ResetProgressTracking(); return;
                }
                NavigationStatus = pathPending ? NavigationState.WaitingForPath : NavigationState.WaitingForSlot;
                if (!pathPending && Time.time - lastProgressTime > npcData.StuckTimeout)
                { firstStandCandidate = 0; MiningStandReservations.Reject(this); lastProgressTime = Time.time; }
                return;
            }
            NavigationStatus = pathPending ? NavigationState.WaitingForPath :
                MiningCrowdCoordinator.IsYielding(this) ? NavigationState.WaitingForCrowd : NavigationState.Moving;
            float remaining = RemainingRouteDistance(position);
            if (remaining + npcData.StuckProgressDistance < bestRemaining)
            {
                bestRemaining = remaining; lastProgressTime = Time.time; stuckRepathAttempts = 0;
                MovementRejection = null; return;
            }
            if (standLease != null && standLease.IsWaiting && remaining <= ArrivalTolerance)
            { NavigationStatus = NavigationState.WaitingForSlot; return; }
            if (MiningCrowdCoordinator.IsYielding(this) && Time.time - lastProgressTime < npcData.StuckTimeout * 4) return;
            if (pathPending || Time.time - lastProgressTime < npcData.StuckTimeout) return;
            NavigationStatus = NavigationState.Recovering;
            if (TryRecoverPlacement()) { ResetGlobalPath(); ResetProgressTracking(); return; }
            if (++stuckRepathAttempts <= maximumStuckRepathAttempts || hasCommandedTarget)
            { RejectStandPoint(); ResetGlobalPath(); ResetProgressTracking(); return; }
            AbandonUnreachableTarget();
        }
        private void RejectStandPoint()
        {
            firstStandCandidate = standLease != null ? standLease.Candidate + 1 : firstStandCandidate + 1;
            MiningStandReservations.Reject(this); standLease = null; approachTarget = null;
        }
        private void AbandonUnreachableTarget()
        {
            if (hasCommandedTarget)
            { RejectStandPoint(); firstStandCandidate = 0; nextRepathTime = Time.time + repathInterval; return; }
            Component failed = targetOre != null ? (Component)targetOre : targetChest != null ? targetChest : targetLuckyBlock;
            if (failed != null) unreachableUntil[failed] = Time.time + npcData.IgnoredTargetDuration;
            NavigationStatus = NavigationState.Unreachable;
            ReleaseTarget(); nextTargetRefreshTime = 0;
        }
        private void UpdateGlobalPath(Vector3 start, Vector3 end)
        {
            var grid = MiningNavGrid.Instance;
            if (!MiningNavigation.PathfindingAvailable || pathPending) return;
            bool moved = !hasPathTarget || (end - lastPathTarget).sqrMagnitude > .0001f;
            if (!moved && pathWaypointIndex < currentPath.Count) return;
            if (Time.time < nextRepathTime) return;
            pathPending = true; requestStarted = Time.time; int generation = ++requestGeneration;
            nextRepathTime = Time.time + repathInterval; lastPathTarget = end; hasPathTarget = true;
            grid.RequestPath(this, Profile.Foot(start), Profile.Foot(end), Profile, result =>
            {
                if (!isActiveAndEnabled || generation != requestGeneration) return;
                pathPending = false;
                if (result.Status == PathStatus.Stale || result.Status == PathStatus.Canceled || result.Status == PathStatus.Unavailable)
                { nextRepathTime = Time.time + repathInterval; return; }
                if (!result.Success)
                {
                    MovementRejection = result.Status.ToString();
                    if (result.Status == PathStatus.Crowded)
                    {
                        if (!awaitingCrowdAccess) { awaitingCrowdAccess = true; accessWaitStarted = Time.time; }
                        MiningStandReservations.YieldBlockingWorker(this, Profile.Foot(RootPosition), result.Route, Profile);
                        NavigationStatus = NavigationState.WaitingForCrowd;
                        nextRepathTime = Time.time + npcData.RouteValidationInterval;
                        return;
                    }
                    awaitingCrowdAccess = false;
                    if (result.Status == PathStatus.InvalidStart) { NavigationStatus = NavigationState.Recovering; TryRecoverPlacement(); }
                    else
                    {
                        bool wasWaiting = standLease != null && standLease.IsWaiting;
                        RejectStandPoint();
                        int limit = grid.StandProbeDirections * (wasWaiting ? 1 + NavigationWaitingRings * 2 : 1) + (wasWaiting ? 1 : 0);
                        if (firstStandCandidate >= limit) AbandonUnreachableTarget();
                    }
                    return;
                }
                int first = -1;
                for (int i = result.Route.Count - 1; i >= 0; i--)
                    if (grid.IsMinerCorridorClear(this, Profile.Foot(RootPosition), result.Route[i], Profile)) { first = i; break; }
                if (first < 0) { nextRepathTime = Time.time + repathInterval; return; }
                currentPath.Clear();
                awaitingCrowdAccess = false;
                for (int i = first; i < result.Route.Count; i++) currentPath.Add(Profile.Root(result.Route[i]));
                pathWaypointIndex = 0; routeStart = RootPosition;
                steeringWaypoint = -1;
                currentPathSource = MiningPathSource.Grid;
                bestRemaining = RemainingRouteDistance(RootPosition);
            });
        }
        private Vector3 GetNavigationTarget(Vector3 position, Vector3 fallback)
        {
            if (!MiningNavigation.PathfindingAvailable || pathWaypointIndex >= currentPath.Count) return position;
            float reach = Mathf.Max(.08f, ArrivalTolerance);
            while (pathWaypointIndex < currentPath.Count)
            {
                Vector3 corner = currentPath[pathWaypointIndex];
                Vector3 previous = pathWaypointIndex == 0 ? routeStart : currentPath[pathWaypointIndex - 1];
                Vector3 edge = Vector3.ProjectOnPlane(corner - previous, Vector3.up);
                Vector3 delta = Vector3.ProjectOnPlane(corner - position, Vector3.up);
                Vector3 lateral = Vector3.ProjectOnPlane(position - previous, Vector3.up);
                lateral -= Vector3.Project(lateral, edge);
                bool passed = pathWaypointIndex < currentPath.Count - 1 && edge.sqrMagnitude > .0001f &&
                    Vector3.Dot(position - corner, edge) >= 0 && lateral.sqrMagnitude <= Profile.Radius * Profile.Radius;
                if (delta.sqrMagnitude > reach * reach && !passed) break;
                pathWaypointIndex++;
            }
            if (pathWaypointIndex >= currentPath.Count) return position;
            var grid = MiningNavGrid.Instance;
            bool cached = steeringWaypoint == pathWaypointIndex && steeringRevision == grid.Revision && Time.time < steeringValidUntil;
            Vector3 next = currentPath[pathWaypointIndex];
            if (!cached && !grid.IsMinerCorridorClear(this, Profile.Foot(position), Profile.Foot(next), Profile))
            {
                MovementRejection = "Route corridor blocked";
                currentPath.Clear(); currentPathSource = MiningPathSource.None; nextRepathTime = Time.time + repathInterval;
                TryRecoverPlacement(); return position;
            }
            float remaining = pathLookAheadDistance;
            Vector3 segmentStart = position;
            int visible = pathWaypointIndex;
            for (int i = pathWaypointIndex; i < currentPath.Count; i++)
            {
                if (cached && i > steeringVisibleWaypoint) break;
                if (!cached && i > pathWaypointIndex &&
                    !grid.IsMinerCorridorClear(this, Profile.Foot(position), Profile.Foot(currentPath[i]), Profile)) break;
                Vector3 delta = currentPath[i] - segmentStart; delta.y = 0;
                Vector3 look = delta.magnitude > remaining ? segmentStart + delta.normalized * remaining : currentPath[i];
                visible = i;
                next = look; remaining -= delta.magnitude;
                if (remaining <= 0) break;
                segmentStart = currentPath[i];
            }
            if (!cached)
            {
                // Cache corridor visibility, never a stationary point ahead of
                // the actor. Look-ahead still follows its current position each tick.
                steeringVisibleWaypoint = visible;
                steeringWaypoint = pathWaypointIndex; steeringRevision = grid.Revision;
                steeringValidUntil = Time.time + npcData.RouteValidationInterval;
            }
            return next;
        }
        public float RemainingRouteDistance(Vector3 position)
        {
            float distance = 0;
            for (int i = pathWaypointIndex; i < currentPath.Count; i++)
            { distance += Vector3.ProjectOnPlane(currentPath[i] - position, Vector3.up).magnitude; position = currentPath[i]; }
            return distance;
        }
        private bool TryRecoverPlacement()
        {
            var grid = MiningNavGrid.Instance;
            if (!MiningNavigation.PathfindingAvailable || grid.IsCapsulePlacementClear(Profile.Foot(RootPosition), Profile.Radius, Profile.Height)) return false;
            if (MiningPlacement.TryEscape(capsule, RootPosition, Profile, npcData.RecoveryRadius, out Vector3 safe))
            { body.position = safe; StopHorizontalMovement(); return true; }
            MovementRejection = "Invalid capsule placement; no safe escape connector";
            return false;
        }
        private float GetMoveSpeedMultiplier() => Mathf.Max(.01f, progressionSystem != null
            ? progressionSystem.CurrentMoveSpeedMultiplier : oreSpawner != null && oreSpawner.UpgradeSystem != null
                ? oreSpawner.UpgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed) : 1);
        private void ResetGlobalPath()
        {
            MiningNavGrid.Instance?.Cancel(this); requestGeneration++; pathPending = false;
            awaitingCrowdAccess = false;
            currentPath.Clear(); pathWaypointIndex = 0; hasPathTarget = false; nextRepathTime = 0;
            steeringWaypoint = -1;
            bestRemaining = float.PositiveInfinity; inFinalApproach = false; currentPathSource = MiningPathSource.None;
        }
    }
}
