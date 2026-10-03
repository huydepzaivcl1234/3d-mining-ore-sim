using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace MiningSimulator.Ores
{
    /// <summary>How a given path was produced. Useful for debugging and for the setup validator.</summary>
    public enum MiningPathSource
    {
        None,
        NavMesh,
        Grid,
    }

    /// <summary>
    /// Single entry point every miner uses to ask for a route. It tries, in order:
    ///
    /// 1. Unity NavMesh (<see cref="NavMesh.CalculatePath"/>) - the preferred path. Gives a true
    ///    global shortest route over the real walkable surface and routes around the compact
    ///    circular carve owned by each mineable. Ore geometry itself is excluded from the bake.
    /// 2. <see cref="MiningNavGrid"/> A* - used when no NavMesh exists in the scene, or when the
    ///    NavMesh query fails (agent off-mesh, target in a disconnected region).
    /// 3. Nothing - the caller falls back to its own direct-line reactive steering.
    ///
    /// Query results are deliberately NOT cached here: throttling repath frequency is the caller's
    /// job (see MiningNpc.repathInterval), because how stale a route may be depends on what the
    /// miner is doing, not on the navigation backend.
    /// </summary>
    public static class MiningNavigation
    {
        // NavMeshPath allocates internally, so one shared instance is reused for every query.
        // Safe because all queries run on the main thread and the corners are copied out
        // immediately before the next query can overwrite them.
        private static NavMeshPath sharedPath;
        private static Vector3[] cornerBuffer = new Vector3[64];
        private static readonly List<Vector3> DistancePathBuffer = new(16);
        private static float nextNavigationRecoveryAttempt;
        private static bool navigationRecoveryLogged;
        private static readonly RaycastHit[] ClearanceHits = new RaycastHit[64];
        private static readonly Collider[] ClearanceOverlaps = new Collider[64];

        /// <summary>Ore/chest clearance only; miners still pass through monsters.</summary>
        public static bool IsMineableSegmentClear(Vector3 start, Vector3 end, float radius, Component ignored = null)
        {
            Vector3 delta = Vector3.ProjectOnPlane(end - start, Vector3.up);
            if (delta.sqrMagnitude < .0001f) return true;
            Vector3 origin = start + Vector3.up * (radius + .12f);
            int count = Physics.SphereCastNonAlloc(origin, radius, delta.normalized,
                ClearanceHits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == ClearanceHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (BlocksMineable(ClearanceHits[i].collider, ignored)) return false;
            count = Physics.OverlapSphereNonAlloc(end + Vector3.up * (radius + .12f),
                radius, ClearanceOverlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == ClearanceOverlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (BlocksMineable(ClearanceOverlaps[i], ignored)) return false;
            return true;
        }

        private static bool BlocksMineable(Collider collider, Component ignored)
        {
            if (collider == null) return false;
            Ore ore = collider.GetComponentInParent<Ore>();
            if (ore != null) return ore != ignored && !ore.IsDepleted;
            LuckyBlock block = collider.GetComponentInParent<LuckyBlock>();
            if (block != null) return block != ignored && !block.IsResolved;
            MiningChest chest = collider.GetComponentInParent<MiningChest>();
            return chest != null && chest != ignored && chest.CanMine;
        }

        /// <summary>Find a free side, not a destination inside the ore's carve.</summary>
        public static bool TryGetOreApproach(Ore ore, Vector3 start, float radius,
            out Vector3 point, out float distance)
        {
            point = ore.GetClosestSurfacePoint(start);
            distance = float.PositiveInfinity;
            if (!ore.TryGetWorldBounds(out Bounds bounds)) return false;
            Vector3 nearSide = Vector3.ProjectOnPlane(start - bounds.center, Vector3.up).normalized;
            if (nearSide.sqrMagnitude < .001f) nearSide = Vector3.forward;
            float standRadius = Mathf.Max(bounds.extents.x, bounds.extents.z) + radius + .15f;
            for (int step = 0; step < 8; step++)
            {
                int turn = step == 0 ? 0 : ((step + 1) / 2) * (step % 2 == 1 ? 1 : -1);
                Vector3 direction = Quaternion.AngleAxis(turn * 45f, Vector3.up) * nearSide;
                Vector3 candidate = bounds.center + direction * standRadius;
                candidate.y = start.y;
                if (!IsMineableSegmentClear(candidate, candidate + direction * .05f, radius)) continue;
                if (PathfindingAvailable && !TryGetPathDistance(start, candidate, out distance)) continue;
                if (!PathfindingAvailable) distance = Vector3.Distance(start, candidate);
                point = candidate;
                return true;
            }
            return false;
        }

        /// <summary>True when NavMesh path queries are expected to succeed.</summary>
        public static bool NavMeshAvailable =>
            MiningNavMeshBuilder.Instance != null && MiningNavMeshBuilder.Instance.HasNavMesh;

        /// <summary>True when ore selection can compare complete routed path costs.</summary>
        public static bool PathfindingAvailable => NavMeshAvailable ||
            (MiningNavGrid.Instance != null && MiningNavGrid.Instance.HasBaked);

        /// <summary>
        /// Returns the length of a complete global route without allocating a waypoint list.
        /// Ore selection uses this cost so "closest" means shortest reachable path rather than
        /// shortest straight line through intervening rocks.
        /// </summary>
        public static bool TryGetPathDistance(Vector3 start, Vector3 end, out float distance,
            int areaMask = NavMesh.AllAreas, float sampleRadius = 2f)
        {
            distance = float.PositiveInfinity;
            if (!TryFindPath(start, end, DistancePathBuffer, areaMask, sampleRadius))
            {
                return false;
            }

            distance = 0f;
            Vector3 previous = start;
            for (int index = 0; index < DistancePathBuffer.Count; index++)
            {
                Vector3 waypoint = DistancePathBuffer[index];
                distance += Vector3.Distance(previous, waypoint);
                previous = waypoint;
            }

            return true;
        }

        /// <summary>
        /// Fills resultWaypoints with a route from start to end and reports which backend produced
        /// it. Returns false (and clears the list) when no route exists at all.
        /// </summary>
        /// <param name="sampleRadius">
        /// How far from start/end to search for a point actually on the NavMesh. Miners stand
        /// beside ores, so their stand position can sit close to a NavMesh edge - this lets the
        /// query snap to the nearest legal point instead of failing outright.
        /// </param>
        public static bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> resultWaypoints,
            int areaMask = NavMesh.AllAreas, float sampleRadius = 2f)
        {
            resultWaypoints.Clear();
            TryRecoverDisabledNavMeshBuilder();

            if (TryFindNavMeshPath(start, end, resultWaypoints, areaMask, sampleRadius))
            {
                return true;
            }

            // Once a baked NavMesh is available it is authoritative. Falling back to the grid
            // after an incomplete NavMesh route sends miners through unbaked space and can be
            // longer than a connected baked route around the ore field.
            if (!NavMeshAvailable && MiningNavGrid.Instance != null &&
                MiningNavGrid.Instance.TryFindPath(start, end, resultWaypoints))
            {
                return true;
            }

            return false;
        }

        /// <summary>Same as <see cref="TryFindPath"/> but also reports the backend used.</summary>
        public static bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> resultWaypoints,
            out MiningPathSource source, int areaMask = NavMesh.AllAreas, float sampleRadius = 2f)
        {
            resultWaypoints.Clear();
            TryRecoverDisabledNavMeshBuilder();

            if (TryFindNavMeshPath(start, end, resultWaypoints, areaMask, sampleRadius))
            {
                source = MiningPathSource.NavMesh;
                return true;
            }

            // Keep the route source consistent: never replace a baked NavMesh with a grid path.
            if (!NavMeshAvailable && MiningNavGrid.Instance != null &&
                MiningNavGrid.Instance.TryFindPath(start, end, resultWaypoints))
            {
                source = MiningPathSource.Grid;
                return true;
            }

            source = MiningPathSource.None;
            return false;
        }

        private static void TryRecoverDisabledNavMeshBuilder()
        {
            if (NavMeshAvailable || Time.unscaledTime < nextNavigationRecoveryAttempt)
            {
                return;
            }

            nextNavigationRecoveryAttempt = Time.unscaledTime + 1f;
            MiningNavMeshBuilder builder = Object.FindFirstObjectByType<MiningNavMeshBuilder>(
                FindObjectsInactive.Include);
            if (builder == null || !builder.gameObject.activeInHierarchy)
            {
                return;
            }

            NavMeshSurface surface = builder.GetComponent<NavMeshSurface>();
            bool enabledAnything = false;
            if (surface != null && !surface.enabled)
            {
                surface.enabled = true;
                enabledAnything = true;
            }

            if (!builder.enabled)
            {
                builder.enabled = true;
                enabledAnything = true;
            }

            if (enabledAnything && !navigationRecoveryLogged)
            {
                navigationRecoveryLogged = true;
                Debug.LogWarning(
                    "[MiningNavigation] Enabled the scene NavMesh builder and surface so miners " +
                    "can use global paths instead of reactive obstacle steering.", builder);
            }
        }

        private static bool TryFindNavMeshPath(Vector3 start, Vector3 end,
            List<Vector3> resultWaypoints, int areaMask, float sampleRadius)
        {
            if (!NavMeshAvailable)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(start, out NavMeshHit startHit, sampleRadius, areaMask) ||
                !NavMesh.SamplePosition(end, out NavMeshHit endHit, sampleRadius, areaMask))
            {
                return false;
            }

            sharedPath ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(startHit.position, endHit.position, areaMask, sharedPath))
            {
                return false;
            }

            // A partial path stops at the closest reachable point rather than the target. Treat
            // it as no route: otherwise the miner follows that dead end, then switches to the
            // unbaked grid/direct steering instead of selecting the complete baked route.
            if (sharedPath.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            int cornerCount = sharedPath.GetCornersNonAlloc(cornerBuffer);

            // GetCornersNonAlloc silently truncates when the buffer is too small. If it filled the
            // buffer completely the route may have been cut short, so grow and retry once.
            if (cornerCount == cornerBuffer.Length)
            {
                cornerBuffer = new Vector3[cornerBuffer.Length * 2];
                cornerCount = sharedPath.GetCornersNonAlloc(cornerBuffer);
            }

            if (cornerCount == 0)
            {
                return false;
            }

            // Corner 0 is the agent's own position - steering toward it would mean steering at
            // where the miner already stands, so it is skipped.
            for (int index = 1; index < cornerCount; index++)
            {
                resultWaypoints.Add(cornerBuffer[index]);
            }

            if (resultWaypoints.Count == 0)
            {
                // Start and end resolved to the same corner: already there, but hand back the
                // real target so the caller still closes the final sub-corner gap.
                resultWaypoints.Add(end);
            }

            return true;
        }
    }
}
