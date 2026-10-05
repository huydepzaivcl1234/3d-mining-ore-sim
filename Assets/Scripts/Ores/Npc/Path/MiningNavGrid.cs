using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Single-floor Terrain A*. Cell data only; never creates GameObjects for cells.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MiningNavGrid : MonoBehaviour
    {
        public static MiningNavGrid Instance { get; private set; }
        [Header("Area: automatic uses Terrain bounds")]
        [SerializeField] private bool autoBounds = true;
        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector2 areaSize = new(60, 60);
        [Header("Ground and actor clearance")]
        [Min(.1f)] [SerializeField] private float cellSize = 1;
        [SerializeField] private LayerMask obstacleLayers = ~0, groundLayers = ~0;
        [Min(.05f)] [SerializeField] private float agentRadius = .55f;
        [Min(.1f)] [SerializeField] private float probeHeight = 1.8f;
        [Min(.01f)] [SerializeField] private float probeGroundClearance = .08f;
        [Range(0, 89)] [SerializeField] private float maximumSlope = 45;
        [Min(.01f)] [SerializeField] private float maximumStep = .5f;
        [Min(0)] [SerializeField] private float slopeCost = 2;
        [SerializeField] private LayerMask difficultGroundLayers;
        [Min(1)] [SerializeField] private float difficultGroundCost = 3;
        [Min(1)] [SerializeField] private float groundProbeHeight = 1000;
        [Header("Per-frame budgets")]
        [Min(1)] [SerializeField] private int bakeCellsPerFrame = 512, searchNodesPerFrame = 256;
        [Min(1)] [SerializeField] private int maximumCells = 250000;
        [Min(0)] [SerializeField] private int maximumSearchRestarts = 1;
        [SerializeField] private bool rebakeOnStart = true;
        [Header("Scene debug (select navigation object)")]
        [SerializeField] private bool drawGizmos = true;
        [Min(1)] [SerializeField] private int maximumGizmoCells = 5000;
        [Min(1)] [SerializeField] private float gizmoRadius = 20;
        [Min(1)] [SerializeField] private int smoothingLookAheadCells = 12;
        [Header("Target stand points")]
        [Min(4)] [SerializeField] private int standProbeDirections = 24;
        [Min(.1f)] [SerializeField] private float standProjectionRadius = 1.5f;
        [Min(.01f)] [SerializeField] private float standPadding = .2f;
        private struct Cell { public Vector3 point; public float cost; public bool ground, open; }
        private Cell[] cells;
        private Terrain[] terrains;
        private Vector3 origin;
        private int width, depth, cursor, nextTicket;
        private readonly Collider[] overlaps = new Collider[128];
        private readonly RaycastHit[] hits = new RaycastHit[128];
        private readonly HashSet<int> dirty = new();
        private readonly Queue<Request> requests = new();
        private readonly Dictionary<UnityEngine.Object, int> tickets = new();
        private Coroutine worker;
        private Request activeRequest;
        private readonly Search sync = new(), queued = new();
        private sealed class Request
        {
            public UnityEngine.Object owner;
            public int ticket;
            public Vector3 start, end;
            public float radius, height;
            public Action<bool, List<Vector3>> complete;
        }
        public bool HasBaked { get; private set; }
        public int Revision { get; private set; }
        public int CellCount => cells?.Length ?? 0;
        public float BuildProgress => CellCount == 0 ? 0 : (float)cursor / CellCount;
        public int PendingRequests => requests.Count + (worker != null ? 1 : 0);
        public int StandProbeDirections => Mathf.Max(4, standProbeDirections);
        public float StandProjectionRadius => standProjectionRadius;
        public float StandPadding => standPadding;
        public void Configure(float size, float radius, float height, float slope, float step,
            LayerMask obstacles, int bakeBudget, int searchBudget)
        {
            cellSize = Mathf.Max(.1f, size); agentRadius = Mathf.Max(.05f, radius);
            probeHeight = Mathf.Max(radius * 2, height); maximumSlope = Mathf.Clamp(slope, 0, 89);
            maximumStep = Mathf.Max(.01f, step); obstacleLayers = obstacles;
            bakeCellsPerFrame = Mathf.Max(1, bakeBudget); searchNodesPerFrame = Mathf.Max(1, searchBudget);
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }
        private void Start() { if (rebakeOnStart && cells == null) RequestFullRebake(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void OnDisable()
        {
            StopAllCoroutines(); worker = null;
            var canceled = new List<Request>(requests);
            if (activeRequest != null) canceled.Add(activeRequest);
            requests.Clear(); activeRequest = null;
            foreach (Request request in canceled)
                if (Current(request)) { tickets.Remove(request.owner); request.complete(false, new List<Vector3>()); }
            tickets.Clear();
        }
        public void EnsureClearance(float radius)
        {
            if (radius <= agentRadius) return;
            agentRadius = radius;
            if (cells != null) for (int i = 0; i < cells.Length; i++) dirty.Add(i);
        }
        public void RequestFullRebake()
        {
            if (activeRequest != null && Current(activeRequest)) requests.Enqueue(activeRequest);
            activeRequest = null;
            StopAllCoroutines(); worker = null; HasBaked = false; dirty.Clear();
            terrains = Terrain.activeTerrains;
            Vector3 min = areaCenter - new Vector3(areaSize.x, 0, areaSize.y) * .5f;
            Vector3 max = min + new Vector3(areaSize.x, 0, areaSize.y);
            if (autoBounds && terrains.Length > 0)
            {
                min = terrains[0].transform.position; max = min + terrains[0].terrainData.size;
                foreach (Terrain terrain in terrains)
                { min = Vector3.Min(min, terrain.transform.position); max = Vector3.Max(max, terrain.transform.position + terrain.terrainData.size); }
            }
            cellSize = Mathf.Max(.1f, cellSize);
            width = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / cellSize));
            depth = Mathf.Max(1, Mathf.CeilToInt((max.z - min.z) / cellSize));
            if ((long)width * depth > maximumCells)
            { cells = null; Debug.LogError("[MiningNavGrid] Grid exceeds Maximum Cells. Increase Cell Size or reduce Area Size.", this); return; }
            origin = min; cells = new Cell[width * depth]; cursor = 0; Revision++;
            if (Application.isPlaying)
            {
                foreach (Ore ore in FindObjectsByType<Ore>(FindObjectsSortMode.None)) MiningGridObstacle.Ensure(ore);
                foreach (MiningChest chest in FindObjectsByType<MiningChest>(FindObjectsSortMode.None)) MiningGridObstacle.Ensure(chest);
                foreach (LuckyBlock block in FindObjectsByType<LuckyBlock>(FindObjectsSortMode.None)) MiningGridObstacle.Ensure(block);
            }
        }
        public void Rebake()
        {
            RequestFullRebake(); if (cells == null) return;
            while (cursor < cells.Length) BakeCell(cursor++);
            HasBaked = true;
        }
        private void Update()
        {
            if (cells == null) return;
            int budget = Mathf.Max(1, bakeCellsPerFrame);
            if (!HasBaked)
            {
                while (budget-- > 0 && cursor < cells.Length) BakeCell(cursor++);
                HasBaked = cursor == cells.Length;
            }
            if (!HasBaked) return;
            bool changed = false;
            while (budget-- > 0 && dirty.Count > 0)
            {
                var it = dirty.GetEnumerator(); it.MoveNext(); int i = it.Current; it.Dispose(); dirty.Remove(i);
                bool open = cells[i].ground && IsCapsuleClear(cells[i].point, agentRadius, probeHeight);
                changed |= open != cells[i].open; cells[i].open = open;
            }
            if (changed) Revision++;
            if (worker == null && requests.Count > 0 && dirty.Count == 0) worker = StartCoroutine(ProcessRequests());
        }
        private void BakeCell(int i)
        {
            Vector3 p = origin + new Vector3((i % width + .5f) * cellSize, 0, (i / width + .5f) * cellSize);
            bool ground = SampleGround(ref p, out float slope, out int layer) && slope <= maximumSlope;
            cells[i] = new Cell { point = p, ground = ground, open = ground && IsCapsuleClear(p, agentRadius, probeHeight),
                cost = (1 + slopeCost * slope / 90) * (((1 << layer) & difficultGroundLayers.value) != 0 ? difficultGroundCost : 1) };
        }
        private bool SampleGround(ref Vector3 p, out float slope, out int layer)
        {
            slope = 0; layer = 0;
            foreach (Terrain t in terrains)
            {
                Vector3 local = p - t.transform.position, size = t.terrainData.size;
                if (local.x < 0 || local.z < 0 || local.x >= size.x || local.z >= size.z) continue;
                p.y = t.SampleHeight(p) + t.transform.position.y;
                slope = t.terrainData.GetSteepness(local.x / size.x, local.z / size.z);
                layer = t.gameObject.layer; return true;
            }
            int count = Physics.RaycastNonAlloc(p + Vector3.up * groundProbeHeight, Vector3.down,
                hits, groundProbeHeight * 2, groundLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i]; if (hit.distance >= nearest || IsActorOrMineable(hit.collider)) continue;
                nearest = hit.distance; p.y = hit.point.y; slope = Vector3.Angle(hit.normal, Vector3.up); layer = hit.collider.gameObject.layer;
            }
            return count < hits.Length && nearest < float.PositiveInfinity;
        }
        private static bool IsActorOrMineable(Collider c) => c.GetComponentInParent<Ore>() != null ||
            c.GetComponentInParent<MiningChest>() != null || c.GetComponentInParent<LuckyBlock>() != null ||
            c.GetComponentInParent<MiningNpc>() != null || c.GetComponentInParent<CharacterController>() != null ||
            (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic);
        private bool Blocks(Collider c, float ground)
        {
            if (c is TerrainCollider || c.bounds.max.y <= ground + probeGroundClearance) return false;
            Ore ore = c.GetComponentInParent<Ore>(); if (ore != null) return !ore.IsDepleted;
            MiningChest chest = c.GetComponentInParent<MiningChest>(); if (chest != null) return chest.CanMine;
            LuckyBlock block = c.GetComponentInParent<LuckyBlock>(); if (block != null) return !block.IsResolved;
            return !IsActorOrMineable(c);
        }
        public bool IsPointClear(Vector3 p, float radius, float height = 0)
        {
            if (terrains == null || !SampleGround(ref p, out float slope, out _) || slope > maximumSlope) return false;
            return IsCapsuleClear(p, radius, height);
        }
        public bool TryGetGroundPoint(Vector3 position, out Vector3 point)
        {
            point = position;
            return HasBaked && Index(point) >= 0 && SampleGround(ref point, out float slope, out _) &&
                slope <= maximumSlope;
        }
        private bool IsCapsuleClear(Vector3 p, float radius, float height)
        {
            radius = Mathf.Max(.05f, radius);
            Vector3 bottom = p + Vector3.up * (radius + probeGroundClearance);
            Vector3 top = p + Vector3.up * Mathf.Max(radius + probeGroundClearance, (height > 0 ? height : probeHeight) - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, obstacleLayers, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (Blocks(overlaps[i], p.y)) return false;
            return true;
        }
        public void MarkDirty(Bounds b)
        {
            if (cells == null) return;
            b.Expand(agentRadius * 2 + cellSize);
            int x0 = Mathf.Clamp(Mathf.FloorToInt((b.min.x - origin.x) / cellSize), 0, width - 1);
            int x1 = Mathf.Clamp(Mathf.FloorToInt((b.max.x - origin.x) / cellSize), 0, width - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((b.min.z - origin.z) / cellSize), 0, depth - 1);
            int z1 = Mathf.Clamp(Mathf.FloorToInt((b.max.z - origin.z) / cellSize), 0, depth - 1);
            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++) dirty.Add(z * width + x);
        }
        private int Index(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - origin.x) / cellSize), z = Mathf.FloorToInt((p.z - origin.z) / cellSize);
            return x < 0 || z < 0 || x >= width || z >= depth ? -1 : z * width + x;
        }
        public bool TryProject(Vector3 p, float distance, out Vector3 projected)
        {
            projected = p; if (!HasBaked || Index(p) < 0) return false;
            int center = Index(p), range = Mathf.CeilToInt(distance / cellSize), best = -1;
            float nearest = distance * distance + cellSize * cellSize;
            for (int z = Mathf.Max(0, center / width - range); z <= Mathf.Min(depth - 1, center / width + range); z++)
            for (int x = Mathf.Max(0, center % width - range); x <= Mathf.Min(width - 1, center % width + range); x++)
            {
                int i = z * width + x; Vector3 delta = cells[i].point - p; delta.y = 0;
                if (cells[i].open && delta.sqrMagnitude < nearest) { nearest = delta.sqrMagnitude; best = i; }
            }
            if (best < 0) return false; projected = cells[best].point; return true;
        }
        private bool Connected(Search search, int a, int b) => a >= 0 && b >= 0 &&
            cells[a].ground && cells[b].ground && Fits(search, a) && Fits(search, b) &&
            Mathf.Abs(cells[a].point.y - cells[b].point.y) <= maximumStep;
        private bool CornerClear(int index, float radius, float height) =>
            index >= 0 && index < cells.Length && cells[index].ground &&
            IsCapsuleClear(cells[index].point, radius, height);
        public bool IsSegmentClear(Vector3 start, Vector3 end, float radius, float height = 0)
        {
            if (!HasBaked) return false;
            Vector3 delta = end - start; delta.y = 0;
            float sampleSpacing = Mathf.Min(cellSize * .25f, Mathf.Max(.025f, radius * .5f));
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / sampleSpacing)), previous = -1;
            for (int step = 0; step <= steps; step++)
            {
                Vector3 p = Vector3.Lerp(start, end, (float)step / steps); int i = Index(p);
                // Cell centers are conservative samples, not the actor's actual position.
                // A clear endpoint beside an ore can belong to a cell whose center is blocked.
                if (i < 0 || !cells[i].ground || !TryGetGroundPoint(p, out Vector3 ground) ||
                    !IsCapsuleClear(ground, radius, height)) return false;
                if (previous >= 0 && i != previous)
                {
                    if (Mathf.Abs(cells[previous].point.y - cells[i].point.y) > maximumStep) return false;
                    int dx = i % width - previous % width, dz = i / width - previous / width;
                    if (dx != 0 && dz != 0 && (!CornerClear(previous + dx, radius, height) ||
                        !CornerClear(previous + dz * width, radius, height) ||
                        Mathf.Abs(cells[previous + dx].point.y - cells[i].point.y) > maximumStep ||
                        Mathf.Abs(cells[previous + dz * width].point.y - cells[i].point.y) > maximumStep)) return false;
                }
                previous = i;
            }
            return true;
        }
        public void Cancel(UnityEngine.Object owner) { if (owner != null) tickets.Remove(owner); }
        public void RequestPath(UnityEngine.Object owner, Vector3 start, Vector3 end, Action<bool, List<Vector3>> complete,
            float radius = 0, float height = 0)
        {
            int ticket = ++nextTicket; tickets[owner] = ticket;
            requests.Enqueue(new Request { owner = owner, ticket = ticket, start = start, end = end, complete = complete,
                radius = radius > 0 ? radius : agentRadius, height = height > 0 ? height : probeHeight });
        }
        private bool Current(Request r) => r.owner != null && tickets.TryGetValue(r.owner, out int ticket) && ticket == r.ticket;
        private IEnumerator ProcessRequests()
        {
            yield return null;
            while (requests.Count > 0)
            {
                Request r = requests.Dequeue(); if (!Current(r)) continue;
                activeRequest = r;
                bool started = Begin(queued, r.start, r.end, r.radius, r.height); int revision = Revision, restarts = 0;
                while (started && !queued.finished && Current(r))
                {
                    Expand(queued, Mathf.Max(1, searchNodesPerFrame));
                    if (!queued.finished) yield return null;
                    if (!queued.finished && Revision != revision && restarts < maximumSearchRestarts)
                    { started = Begin(queued, r.start, r.end, r.radius, r.height); revision = Revision; restarts++; }
                }
                if (!Current(r)) { activeRequest = null; continue; }
                var route = new List<Vector3>();
                bool success = started && queued.found && BuildRoute(queued, r.start, r.end, route);
                tickets.Remove(r.owner); activeRequest = null; r.complete(success, route); yield return null;
            }
            worker = null;
        }
        public bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> result)
        {
            result.Clear(); if (!Begin(sync, start, end)) return false;
            Expand(sync, cells.Length * 8); return sync.found && BuildRoute(sync, start, end, result);
        }
        private bool Begin(Search s, Vector3 start, Vector3 end, float radius = 0, float height = 0)
        {
            radius = radius > 0 ? radius : agentRadius; height = height > 0 ? height : probeHeight;
            if (!TryConnectEndpoint(start, radius, height, out Vector3 from) ||
                !TryConnectEndpoint(end, radius, height, out Vector3 to)) return false;
            s.Reset(cells.Length, Index(from), Index(to)); s.radius = radius; s.height = height;
            s.Push(s.start, Heuristic(s.start, s.goal)); return true;
        }
        private bool TryConnectEndpoint(Vector3 endpoint, float radius, float height, out Vector3 point)
        {
            point = endpoint;
            int center = Index(endpoint);
            if (!HasBaked || center < 0 || !IsPointClear(endpoint, radius, height)) return false;
            int best = -1; float nearest = float.PositiveInfinity;
            int range = Mathf.CeilToInt(StandProjectionRadius / cellSize);
            for (int z = Mathf.Max(0, center / width - range); z <= Mathf.Min(depth - 1, center / width + range); z++)
            for (int x = Mathf.Max(0, center % width - range); x <= Mathf.Min(width - 1, center % width + range); x++)
            {
                int i = z * width + x;
                float distance = Vector3.ProjectOnPlane(cells[i].point - endpoint, Vector3.up).sqrMagnitude;
                if (!cells[i].ground || distance >= nearest || !IsCapsuleClear(cells[i].point, radius, height) ||
                    !IsSegmentClear(endpoint, cells[i].point, radius, height)) continue;
                best = i; nearest = distance;
            }
            if (best < 0) return false;
            point = cells[best].point; return true;
        }
        private float Heuristic(int a, int b)
        {
            int x = Mathf.Abs(a % width - b % width), z = Mathf.Abs(a / width - b / width);
            return cellSize * (Mathf.Max(x, z) + .41421356f * Mathf.Min(x, z));
        }
        private void Expand(Search s, int budget)
        {
            while (budget-- > 0 && s.Count > 0)
            {
                int current = s.Pop(); if (s.closed[current] == s.stamp) continue;
                s.closed[current] = s.stamp;
                if (current == s.goal) { s.found = s.finished = true; return; }
                int x = current % width, z = current / width;
                for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx == 0 && dz == 0) || x + dx < 0 || x + dx >= width || z + dz < 0 || z + dz >= depth) continue;
                    int next = current + dz * width + dx;
                    if (!Connected(s, current, next) || s.closed[next] == s.stamp) continue;
                    if (dx != 0 && dz != 0 && (!Connected(s, current, current + dx) || !Connected(s, current, current + dz * width) ||
                        !Connected(s, current + dx, next) || !Connected(s, current + dz * width, next))) continue;
                    float score = s.score[current] + Vector3.Distance(cells[current].point, cells[next].point) * (cells[current].cost + cells[next].cost) * .5f;
                    if (s.seen[next] == s.stamp && score >= s.score[next]) continue;
                    s.seen[next] = s.stamp; s.score[next] = score; s.parent[next] = current;
                    s.Push(next, score + Heuristic(next, s.goal));
                }
            }
            if (s.Count == 0) s.finished = true;
        }
        private bool Fits(Search s, int index)
        {
            if (Mathf.Approximately(s.radius, agentRadius) && Mathf.Approximately(s.height, probeHeight)) return cells[index].open;
            if (s.clearanceStamp[index] != s.stamp)
            { s.clearanceStamp[index] = s.stamp; s.fits[index] = IsCapsuleClear(cells[index].point, s.radius, s.height); }
            return s.fits[index];
        }
        private bool BuildRoute(Search s, Vector3 start, Vector3 end, List<Vector3> result)
        {
            var raw = s.route; raw.Clear();
            for (int i = s.goal; i != s.start; i = s.parent[i]) raw.Add(cells[i].point);
            raw.Add(cells[s.start].point); raw.Reverse(); raw.Add(end);
            Vector3 previous = start;
            for (int i = 0; i < raw.Count;)
            {
                int next = i;
                // Clearance-aware smoothing, without shortcuts through costly terrain.
                while (next + 1 < raw.Count && next - i < smoothingLookAheadCells &&
                    IsSegmentClear(previous, raw[next + 1], s.radius, s.height) && cells[Index(raw[next + 1])].cost <= 1.01f) next++;
                if (!IsSegmentClear(previous, raw[next], s.radius, s.height)) { result.Clear(); return false; }
                result.Add(raw[next]); previous = raw[next]; i = next + 1;
            }
            return result.Count > 0;
        }
        private sealed class Search
        {
            public int[] seen, closed, parent; public float[] score;
            public int[] clearanceStamp; public bool[] fits; public float radius, height;
            public int stamp, start, goal; public bool found, finished;
            public readonly List<Vector3> route = new();
            private readonly List<(int node, float cost)> heap = new();
            public int Count => heap.Count;
            public void Reset(int count, int from, int to)
            {
                if (seen == null || seen.Length != count)
                { seen = new int[count]; closed = new int[count]; parent = new int[count]; score = new float[count]; clearanceStamp = new int[count]; fits = new bool[count]; stamp = 0; }
                if (++stamp == int.MaxValue) { Array.Clear(seen, 0, count); Array.Clear(closed, 0, count); Array.Clear(clearanceStamp, 0, count); stamp = 1; }
                start = from; goal = to; found = finished = false; heap.Clear(); seen[from] = stamp; score[from] = 0;
            }
            public void Push(int node, float cost)
            {
                heap.Add((node, cost)); int i = heap.Count - 1;
                while (i > 0) { int p = (i - 1) / 2; if (heap[p].cost <= cost) break; heap[i] = heap[p]; i = p; }
                heap[i] = (node, cost);
            }
            public int Pop()
            {
                int node = heap[0].node; var last = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
                if (heap.Count == 0) return node;
                int i = 0;
                while (i * 2 + 1 < heap.Count)
                {
                    int child = i * 2 + 1;
                    if (child + 1 < heap.Count && heap[child + 1].cost < heap[child].cost) child++;
                    if (heap[child].cost >= last.cost) break;
                    heap[i] = heap[child]; i = child;
                }
                heap[i] = last; return node;
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || cells == null) return;
            Vector3 focus = areaCenter;
#if UNITY_EDITOR
            if (UnityEditor.SceneView.lastActiveSceneView != null) focus = UnityEditor.SceneView.lastActiveSceneView.pivot;
#endif
            int drawn = 0;
            foreach (Cell c in cells)
            {
                Vector3 offset = c.point - focus; offset.y = 0; if (offset.sqrMagnitude > gizmoRadius * gizmoRadius) continue;
                Gizmos.color = c.open ? new Color(0, 1, .4f, .25f) : new Color(1, .15f, .1f, .6f);
                Gizmos.DrawWireCube(c.point + Vector3.up * .04f, new Vector3(cellSize * .95f, .04f, cellSize * .95f));
                if (++drawn >= maximumGizmoCells) break;
            }
        }
    }
}
