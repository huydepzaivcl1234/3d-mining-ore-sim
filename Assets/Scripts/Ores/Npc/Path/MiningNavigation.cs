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
    ///    global shortest route over the real walkable surface, respects ore carving from
    ///    <see cref="MiningNavMeshObstacle"/>, and costs no per-agent bookkeeping.
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
        private static float nextNavigationRecoveryAttempt;
        private static bool navigationRecoveryLogged;

        /// <summary>True when NavMesh path queries are expected to succeed.</summary>
        public static bool NavMeshAvailable =>
            MiningNavMeshBuilder.Instance != null && MiningNavMeshBuilder.Instance.HasNavMesh;

        /// <summary>
        /// Fills resultWaypoints with a route from start to end and reports which backend produced
        /// it. Returns false (and clears the list) when no route exists at all.
        /// </summary>
        /// <param name="sampleRadius">
        /// How far from start/end to search for a point actually on the NavMesh. Miners stand
        /// beside ores, so their stand position can sit slightly inside a carved hole - this lets
        /// the query snap to the nearest legal point instead of failing outright.
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

            if (MiningNavGrid.Instance != null &&
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

            if (MiningNavGrid.Instance != null &&
                MiningNavGrid.Instance.TryFindPath(start, end, resultWaypoints))
            {
                source = MiningPathSource.Grid;
                return true;
            }

            source = MiningPathSource.None;
            return false;
        }

        /// <summary>
        /// Recovers the common scene-authoring mistake where the NavMeshSurface and its builder
        /// exist but were disabled. Without this, every NPC silently falls through to reactive
        /// steering and a commanded target can oscillate between two obstacles indefinitely.
        /// The builder's Start method performs the actual bake on the next frame; callers simply
        /// retry their throttled path request after that.
        /// </summary>
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

            // A partial path stops at the closest reachable point rather than the target. That is
            // still worth walking (it makes real progress, and the miner re-paths on arrival), but
            // an invalid path is not.
            if (sharedPath.status == NavMeshPathStatus.PathInvalid)
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
