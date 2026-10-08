using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MiningSimulator.Ores
{
    // NavMesh value retained for existing consumers/serialized debug output.
    public enum MiningPathSource { None, NavMesh, Grid }
    public static class MiningNavigation
    {
        private static readonly List<Vector3> distancePath = new();
        public static bool NavMeshAvailable => false;
        public static bool PathfindingAvailable => MiningNavGrid.Instance != null && MiningNavGrid.Instance.isActiveAndEnabled && MiningNavGrid.Instance.HasBaked;
        public static bool IsMineableSegmentClear(Vector3 start, Vector3 end, float radius, Component ignored = null) =>
            PathfindingAvailable && MiningNavGrid.Instance.IsSegmentClear(start, end, radius);
        public static bool TryGetOreApproach(Ore ore, Vector3 start, float radius, out Vector3 point, out float distance,
            float miningRange = float.PositiveInfinity)
        {
            point = start; distance = float.PositiveInfinity;
            if (ore == null || !ore.TryGetWorldBounds(out Bounds bounds) ||
                !TryGetApproach(bounds, start, radius, miningRange, out point,
                    ore.SqrDistanceToSurface, ore.GetClosestSurfacePoint)) return false;
            // Selection is cheap; reachability is resolved by the queued search, not N searches per ore.
            distance = Vector3.Distance(start, point); return true;
        }
        public static bool TryGetApproach(Bounds bounds, Vector3 start, float radius, float miningRange, out Vector3 point,
            System.Func<Vector3, float> surfaceDistance = null, System.Func<Vector3, Vector3> closestSurface = null)
        {
            point = start;
            var grid = MiningNavGrid.Instance; if (grid == null || !grid.HasBaked) return false;
            Vector3 near = Vector3.ProjectOnPlane(start - bounds.center, Vector3.up).normalized;
            if (near.sqrMagnitude < .001f) near = Vector3.forward;
            // Probe around the actual bounds, not the ore's pivot. Keep the entire capsule outside.
            for (int n = 0; n < grid.StandProbeDirections; n++)
            {
                int turn = n == 0 ? 0 : ((n + 1) / 2) * (n % 2 == 1 ? 1 : -1);
                Vector3 direction = Quaternion.AngleAxis(turn * 360f / grid.StandProbeDirections, Vector3.up) * near;
                Vector3 surface = bounds.ClosestPoint(bounds.center + direction * (bounds.size.magnitude + 1));
                Vector3 candidate = surface + direction * (radius + grid.StandPadding); candidate.y = start.y;
                if (!grid.TryProject(candidate, grid.StandProjectionRadius, out Vector3 projected, radius)) continue;
                if (ValidStandPoint(projected)) { point = projected; return true; }
                // Keep the grid coarse, but finish at a continuous point inside the real
                // collider's mining reach. AABB corners are not the ore surface.
                Vector3 closest = closestSurface != null ? closestSurface(projected) : bounds.ClosestPoint(projected);
                Vector3 away = Vector3.ProjectOnPlane(projected - closest, Vector3.up).normalized;
                if (away.sqrMagnitude < .0001f) away = direction;
                float offset = Mathf.Min(radius + grid.StandPadding, miningRange);
                Vector3 endpoint = closest + away * offset;
                if (!grid.TryGetGroundPoint(endpoint, out endpoint) || !ValidStandPoint(endpoint) ||
                    !grid.IsSegmentClear(projected, endpoint, radius)) continue;
                point = endpoint; return true;
            }
            return false;
            bool ValidStandPoint(Vector3 position)
            {
                Vector3 gap = Vector3.ProjectOnPlane(position - bounds.ClosestPoint(position), Vector3.up);
                float distance = surfaceDistance != null ? surfaceDistance(position) : gap.sqrMagnitude;
                return distance <= miningRange * miningRange + .0001f && grid.IsPointClear(position, radius);
            }
        }
        public static bool TryGetPathDistance(Vector3 start, Vector3 end, out float distance,
            int areaMask = NavMesh.AllAreas, float sampleRadius = 2)
        {
            distance = float.PositiveInfinity;
            if (!TryFindPath(start, end, distancePath, areaMask, sampleRadius)) return false;
            distance = 0; foreach (Vector3 waypoint in distancePath) { distance += Vector3.Distance(start, waypoint); start = waypoint; }
            return true;
        }
        public static bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> result,
            int areaMask = NavMesh.AllAreas, float sampleRadius = 2)
        {
            result.Clear(); return PathfindingAvailable && MiningNavGrid.Instance.TryFindPath(start, end, result);
        }
        public static bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> result, out MiningPathSource source,
            int areaMask = NavMesh.AllAreas, float sampleRadius = 2)
        {
            bool found = TryFindPath(start, end, result, areaMask, sampleRadius);
            source = found ? MiningPathSource.Grid : MiningPathSource.None; return found;
        }
    }
}
