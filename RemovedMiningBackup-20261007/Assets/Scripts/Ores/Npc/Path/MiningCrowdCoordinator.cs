using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One snapshot/solve per physics tick. No contact pushing or static-obstacle detour forces.</summary>
    public static class MiningCrowdCoordinator
    {
        private struct Agent
        {
            public MiningNpc miner;
            public Vector2 position, velocity, preferred, accepted;
            public float radius;
        }
        private struct Constraint { public Vector2 point, normal; }
        private sealed class Passage
        {
            public MiningNpc owner;
            public float expires, lastProgress;
            public Vector2 lastPosition;
            public readonly List<MiningNpc> queue = new();
        }
        private static readonly List<Agent> agents = new();
        private static readonly Dictionary<MiningNpc, int> indices = new();
        private static readonly Dictionary<MiningNpc, int> stableIds = new();
        private static int nextId;
        private static readonly Dictionary<Vector2Int, List<int>> buckets = new();
        private static readonly Stack<List<int>> bucketPool = new();
        private static readonly List<Constraint> constraints = new();
        private static readonly Dictionary<Vector2Int, Passage> passages = new();
        private static readonly List<Vector2Int> expiredPassages = new();
        private static readonly HashSet<MiningNpc> yielding = new();
        private static float tick = float.NegativeInfinity;
        private const float BucketSize = 5f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { agents.Clear(); indices.Clear(); stableIds.Clear(); nextId = 0; buckets.Clear(); passages.Clear(); yielding.Clear(); tick = float.NegativeInfinity; }
        private static Vector2 Xz(Vector3 p) => new(p.x, p.z);
        private static Vector3 World(Vector2 p) => new(p.x, 0, p.y);
        private static Vector2Int Key(Vector2 p) => new(Mathf.FloorToInt(p.x / BucketSize), Mathf.FloorToInt(p.y / BucketSize));
        public static bool IsYielding(MiningNpc miner) => yielding.Contains(miner);
        public static void Remove(MiningNpc miner)
        {
            yielding.Remove(miner); indices.Remove(miner); stableIds.Remove(miner);
            foreach (var passage in passages.Values) { passage.queue.Remove(miner); if (passage.owner == miner) passage.owner = null; }
        }
        public static Vector3 Resolve(MiningNpc miner, Vector3 preferred)
        {
            if (tick != Time.fixedTime) BuildSnapshot();
            if (!indices.TryGetValue(miner, out int index)) return Vector3.zero;
            return World(agents[index].accepted);
        }
        private static void BuildSnapshot()
        {
            tick = Time.fixedTime; agents.Clear(); indices.Clear(); yielding.Clear();
            foreach (var list in buckets.Values) { list.Clear(); bucketPool.Push(list); }
            buckets.Clear();
            foreach (var miner in MiningNpc.Miners)
            {
                if (miner == null || !miner.isActiveAndEnabled || miner.IsDead || miner.NavigationData == null) continue;
                int i = agents.Count; indices[miner] = i;
                if (!stableIds.ContainsKey(miner)) stableIds[miner] = ++nextId;
                var agent = new Agent { miner = miner, position = Xz(miner.RootPosition), velocity = Xz(miner.HorizontalVelocity),
                    preferred = Xz(miner.CrowdPreferredVelocity()), radius = miner.NavigationRadius };
                agents.Add(agent); var key = Key(agent.position);
                if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = bucketPool.Count > 0 ? bucketPool.Pop() : new List<int>();
                list.Add(i);
            }
            expiredPassages.Clear();
            foreach (var pair in passages) if (pair.Value.expires < Time.time) expiredPassages.Add(pair.Key);
            foreach (var key in expiredPassages) passages.Remove(key);
            for (int i = 0; i < agents.Count; i++)
            {
                Agent a = agents[i];
                if (a.preferred.sqrMagnitude < .0001f) { a.accepted = Vector2.zero; agents[i] = a; continue; }
                constraints.Clear();
                var data = a.miner.NavigationData; var key = Key(a.position);
                int range = Mathf.CeilToInt(data.CrowdNeighborDistance / BucketSize);
                for (int z = -range; z <= range; z++) for (int x = -range; x <= range; x++)
                {
                    if (!buckets.TryGetValue(key + new Vector2Int(x, z), out var list)) continue;
                    foreach (int j in list)
                    {
                        if (i == j) continue;
                        Agent b = agents[j];
                        if ((b.position - a.position).sqrMagnitude > data.CrowdNeighborDistance * data.CrowdNeighborDistance) continue;
                        constraints.Add(ReciprocalConstraint(a, b, data.CrowdTimeHorizon));
                    }
                }
                float acceleration = data.MovementAcceleration * a.miner.CrowdSpeedMultiplier;
                float speed = Mathf.Max(a.velocity.magnitude, a.preferred.magnitude);
                Vector2 accelerated = Vector2.MoveTowards(a.velocity, a.preferred,
                    Mathf.Max(acceleration, data.BrakingAcceleration * a.miner.CrowdSpeedMultiplier * a.miner.CrowdSpeedMultiplier) * Time.fixedDeltaTime);
                Vector2 candidate = Solve(accelerated, speed, constraints);
                // A constrained velocity is only useful if it fits the static corridor.
                if (!Fits(a, candidate) || !Satisfies(candidate, constraints)) candidate = Vector2.zero;
                if (!HasPassage(a)) candidate = Vector2.zero;
                a.accepted = candidate; agents[i] = a;
                if (candidate.sqrMagnitude < .001f) yielding.Add(a.miner);
            }
            // Hard next-step separation validates the final paired result, not stale neighbour preferences.
            for (int i = 0; i < agents.Count; i++) for (int j = i + 1; j < agents.Count; j++)
            {
                Agent a = agents[i], b = agents[j];
                Vector2 delta = b.position - a.position;
                if (delta.sqrMagnitude > (a.radius + b.radius + (a.accepted.magnitude + b.accepted.magnitude) * Time.fixedDeltaTime) *
                    (a.radius + b.radius + (a.accepted.magnitude + b.accepted.magnitude) * Time.fixedDeltaTime)) continue;
                Vector2 next = delta + (b.accepted - a.accepted) * Time.fixedDeltaTime;
                float separation = a.radius + b.radius;
                if (next.sqrMagnitude >= separation * separation || next.sqrMagnitude >= delta.sqrMagnitude) continue;
                a.accepted = b.accepted = Vector2.zero;
                yielding.Add(a.miner); yielding.Add(b.miner); agents[i] = a; agents[j] = b;
            }
        }
        private static bool Fits(Agent a, Vector2 velocity)
        {
            var grid = MiningNavGrid.Instance; var profile = a.miner.Profile;
            return MiningNavigation.PathfindingAvailable && grid.IsSegmentClear(profile.Foot(a.miner.RootPosition),
                profile.Foot(a.miner.RootPosition + World(velocity) * Time.fixedDeltaTime), profile.Radius, profile.Height);
        }
        private static Constraint ReciprocalConstraint(Agent a, Agent b, float horizon)
        {
            Vector2 relativePosition = b.position - a.position, relativeVelocity = a.velocity - b.velocity;
            float distanceSq = relativePosition.sqrMagnitude, radius = a.radius + b.radius;
            Vector2 normal, correction;
            if (distanceSq > radius * radius)
            {
                float inverse = 1 / Mathf.Max(.1f, horizon);
                Vector2 w = relativeVelocity - relativePosition * inverse;
                float dot = Vector2.Dot(w, relativePosition);
                if (dot < 0 && dot * dot > radius * radius * w.sqrMagnitude)
                { normal = w.normalized; correction = (radius * inverse - w.magnitude) * normal; }
                else
                {
                    float leg = Mathf.Sqrt(distanceSq - radius * radius);
                    Vector2 direction = Det(relativePosition, w) > 0
                        ? new Vector2(relativePosition.x * leg - relativePosition.y * radius, relativePosition.x * radius + relativePosition.y * leg) / distanceSq
                        : -new Vector2(relativePosition.x * leg + relativePosition.y * radius, -relativePosition.x * radius + relativePosition.y * leg) / distanceSq;
                    correction = Vector2.Dot(relativeVelocity, direction) * direction - relativeVelocity;
                    normal = new Vector2(-direction.y, direction.x);
                }
            }
            else
            {
                float inverse = 1 / Mathf.Max(.001f, Time.fixedDeltaTime);
                Vector2 w = relativeVelocity - relativePosition * inverse;
                normal = w.sqrMagnitude > .000001f ? w.normalized : (stableIds[a.miner] < stableIds[b.miner] ? Vector2.left : Vector2.right);
                correction = (radius * inverse - w.magnitude) * normal;
            }
            float responsibility = b.preferred.sqrMagnitude < .0001f ? 1 : .5f;
            return new Constraint { point = a.velocity + correction * responsibility, normal = normal };
        }
        private static float Det(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static bool Satisfies(Vector2 velocity, List<Constraint> lines)
        { foreach (var line in lines) if (Vector2.Dot(velocity - line.point, line.normal) < -.001f) return false; return true; }
        private static Vector2 Solve(Vector2 preferred, float speed, List<Constraint> lines)
        {
            Vector2 result = Vector2.ClampMagnitude(preferred, speed);
            for (int i = 0; i < lines.Count; i++)
            {
                Constraint line = lines[i];
                if (Vector2.Dot(result - line.point, line.normal) >= 0) continue;
                Vector2 direction = new(-line.normal.y, line.normal.x);
                float dot = Vector2.Dot(line.point, direction);
                float discriminant = dot * dot + speed * speed - line.point.sqrMagnitude;
                if (discriminant < 0) return Vector2.zero;
                float root = Mathf.Sqrt(discriminant), low = -dot - root, high = -dot + root;
                for (int j = 0; j < i; j++)
                {
                    float denominator = Vector2.Dot(direction, lines[j].normal);
                    float numerator = Vector2.Dot(lines[j].point - line.point, lines[j].normal);
                    if (Mathf.Abs(denominator) < .00001f) { if (numerator > 0) return Vector2.zero; continue; }
                    float t = numerator / denominator;
                    if (denominator > 0) low = Mathf.Max(low, t); else high = Mathf.Min(high, t);
                }
                if (low > high) return Vector2.zero;
                result = line.point + direction * Mathf.Clamp(Vector2.Dot(preferred - line.point, direction), low, high);
            }
            return result;
        }
        private static bool HasPassage(Agent agent)
        {
            var grid = MiningNavGrid.Instance; var profile = agent.miner.Profile;
            if (!MiningNavigation.PathfindingAvailable) return false;
            Vector3 forward = World(agent.preferred.normalized), right = Vector3.Cross(Vector3.up, forward);
            Vector3 foot = profile.Foot(agent.miner.RootPosition);
            if (grid.IsPointClear(foot + right * profile.Radius * 2, profile.Radius, profile.Height) ||
                grid.IsPointClear(foot - right * profile.Radius * 2, profile.Radius, profile.Height)) return true;
            // A coarse spatial bucket is not a corridor. Only arbitrate opposing
            // traffic with a clear connection; parallel or wall-separated lanes
            // must not share an exclusive claim.
            bool opposing = false;
            foreach (var other in agents)
            {
                if (other.miner == agent.miner || other.preferred.sqrMagnitude < .001f ||
                    Vector2.Dot(agent.preferred.normalized, other.preferred.normalized) > -.3f ||
                    (other.position - agent.position).sqrMagnitude > BucketSize * BucketSize) continue;
                if (grid.IsSegmentClear(foot, other.miner.Profile.Foot(other.miner.RootPosition), profile.Radius, profile.Height))
                { opposing = true; break; }
            }
            if (!opposing) return true;
            // Quantized corridor tiles share one FIFO claim. An owner retains it while advancing.
            var key = new Vector2Int(Mathf.FloorToInt(foot.x / BucketSize), Mathf.FloorToInt(foot.z / BucketSize));
            if (!passages.TryGetValue(key, out var passage)) passages[key] = passage = new Passage();
            passage.queue.RemoveAll(m => m == null || !indices.ContainsKey(m));
            if (!passage.queue.Contains(agent.miner)) passage.queue.Add(agent.miner);
            if (passage.owner != null && (!indices.TryGetValue(passage.owner, out int ownerIndex) ||
                Key(agents[ownerIndex].position) != key || agents[ownerIndex].preferred.sqrMagnitude < .0001f))
            { passage.queue.Remove(passage.owner); passage.owner = null; }
            if (passage.owner != null && indices.TryGetValue(passage.owner, out int holder))
            {
                Vector2 position = agents[holder].position;
                if ((position - passage.lastPosition).sqrMagnitude >= .08f * .08f)
                { passage.lastPosition = position; passage.lastProgress = Time.time; }
                else if (Time.time - passage.lastProgress > passage.owner.NavigationData.StuckTimeout)
                {
                    passage.queue.Remove(passage.owner); passage.queue.Add(passage.owner);
                    passage.owner = null;
                }
            }
            if (passage.owner == null || !passage.owner.isActiveAndEnabled)
            {
                passage.owner = passage.queue[0];
                passage.lastPosition = agents[indices[passage.owner]].position;
                passage.lastProgress = Time.time;
            }
            if (passage.owner == agent.miner) passage.expires = Time.time + agent.miner.NavigationData.CrowdSideHoldTime;
            return passage.owner == agent.miner;
        }
    }
}
