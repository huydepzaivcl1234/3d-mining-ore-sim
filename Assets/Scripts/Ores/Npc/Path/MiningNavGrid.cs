using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Grid-based A* navigation for mining NPCs.
    ///
    /// The old movement (still in <see cref="MiningNpc"/>) only ever asks "is there something
    /// directly in front of me right now?" via SphereCasts, then nudges left/right around it.
    /// That is a purely local/reactive scheme - it has no idea an aisle it just stepped into is a
    /// dead end, so it cannot produce an actual shortest path, only "whatever looked clear a
    /// moment ago".
    ///
    /// This component bakes the level into a grid of walkable/blocked cells (inflated by the
    /// agent's radius so a "clear" cell is guaranteed to physically fit the NPC's capsule) and
    /// runs A* over that grid on request, returning a smoothed (string-pulled) waypoint list -
    /// an actual global shortest path around every currently-known obstacle, not just the nearest
    /// one.
    ///
    /// It intentionally does NOT replace the reactive steering in MiningNpc: obstacles that
    /// changed since the last bake (an ore that just spawned, another NPC standing in the way)
    /// are still handled by that local layer. This grid supplies the strategic route between
    /// waypoints; MiningNpc's existing SphereCast/detour code supplies the last-meter dodge.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MiningNavGrid : MonoBehaviour
    {
        public static MiningNavGrid Instance { get; private set; }

        [Header("Area")]
        [Tooltip("Computes the navigable area automatically from every collider on Obstacle Layers, expanded by Auto Bounds Padding. Turn off to set Area Center/Area Size by hand instead.")]
        [SerializeField] private bool autoBounds = true;
        [Min(0f)][SerializeField] private float autoBoundsPadding = 6f;
        [Tooltip("World-space center of the navigable area. Ignored while Auto Bounds is on.")]
        [SerializeField] private Vector3 areaCenter;
        [Tooltip("Width (X) and depth (Z) of the navigable area, in world units. Ignored while Auto Bounds is on.")]
        [SerializeField] private Vector2 areaSize = new(60f, 60f);

        [Header("Grid")]
        [Tooltip("World size of one grid cell. Smaller = more accurate paths but more memory/CPU per bake.")]
        [Min(0.05f)][SerializeField] private float cellSize = 0.4f;
        [Tooltip("Obstacles that block a cell. This should match ores/rocks/terrain - NOT the NPC layer itself (NPC-NPC avoidance is already handled by MiningNpc's own separation logic, and NPCs move too often to usefully bake into a periodically-rebaked grid).")]
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [Tooltip("Clearance baked around every obstacle so a 'walkable' cell always physically fits the NPC. Should be >= the NPC capsule radius plus a small margin.")]
        [Min(0f)][SerializeField] private float agentRadius = 0.55f;
        [Tooltip("Vertical size of the obstacle probe box, measured upward from Probe Ground Clearance.")]
        [Min(0.05f)][SerializeField] private float probeHeight = 1.4f;
        [Tooltip("How far above the floor the obstacle probe starts. This must be greater than zero, otherwise the walkable floor's own collider is detected as an obstacle in EVERY cell and the whole grid bakes as blocked. Raise it if a thick floor/terrain still blocks everything.")]
        [Min(0f)][SerializeField] private float probeGroundClearance = 0.2f;

        [Header("Rebake")]
        [Tooltip("Minimum time between automatic full rebakes of obstacle occupancy. Obstacles that change between rebakes are still handled by MiningNpc's reactive steering.")]
        [Min(0.05f)][SerializeField] private float rebakeInterval = 0.75f;
        [SerializeField] private bool rebakeOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos;

        // Grid storage. Sized to width*height and only reallocated when the grid dimensions change.
        private bool[] walkable;
        private bool[] rawBlockedBuffer;
        private int[] visitedStamp;
        private int[] closedStamp;
        private float[] gScore;
        private int[] cameFrom;
        private int currentStamp;
        private int width;
        private int height;
        private Vector3 origin;
        private float nextRebakeTime;
        private bool boundsComputed;
        private bool hasBaked;

        // Binary min-heap (open set) over cell indices, keyed by fScore. Reused across searches.
        private int[] heapItems;
        private float[] heapKeys;
        private int[] heapPosition;
        private int heapCount;

        private readonly Collider[] overlapBuffer = new Collider[16];
        private readonly List<Vector3> rawPathBuffer = new();
        private readonly List<Vector3> smoothPathBuffer = new();
        private readonly List<Vector3> smoothPassBuffer = new();

        private static readonly (int dx, int dz, float cost)[] Neighbors =
        {
            (1, 0, 1f), (-1, 0, 1f), (0, 1, 1f), (0, -1, 1f),
            (1, 1, 1.41421356f), (1, -1, 1.41421356f),
            (-1, 1, 1.41421356f), (-1, -1, 1.41421356f),
        };

        public bool HasBaked => hasBaked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (rebakeOnStart)
            {
                Rebake();
            }
        }

        private void Update()
        {
            if (Time.time >= nextRebakeTime)
            {
                Rebake();
            }
        }

        /// <summary>Forces the area bounds to be recomputed (if Auto Bounds is on) and rebakes immediately.</summary>
        public void RequestFullRebake()
        {
            boundsComputed = false;
            nextRebakeTime = 0f;
        }

        public void Rebake()
        {
            nextRebakeTime = Time.time + rebakeInterval;

            if (autoBounds && !boundsComputed)
            {
                ComputeAutoBounds();
                boundsComputed = true;
            }

            int newWidth = Mathf.Max(2, Mathf.CeilToInt(areaSize.x / cellSize));
            int newHeight = Mathf.Max(2, Mathf.CeilToInt(areaSize.y / cellSize));
            origin = new Vector3(areaCenter.x - newWidth * cellSize * 0.5f, areaCenter.y,
                areaCenter.z - newHeight * cellSize * 0.5f);

            if (walkable == null || newWidth != width || newHeight != height)
            {
                width = newWidth;
                height = newHeight;
                int cellCount = width * height;
                walkable = new bool[cellCount];
                rawBlockedBuffer = new bool[cellCount];
                visitedStamp = new int[cellCount];
                closedStamp = new int[cellCount];
                gScore = new float[cellCount];
                cameFrom = new int[cellCount];
                heapItems = new int[cellCount];
                heapKeys = new float[cellCount];
                heapPosition = new int[cellCount];
                currentStamp = 0;
            }

            BakeWalkability();
            hasBaked = true;
        }

        /// <summary>
        /// Computes a smoothed shortest path from start to end over the baked grid. Returns false
        /// if the grid has not baked yet or no route exists (result is cleared either way).
        /// </summary>
        public bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> resultWaypoints)
        {
            resultWaypoints.Clear();
            if (!hasBaked || walkable == null || walkable.Length == 0)
            {
                return false;
            }

            int startIndex = FindNearestWalkable(WorldToIndex(start));
            int endIndex = FindNearestWalkable(WorldToIndex(end));
            if (startIndex < 0 || endIndex < 0)
            {
                return false;
            }

            if (startIndex == endIndex)
            {
                resultWaypoints.Add(end);
                return true;
            }

            if (!RunAStar(startIndex, endIndex))
            {
                return false;
            }

            BuildRawPath(startIndex, endIndex, start, end, rawPathBuffer);
            SmoothPath(rawPathBuffer, smoothPathBuffer);
            resultWaypoints.AddRange(smoothPathBuffer);
            return resultWaypoints.Count > 0;
        }

        // ------------------------------------------------------------------ Baking

        private void ComputeAutoBounds()
        {
            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            bool any = false;
            Bounds combined = default;
            foreach (Collider candidate in colliders)
            {
                if (candidate == null || ((1 << candidate.gameObject.layer) & obstacleLayers.value) == 0)
                {
                    continue;
                }

                if (!any)
                {
                    combined = candidate.bounds;
                    any = true;
                }
                else
                {
                    combined.Encapsulate(candidate.bounds);
                }
            }

            if (!any)
            {
                return;
            }

            areaCenter = new Vector3(combined.center.x, areaCenter.y, combined.center.z);
            areaSize = new Vector2(combined.size.x + autoBoundsPadding * 2f,
                combined.size.z + autoBoundsPadding * 2f);
        }

        private void BakeWalkability()
        {
            Vector3 halfExtents = new(cellSize * 0.5f, probeHeight * 0.5f, cellSize * 0.5f);
            int blockedCells = 0;
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // The probe starts ABOVE the floor. Probing from y = 0 would hit the ground
                    // collider itself in every single cell and bake the entire grid as blocked.
                    Vector3 center = CellCenter(x, z) +
                                     Vector3.up * (probeGroundClearance + probeHeight * 0.5f);
                    int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapBuffer,
                        Quaternion.identity, obstacleLayers, QueryTriggerInteraction.Ignore);
                    bool blocked = hitCount > 0;
                    rawBlockedBuffer[z * width + x] = blocked;
                    if (blocked)
                    {
                        blockedCells++;
                    }
                }
            }

            WarnIfFullyBlocked(blockedCells);

            int dilationCells = Mathf.CeilToInt(agentRadius / cellSize);
            for (int i = 0; i < walkable.Length; i++)
            {
                walkable[i] = true;
            }

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!rawBlockedBuffer[z * width + x])
                    {
                        continue;
                    }

                    int minX = Mathf.Max(0, x - dilationCells);
                    int maxX = Mathf.Min(width - 1, x + dilationCells);
                    int minZ = Mathf.Max(0, z - dilationCells);
                    int maxZ = Mathf.Min(height - 1, z + dilationCells);
                    for (int nz = minZ; nz <= maxZ; nz++)
                    {
                        int row = nz * width;
                        for (int nx = minX; nx <= maxX; nx++)
                        {
                            walkable[row + nx] = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shouts once if the bake produced a grid nothing can walk on. Without this the failure is
        /// invisible: TryFindPath just returns false forever and every miner silently drops back to
        /// the old reactive steering, which looks like "pathfinding is bad" rather than
        /// "pathfinding never ran".
        /// </summary>
        private void WarnIfFullyBlocked(int blockedCells)
        {
            int total = width * height;
            if (total == 0 || blockedCells < total)
            {
                return;
            }

            Debug.LogError(
                $"[{nameof(MiningNavGrid)}] Every one of the {total} grid cells baked as blocked, so " +
                "no path can ever be found and miners fall back to reactive steering. The usual " +
                "cause is Obstacle Layers including the walkable floor's own layer - set it to the " +
                "ore/rock layers only, or raise Probe Ground Clearance above the floor's thickness.",
                this);
        }

        // ------------------------------------------------------------------ A*

        private bool RunAStar(int startIndex, int endIndex)
        {
            currentStamp++;
            heapCount = 0;

            gScore[startIndex] = 0f;
            visitedStamp[startIndex] = currentStamp;
            cameFrom[startIndex] = -1;
            HeapPush(startIndex, Heuristic(startIndex, endIndex));

            while (heapCount > 0)
            {
                int current = HeapPopMin();
                if (closedStamp[current] == currentStamp)
                {
                    continue;
                }

                closedStamp[current] = currentStamp;
                if (current == endIndex)
                {
                    return true;
                }

                int cx = current % width;
                int cz = current / width;

                foreach ((int dx, int dz, float cost) in Neighbors)
                {
                    int nx = cx + dx;
                    int nz = cz + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height)
                    {
                        continue;
                    }

                    int neighborIndex = nz * width + nx;
                    if (!walkable[neighborIndex] || closedStamp[neighborIndex] == currentStamp)
                    {
                        continue;
                    }

                    // Don't let a diagonal step cut through the corner of a blocked pair of
                    // orthogonal cells - the NPC's inflated capsule couldn't physically fit there.
                    if (dx != 0 && dz != 0 &&
                        (!walkable[cz * width + nx] || !walkable[nz * width + cx]))
                    {
                        continue;
                    }

                    float tentativeG = gScore[current] + cost * cellSize;
                    bool inOpen = visitedStamp[neighborIndex] == currentStamp;
                    if (inOpen && tentativeG >= gScore[neighborIndex])
                    {
                        continue;
                    }

                    gScore[neighborIndex] = tentativeG;
                    cameFrom[neighborIndex] = current;
                    visitedStamp[neighborIndex] = currentStamp;
                    float f = tentativeG + Heuristic(neighborIndex, endIndex);
                    if (inOpen)
                    {
                        HeapDecreaseKey(neighborIndex, f);
                    }
                    else
                    {
                        HeapPush(neighborIndex, f);
                    }
                }
            }

            return false;
        }

        private float Heuristic(int a, int b)
        {
            int ax = a % width, az = a / width;
            int bx = b % width, bz = b / width;
            int dx = Mathf.Abs(ax - bx);
            int dz = Mathf.Abs(az - bz);
            return (Mathf.Max(dx, dz) + 0.41421356f * Mathf.Min(dx, dz)) * cellSize;
        }

        private void BuildRawPath(int startIndex, int endIndex, Vector3 start, Vector3 end,
            List<Vector3> result)
        {
            result.Clear();
            int node = endIndex;
            int safety = width * height + 1;
            while (node != -1 && safety-- > 0)
            {
                result.Add(node == endIndex ? end : node == startIndex ? start : CellCenter(node));
                node = node == startIndex ? -1 : cameFrom[node];
            }

            result.Reverse();
        }

        /// <summary>
        /// Turns the raw cell-by-cell A* result into an any-angle route by string-pulling: keep a
        /// node only when the straight line from the last kept node to the node after it is
        /// blocked. Repeated until it stops shrinking, because one pass leaves corners that only
        /// become cuttable once earlier corners are gone - that leftover staircase is what makes
        /// grid paths look longer and more jagged than the true shortest route.
        /// </summary>
        private void SmoothPath(List<Vector3> rawPath, List<Vector3> result)
        {
            result.Clear();
            if (rawPath.Count == 0)
            {
                return;
            }

            if (rawPath.Count <= 2)
            {
                result.AddRange(rawPath);
                return;
            }

            StringPull(rawPath, result);

            // Each extra pass costs one line-of-sight walk per remaining node, and the node count
            // drops fast, so the cap is just a safety net rather than a real limit.
            for (int pass = 0; pass < 3 && result.Count > 2; pass++)
            {
                smoothPassBuffer.Clear();
                smoothPassBuffer.AddRange(result);
                StringPull(smoothPassBuffer, result);
                if (result.Count == smoothPassBuffer.Count)
                {
                    break;
                }
            }
        }

        private void StringPull(List<Vector3> source, List<Vector3> result)
        {
            result.Clear();
            result.Add(source[0]);
            int anchor = 0;
            for (int i = 1; i < source.Count - 1; i++)
            {
                if (!HasClearGridLine(source[anchor], source[i + 1]))
                {
                    result.Add(source[i]);
                    anchor = i;
                }
            }

            result.Add(source[^1]);
        }

        /// <summary>
        /// Exact line-of-sight between two world points over the walkable grid, used by the
        /// string-pulling in <see cref="SmoothPath"/> to decide whether a corner can be cut.
        ///
        /// This walks the grid cell by cell (Amanatides and Woo voxel traversal) instead of
        /// sampling points along the line. Point sampling - the previous approach - had two
        /// real failure modes that both show up as a miner shortcutting into a rock:
        ///   1. It stepped half a cell at a time, so a line crossing the corner region of a
        ///      blocked cell could pass between two samples and be reported clear.
        ///   2. It resolved samples through the clamping WorldToIndex, so a shortcut that left
        ///      the baked area entirely got clamped onto an edge cell; if that edge cell happened
        ///      to be walkable the whole off-grid segment was reported clear.
        /// Leaving the grid is now treated as blocked: unbaked space is unknown, not free.
        /// </summary>
        private bool HasClearGridLine(Vector3 a, Vector3 b)
        {
            if (!TryWorldToCell(a, out int x, out int z) ||
                !TryWorldToCell(b, out int endX, out int endZ))
            {
                return false;
            }

            if (!walkable[z * width + x] || !walkable[endZ * width + endX])
            {
                return false;
            }

            // Continuous cell-space coordinates of the segment.
            float fromX = (a.x - origin.x) / cellSize;
            float fromZ = (a.z - origin.z) / cellSize;
            float deltaX = (b.x - origin.x) / cellSize - fromX;
            float deltaZ = (b.z - origin.z) / cellSize - fromZ;

            int stepX = deltaX > 0f ? 1 : deltaX < 0f ? -1 : 0;
            int stepZ = deltaZ > 0f ? 1 : deltaZ < 0f ? -1 : 0;

            // Distance along the segment to the next cell boundary on each axis, and how much
            // more distance each subsequent boundary costs.
            float nextX = stepX > 0 ? x + 1 : x;
            float nextZ = stepZ > 0 ? z + 1 : z;
            float maxX = stepX != 0 ? (nextX - fromX) / deltaX : float.PositiveInfinity;
            float maxZ = stepZ != 0 ? (nextZ - fromZ) / deltaZ : float.PositiveInfinity;
            float deltaStepX = stepX != 0 ? stepX / deltaX : float.PositiveInfinity;
            float deltaStepZ = stepZ != 0 ? stepZ / deltaZ : float.PositiveInfinity;

            int guard = width + height + 2;
            while (guard-- > 0)
            {
                if (x == endX && z == endZ)
                {
                    return true;
                }

                if (Mathf.Abs(maxX - maxZ) <= 0.0001f && stepX != 0 && stepZ != 0)
                {
                    // The segment passes exactly through a cell corner. Refuse to squeeze
                    // diagonally between two blocked cells - same rule the A* itself applies to
                    // diagonal steps, so smoothing can't undo it.
                    if (!walkable[z * width + x + stepX] ||
                        !walkable[(z + stepZ) * width + x])
                    {
                        return false;
                    }

                    x += stepX;
                    z += stepZ;
                    maxX += deltaStepX;
                    maxZ += deltaStepZ;
                }
                else if (maxX < maxZ)
                {
                    x += stepX;
                    maxX += deltaStepX;
                }
                else
                {
                    z += stepZ;
                    maxZ += deltaStepZ;
                }

                if (x < 0 || x >= width || z < 0 || z >= height || !walkable[z * width + x])
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Non-clamping world-to-cell conversion. <see cref="WorldToIndex"/> clamps, which is
        /// right when snapping a start/end point onto the grid but wrong for line-of-sight,
        /// where a point outside the grid must be reported as outside rather than silently
        /// pulled onto the nearest edge cell.
        /// </summary>
        private bool TryWorldToCell(Vector3 world, out int x, out int z)
        {
            x = Mathf.FloorToInt((world.x - origin.x) / cellSize);
            z = Mathf.FloorToInt((world.z - origin.z) / cellSize);
            return x >= 0 && x < width && z >= 0 && z < height;
        }

        // ------------------------------------------------------------------ Grid helpers

        private int WorldToIndex(Vector3 world)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((world.x - origin.x) / cellSize), 0, width - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((world.z - origin.z) / cellSize), 0, height - 1);
            return z * width + x;
        }

        private Vector3 CellCenter(int index)
        {
            return CellCenter(index % width, index / width);
        }

        private Vector3 CellCenter(int x, int z)
        {
            return new Vector3(origin.x + (x + 0.5f) * cellSize, origin.y,
                origin.z + (z + 0.5f) * cellSize);
        }

        private int FindNearestWalkable(int index)
        {
            if (walkable[index])
            {
                return index;
            }

            int cx = index % width;
            int cz = index / width;
            int maxRadius = Mathf.Max(width, height);
            for (int r = 1; r <= maxRadius; r++)
            {
                int minX = cx - r, maxX = cx + r, minZ = cz - r, maxZ = cz + r;
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (z < 0 || z >= height)
                    {
                        continue;
                    }

                    bool edgeRow = z == minZ || z == maxZ;
                    int step = edgeRow ? 1 : Mathf.Max(1, maxX - minX);
                    for (int x = minX; x <= maxX; x += step)
                    {
                        if (x < 0 || x >= width)
                        {
                            continue;
                        }

                        int idx = z * width + x;
                        if (walkable[idx])
                        {
                            return idx;
                        }
                    }
                }
            }

            return -1;
        }

        // ------------------------------------------------------------------ Binary min-heap

        private void HeapPush(int cellIndex, float key)
        {
            int pos = heapCount++;
            heapItems[pos] = cellIndex;
            heapKeys[pos] = key;
            heapPosition[cellIndex] = pos;
            SiftUp(pos);
        }

        private int HeapPopMin()
        {
            int rootItem = heapItems[0];
            heapCount--;
            if (heapCount > 0)
            {
                heapItems[0] = heapItems[heapCount];
                heapKeys[0] = heapKeys[heapCount];
                heapPosition[heapItems[0]] = 0;
                SiftDown(0);
            }

            return rootItem;
        }

        private void HeapDecreaseKey(int cellIndex, float newKey)
        {
            int pos = heapPosition[cellIndex];
            if (newKey >= heapKeys[pos])
            {
                return;
            }

            heapKeys[pos] = newKey;
            SiftUp(pos);
        }

        private void SiftUp(int pos)
        {
            while (pos > 0)
            {
                int parent = (pos - 1) / 2;
                if (heapKeys[parent] <= heapKeys[pos])
                {
                    break;
                }

                SwapHeap(parent, pos);
                pos = parent;
            }
        }

        private void SiftDown(int pos)
        {
            while (true)
            {
                int left = pos * 2 + 1;
                int right = left + 1;
                int smallest = pos;
                if (left < heapCount && heapKeys[left] < heapKeys[smallest])
                {
                    smallest = left;
                }

                if (right < heapCount && heapKeys[right] < heapKeys[smallest])
                {
                    smallest = right;
                }

                if (smallest == pos)
                {
                    break;
                }

                SwapHeap(pos, smallest);
                pos = smallest;
            }
        }

        private void SwapHeap(int a, int b)
        {
            (heapItems[a], heapItems[b]) = (heapItems[b], heapItems[a]);
            (heapKeys[a], heapKeys[b]) = (heapKeys[b], heapKeys[a]);
            heapPosition[heapItems[a]] = a;
            heapPosition[heapItems[b]] = b;
        }

        // ------------------------------------------------------------------ Debug

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || walkable == null)
            {
                return;
            }

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = z * width + x;
                    Gizmos.color = walkable[index]
                        ? new Color(0f, 1f, 0f, 0.15f)
                        : new Color(1f, 0f, 0f, 0.35f);
                    Gizmos.DrawCube(CellCenter(x, z), new Vector3(cellSize, 0.05f, cellSize));
                }
            }
        }
    }
}