using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningNpc
    {
        private bool pathPending;
        private int routeRevision, requestGeneration;
        private Component approachTarget;
        private readonly System.Collections.Generic.Dictionary<Component, float> unreachableUntil = new();
        public bool IsNavigationTargetCoolingDown(Component candidate)
        {
            if (candidate == null || !unreachableUntil.TryGetValue(candidate, out float until)) return false;
            if (Time.time < until) return true;
            unreachableUntil.Remove(candidate); return false;
        }
        private void ResetProgressTracking()
        {
            lastProgressPosition = body != null ? body.position : transform.position; lastProgressTime = Time.time;
        }
        private float GetObstacleProbeRadius() => Mathf.Max(npcData.ObstacleProbeRadius,
            npcData.ColliderRadius + npcData.StandSlotSpacingPadding);
        private void TrackMovementProgress(Vector3 position)
        {
            if (!MiningNavigation.PathfindingAvailable || pathPending) { ResetProgressTracking(); return; }
            Vector3 delta = position - lastProgressPosition; delta.y = 0;
            if (delta.sqrMagnitude >= npcData.StuckProgressDistance * npcData.StuckProgressDistance)
            { lastProgressPosition = position; lastProgressTime = Time.time; return; }
            if (Time.time - lastProgressTime < npcData.StuckTimeout) return;
            if (++stuckRepathAttempts <= maximumStuckRepathAttempts || hasCommandedTarget)
            { approachTarget = null; ResetGlobalPath(); ResetProgressTracking(); return; }
            AbandonUnreachableTarget();
        }
        private void AbandonUnreachableTarget()
        {
            if (hasCommandedTarget)
            { approachTarget = null; nextRepathTime = Time.time + repathInterval; ResetProgressTracking(); return; }
            Component failed = targetOre != null ? (Component)targetOre : targetChest != null ? targetChest : targetLuckyBlock;
            if (failed != null) unreachableUntil[failed] = Time.time + npcData.IgnoredTargetDuration;
            if (targetOre != null) { ignoredOre = targetOre; ignoredOreUntil = Time.time + npcData.IgnoredTargetDuration; }
            if (targetLuckyBlock != null) { ignoredLuckyBlock = targetLuckyBlock; ignoredLuckyBlockUntil = Time.time + npcData.IgnoredTargetDuration; }
            if (targetChest != null) { ignoredChest = targetChest; ignoredChestUntil = Time.time + npcData.IgnoredTargetDuration; }
            ReleaseTarget(); nextTargetRefreshTime = 0; ResetProgressTracking();
        }
        private void UpdateGlobalPath(Vector3 start, Vector3 end)
        {
            var grid = MiningNavGrid.Instance; if (grid == null || !grid.HasBaked || pathPending) return;
            grid.EnsureClearance(NavigationRadius);
            bool moved = !hasPathTarget || (end - lastPathTarget).sqrMagnitude > repathTargetMoveThreshold * repathTargetMoveThreshold;
            bool stale = routeRevision != grid.Revision;
            if (!moved && !stale && pathWaypointIndex < currentPath.Count) return;
            if (!moved && Time.time < nextRepathTime) return;
            pathPending = true; int generation = ++requestGeneration;
            nextRepathTime = Time.time + repathInterval; lastPathTarget = end; hasPathTarget = true;
            grid.RequestPath(this, start, end, (success, route) =>
            {
                if (!isActiveAndEnabled || generation != requestGeneration) return;
                pathPending = false; currentPath.Clear(); pathWaypointIndex = 0;
                currentPathSource = success ? MiningPathSource.Grid : MiningPathSource.None;
                if (success) { currentPath.AddRange(route); routeRevision = grid.Revision; }
                else AbandonUnreachableTarget();
            });
        }
        private Vector3 GetNavigationTarget(Vector3 position, Vector3 fallback)
        {
            if (!MiningNavigation.PathfindingAvailable) return position;
            float reach = Mathf.Max(.08f, npcData.StoppingDistance);
            while (pathWaypointIndex < currentPath.Count)
            {
                Vector3 delta = currentPath[pathWaypointIndex] - position; delta.y = 0;
                if (delta.sqrMagnitude > reach * reach) break;
                pathWaypointIndex++;
            }
            if (pathWaypointIndex >= currentPath.Count) return position;
            Vector3 next = currentPath[pathWaypointIndex];
            float remaining = pathLookAheadDistance;
            Vector3 segmentStart = position;
            for (int i = pathWaypointIndex; i < currentPath.Count; i++)
            {
                Vector3 delta = currentPath[i] - segmentStart; delta.y = 0;
                Vector3 look = delta.magnitude > remaining ? segmentStart + delta.normalized * remaining : currentPath[i];
                if (!MiningNavigation.IsMineableSegmentClear(position, look, NavigationRadius)) break;
                next = look; remaining -= delta.magnitude; if (remaining <= 0) break;
                segmentStart = currentPath[i];
            }
            if (MiningNavigation.IsMineableSegmentClear(position, next, NavigationRadius)) return next;
            currentPath.Clear(); currentPathSource = MiningPathSource.None; nextRepathTime = 0;
            approachTarget = null; return position;
        }
        private float GetMoveSpeedMultiplier() => Mathf.Max(.01f, progressionSystem != null
            ? progressionSystem.CurrentMoveSpeedMultiplier : oreSpawner != null && oreSpawner.UpgradeSystem != null
                ? oreSpawner.UpgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed) : 1);
        private float GetCornerSpeedLimit(Vector3 position, float speed, float braking)
        {
            if (pathWaypointIndex >= currentPath.Count - 1) return speed;
            Vector3 approach = currentPath[pathWaypointIndex] - position;
            Vector3 exit = currentPath[pathWaypointIndex + 1] - currentPath[pathWaypointIndex];
            approach.y = exit.y = 0;
            float turn = Mathf.InverseLerp(15, 150, Vector3.Angle(approach, exit));
            float turnSpeed = Mathf.Lerp(speed, speed * .2f, turn);
            return Mathf.Sqrt(turnSpeed * turnSpeed + 2 * braking * Mathf.Max(0, approach.magnitude - npcData.ColliderRadius));
        }
        private void ResetGlobalPath()
        {
            MiningNavGrid.Instance?.Cancel(this); requestGeneration++; pathPending = false;
            currentPath.Clear(); pathWaypointIndex = 0; hasPathTarget = false; nextRepathTime = 0;
            inFinalApproach = false; currentPathSource = MiningPathSource.None;
        }
    }
}
