using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed class StandReservation
    {
        public MiningNpc Owner { get; internal set; }
        public Component Target { get; internal set; }
        public Vector3 Position { get; internal set; }
        public bool IsWaiting { get; internal set; }
        public int Candidate { get; internal set; }
        internal int order;
        internal Bounds targetBounds;
    }

    /// <summary>Shared space ownership across ores, chests and blocks. A lease never orbits with its owner.</summary>
    public static class MiningStandReservations
    {
        private static readonly Dictionary<MiningNpc, StandReservation> leases = new();
        private static readonly Dictionary<MiningNpc, int> orders = new();
        private static readonly Dictionary<MiningNpc, Vector3> approachDirections = new();
        private static readonly Dictionary<MiningNpc, float> yieldUntil = new(), nextYield = new();
        private struct YieldPassage
        {
            public MiningNpc requester;
            public Vector3 from, to, destination, occupiedPoint;
            public float radius, height;
        }
        private static readonly Dictionary<MiningNpc, YieldPassage> yieldPassages = new();
        private static readonly List<MiningNpc> expired = new();
        private static int nextOrder;
        private static readonly Queue<MiningNpc> allocationQueue = new();
        private static readonly HashSet<MiningNpc> queued = new();
        private static int allocationFrame = -1, allocations;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { leases.Clear(); orders.Clear(); approachDirections.Clear(); yieldUntil.Clear(); nextYield.Clear(); yieldPassages.Clear(); nextOrder = 0; allocationQueue.Clear(); queued.Clear(); allocationFrame = -1; allocations = 0; }
        public static void Release(MiningNpc owner)
        { leases.Remove(owner); orders.Remove(owner); approachDirections.Remove(owner); yieldUntil.Remove(owner); nextYield.Remove(owner); yieldPassages.Remove(owner); queued.Remove(owner); }
        public static void Reject(MiningNpc owner) => leases.Remove(owner);
        public static bool YieldBlockingWorker(MiningNpc requester, Vector3 start, List<Vector3> route, NavigationProfile profile)
        {
            foreach (Vector3 end in route)
            {
                Vector3 segment = Vector3.ProjectOnPlane(end - start, Vector3.up);
                MiningNpc blocker = null; float nearest = float.PositiveInfinity;
                foreach (var pair in leases)
                {
                    var miner = pair.Key;
                    if (miner == null || miner == requester || !miner.isActiveAndEnabled || !miner.IsParkedForNavigation ||
                        (nextYield.TryGetValue(miner, out float next) && Time.time < next)) continue;
                    Vector3 offset = miner.Profile.Foot(miner.RootPosition) - start;
                    if (Mathf.Abs(offset.y) > profile.Height) continue;
                    offset.y = 0;
                    float t = segment.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0;
                    float radius = profile.Radius + miner.NavigationRadius;
                    if ((offset - segment * t).sqrMagnitude < radius * radius && t < nearest)
                    { nearest = t; blocker = miner; }
                }
                if (blocker != null)
                {
                    float duration = blocker.NavigationData.CrowdYieldDuration;
                    yieldUntil[blocker] = Time.time + duration;
                    nextYield[blocker] = Time.time + duration * 2;
                    yieldPassages[blocker] = new YieldPassage { requester = requester, from = start, to = end,
                        destination = route[route.Count - 1], occupiedPoint = blocker.Profile.Foot(blocker.RootPosition),
                        radius = profile.Radius + blocker.NavigationRadius + blocker.ArrivalTolerance, height = profile.Height };
                    leases.Remove(blocker); // Reallocate through the normal safe holding queue.
                    return true;
                }
                start = end;
            }
            return false;
        }
        private static bool AccessPending(YieldPassage passage)
        {
            var requester = passage.requester;
            if (requester == null || !requester.isActiveAndEnabled || requester.IsDead || requester.IsParkedForNavigation ||
                Vector3.ProjectOnPlane(requester.MiningApproachDestination - passage.destination, Vector3.up).sqrMagnitude >
                requester.ArrivalTolerance * requester.ArrivalTolerance) return false;
            Vector3 position = requester.Profile.Foot(requester.RootPosition);
            Vector3 direction = Vector3.ProjectOnPlane(passage.to - passage.from, Vector3.up);
            Vector3 gap = Vector3.ProjectOnPlane(position - passage.occupiedPoint, Vector3.up);
            // A timer alone must not send the blocker back before the requester
            // has passed. Changing/abandoning the destination releases the hold.
            return Vector3.Dot(gap, direction) < 0 || gap.sqrMagnitude < passage.radius * passage.radius;
        }
        private static bool MayAllocate(MiningNpc owner, int budget)
        {
            if (allocationFrame != Time.frameCount) { allocationFrame = Time.frameCount; allocations = 0; }
            if (queued.Add(owner)) allocationQueue.Enqueue(owner);
            while (allocationQueue.Count > 0 && (allocationQueue.Peek() == null ||
                !allocationQueue.Peek().isActiveAndEnabled || !queued.Contains(allocationQueue.Peek())))
            { var expired = allocationQueue.Dequeue(); queued.Remove(expired); }
            if (allocations >= budget || allocationQueue.Count == 0 || allocationQueue.Peek() != owner) return false;
            allocationQueue.Dequeue(); queued.Remove(owner); allocations++; return true;
        }
        public static bool SpaceFree(Vector3 point, float radius, MiningNpc owner = null)
        {
            foreach (var pair in leases)
            {
                if (pair.Key == null || pair.Key == owner || !pair.Key.isActiveAndEnabled) continue;
                Vector3 delta = point - pair.Value.Position; delta.y = 0;
                float distance = radius + pair.Key.NavigationRadius + pair.Key.ArrivalTolerance +
                    (owner != null ? owner.ArrivalTolerance : 0);
                if (delta.sqrMagnitude < distance * distance) return false;
            }
            return true;
        }
        private static bool HoldingSpaceFree(Vector3 point, NavigationProfile profile, MiningNpc owner)
        {
            if (yieldPassages.TryGetValue(owner, out var passage) && Mathf.Abs(point.y - passage.from.y) <= passage.height)
            {
                Vector3 segment = Vector3.ProjectOnPlane(passage.to - passage.from, Vector3.up);
                Vector3 offset = Vector3.ProjectOnPlane(point - passage.from, Vector3.up);
                float t = segment.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0;
                if ((offset - segment * t).sqrMagnitude < passage.radius * passage.radius) return false;
            }
            if (!SpaceFree(point, profile.Radius, owner)) return false;
            foreach (var miner in MiningNpc.Miners)
            {
                if (miner == null || miner == owner || !miner.isActiveAndEnabled || !miner.HasMiningApproach) continue;
                Vector3 from = miner.Profile.Foot(miner.RootPosition), segment = miner.MiningApproachDestination - from;
                segment.y = 0;
                Vector3 offset = point - from; offset.y = 0;
                float t = segment.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0;
                float width = profile.Radius + miner.NavigationRadius + owner.ArrivalTolerance;
                if ((offset - segment * t).sqrMagnitude < width * width) return false;
            }
            return true;
        }
        public static bool TryAcquire(MiningNpc owner, Component target, Bounds bounds, Vector3 start,
            NavigationProfile profile, float reach, int capacity, int firstCandidate,
            Func<Vector3, float> surfaceDistance, Func<Vector3, Vector3> closestSurface,
            out StandReservation reservation)
        {
            var grid = MiningNavGrid.Instance;
            reservation = null;
            if (!MiningNavigation.PathfindingAvailable) return false;
            expired.Clear();
            foreach (var pair in leases)
                if (pair.Key == null || !pair.Key.isActiveAndEnabled || pair.Value.Target == null ||
                    !pair.Value.Target.gameObject.activeInHierarchy) expired.Add(pair.Key);
            foreach (var key in expired) Release(key);
            if (!orders.TryGetValue(owner, out int order)) orders[owner] = order = ++nextOrder;
            leases.TryGetValue(owner, out var existing);
            if (existing != null && existing.Target != target) { Release(owner); orders[owner] = order = ++nextOrder; existing = null; }
            bool mayMine = true;
            int occupied = 0;
            foreach (var pair in leases)
            {
                if (pair.Key == owner || pair.Value.Target != target) continue;
                if (!pair.Value.IsWaiting) occupied++;
                else if (pair.Value.order < order) mayMine = false;
            }
            mayMine &= occupied < capacity;
            if (yieldUntil.TryGetValue(owner, out float until))
            {
                bool approachingHolding = existing != null && existing.IsWaiting &&
                    Vector3.ProjectOnPlane(existing.Position - profile.Foot(start), Vector3.up).sqrMagnitude > owner.ArrivalTolerance * owner.ArrivalTolerance;
                bool accessPending = yieldPassages.TryGetValue(owner, out var passage) && AccessPending(passage);
                if (Time.time < until || approachingHolding || accessPending) mayMine = false;
                else { yieldUntil.Remove(owner); yieldPassages.Remove(owner); }
            }
            // Rejected holding positions must not exhaust the separate mining-candidate range.
            if (existing != null && existing.IsWaiting && mayMine) firstCandidate = 0;
            if (existing != null && (existing.targetBounds.center - bounds.center).sqrMagnitude < .0001f &&
                (existing.targetBounds.size - bounds.size).sqrMagnitude < .0001f && grid.IsPointClear(existing.Position, profile.Radius, profile.Height) &&
                SpaceFree(existing.Position, profile.Radius, owner) &&
                (existing.IsWaiting || surfaceDistance(existing.Position) <= reach * reach + .0001f))
            {
                if (!existing.IsWaiting || (!mayMine && HoldingSpaceFree(existing.Position, profile, owner)))
                {
                    // A deferred allocation can become unnecessary after another worker
                    // releases a lease. Do not leave its owner blocking the FIFO head.
                    queued.Remove(owner);
                    reservation = existing; return true;
                }
            }
            if (!MayAllocate(owner, grid.StandAllocationsPerFrame))
            { reservation = existing; return existing != null; }
            leases.Remove(owner);
            // Orientation is target-owned, not recomputed from the moving owner.
            // Start with the side the worker approaches from, then keep that
            // orientation for its lease lifetime. Sending everyone to the same
            // side creates unnecessary crossing and seals the final approach.
            if (!approachDirections.TryGetValue(owner, out Vector3 near))
            {
                near = Vector3.ProjectOnPlane(start - bounds.center, Vector3.up).normalized;
                if (near.sqrMagnitude < .001f) near = Vector3.forward;
                approachDirections[owner] = near;
            }
            int directions = grid.StandProbeDirections;
            if (mayMine)
            for (int n = firstCandidate; n < directions; n++)
            {
                Vector3 direction = Quaternion.AngleAxis(n * 360f / directions, Vector3.up) * near;
                Vector3 outside = bounds.center + direction * (bounds.size.magnitude + profile.Radius + 1);
                outside.y = start.y;
                Vector3 surface = closestSurface(outside);
                float offset = Mathf.Min(reach, profile.Radius + Mathf.Min(grid.StandPadding, Mathf.Max(0, reach - profile.Radius)));
                Vector3 candidate = surface + direction * offset;
                if (!grid.TryGetGroundPoint(candidate, out candidate) ||
                    surfaceDistance(candidate) > reach * reach + .0001f ||
                    !grid.IsPointClear(candidate, profile.Radius, profile.Height) || !SpaceFree(candidate, profile.Radius, owner)) continue;
                reservation = new StandReservation { Owner = owner, Target = target, Position = candidate, Candidate = n, order = order, targetBounds = bounds };
                leases[owner] = reservation; return true;
            }
            // Distinct holding points, not a queue piled on the first mining destination.
            if (existing != null && existing.IsWaiting && existing.Candidate >= firstCandidate &&
                (existing.targetBounds.center - bounds.center).sqrMagnitude < .0001f &&
                grid.IsPointClear(existing.Position, profile.Radius, profile.Height) && HoldingSpaceFree(existing.Position, profile, owner))
            { leases[owner] = reservation = existing; return true; }
            // Hold outside approach lanes, preferably where the miner already stands.
            // Sending every excess worker through the ore cluster seals its entrances.
            int localBase = directions * (owner.NavigationWaitingRings + 1);
            for (int n = Mathf.Max(0, firstCandidate - localBase); n <= directions * owner.NavigationWaitingRings; n++)
            {
                Vector3 candidate = start;
                if (n > 0)
                {
                    int ring = (n - 1) / directions + 1;
                    Vector3 away = start - bounds.center; away.y = 0;
                    if (away.sqrMagnitude < .001f) away = Vector3.back;
                    candidate += Quaternion.AngleAxis((n - 1) % directions * 360f / directions, Vector3.up) *
                        away.normalized * profile.Radius * 2.5f * ring;
                }
                Vector3 gap = candidate - bounds.ClosestPoint(candidate); gap.y = 0;
                if (gap.sqrMagnitude < (reach + profile.Radius * 2) * (reach + profile.Radius * 2) ||
                    !HoldingSpaceFree(candidate, profile, owner) ||
                    !grid.TryGetGroundPoint(candidate, out candidate) ||
                    !grid.IsPointClear(candidate, profile.Radius, profile.Height)) continue;
                reservation = new StandReservation { Owner = owner, Target = target, Position = candidate,
                    IsWaiting = true, Candidate = localBase + n, order = order, targetBounds = bounds };
                leases[owner] = reservation; return true;
            }
            for (int ring = 1; ring <= owner.NavigationWaitingRings; ring++)
            for (int n = 0; n < directions; n++)
            {
                int candidateIndex = ring * directions + n;
                if (candidateIndex < firstCandidate) continue;
                Vector3 direction = Quaternion.AngleAxis(n * 360f / directions, Vector3.up) * Vector3.forward;
                Vector3 candidate = bounds.center + direction * (bounds.extents.magnitude + ring * profile.Radius * 2.5f);
                if (!HoldingSpaceFree(candidate, profile, owner) || !grid.TryGetGroundPoint(candidate, out candidate) ||
                    !grid.IsPointClear(candidate, profile.Radius, profile.Height)) continue;
                reservation = new StandReservation { Owner = owner, Target = target, Position = candidate, IsWaiting = true, Candidate = candidateIndex, order = order, targetBounds = bounds };
                leases[owner] = reservation; return true;
            }
            return false;
        }
    }
}
