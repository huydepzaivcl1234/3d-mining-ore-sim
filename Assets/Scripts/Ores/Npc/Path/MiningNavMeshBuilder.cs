using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Owns the runtime NavMesh for the mining level.
    ///
    /// Design note - why this almost never rebuilds:
    /// Ores are NOT baked into or carved out of the NavMesh. Before every build they receive a
    /// <see cref="NavMeshModifier"/> with Ignore From Build enabled. The ground underneath remains
    /// the only baked surface, while a compact circular <see cref="MiningNavMeshObstacle"/> gives
    /// shortest-path queries a clean footprint to route around. This avoids rectangular holes and
    /// prevents the top of a rock from becoming walkable.
    ///
    /// The explicit rebuild path below is for the walkable floor itself changing (a new area
    /// unlocked, a rebirth layout swap, or a portal room opening).
    /// Call <see cref="RequestRebuild"/> for those; it coalesces bursts of requests into one bake.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(NavMeshSurface))]
    public sealed class MiningNavMeshBuilder : MonoBehaviour
    {
        public static MiningNavMeshBuilder Instance { get; private set; }

        [Header("Surface")]
        [Tooltip("The surface describing the walkable floor. Leave empty to use the NavMeshSurface on this GameObject.")]
        [SerializeField] private NavMeshSurface surface;
        [Tooltip("Optional scene-authored surface on the UnderGround root. It is resolved or " +
                 "added to that existing root at runtime when the portal first opens.")]
        [SerializeField] private NavMeshSurface undergroundSurface;
        [Tooltip("Bakes the surface once on Start. Turn this off if you bake the surface in the editor and your floor never changes at runtime - the baked data is then used as-is.")]
        [SerializeField] private bool buildOnStart = true;

        [Header("Rebuild Throttling")]
        [Tooltip("Requests arriving within this window are merged into a single bake, so a burst of level changes in one frame doesn't trigger many bakes.")]
        [Min(0f)] [SerializeField] private float rebuildCoalesceDelay = 0.1f;
        [Tooltip("Hard floor on the time between two bakes. A full bake stalls the main thread, so keep this comfortably large.")]
        [Min(0f)] [SerializeField] private float minimumSecondsBetweenBuilds = 1f;

        private float lastBuildTime = float.NegativeInfinity;
        private Coroutine pendingRebuild;
        private NavMeshSurface pendingSurface;

        /// <summary>True once a NavMesh exists and can answer path queries.</summary>
        public bool HasNavMesh { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            if (surface == null)
            {
                surface = GetComponent<NavMeshSurface>();
            }

            ConfigureRuntimeBuildGeometry(surface);
            if (undergroundSurface != null)
            {
                ConfigureUndergroundSurface(undergroundSurface);
            }
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
            // If the surface was baked in the editor, NavMesh data is already loaded and there is
            // nothing to do - re-baking identical geometry on every play would just cost a stall.
            if (buildOnStart)
            {
                Build();
            }
            else
            {
                HasNavMesh = HasNavMeshData();
            }
        }

        /// <summary>Queues a rebuild, merging any requests that arrive in the same short window.</summary>
        public void RequestRebuild()
        {
            QueueRebuild(surface);
        }

        /// <summary>
        /// Builds navigation on an existing designer-authored area root. No platform, spawner,
        /// miner, or hierarchy object is created; only a missing NavMeshSurface component may be
        /// added to the supplied root.
        /// </summary>
        public void RequestRebuildForRoot(GameObject areaRoot)
        {
            if (areaRoot == null)
            {
                Debug.LogWarning("Cannot build Underground NavMesh without an authored area root.",
                    this);
                return;
            }

            undergroundSurface = areaRoot.GetComponent<NavMeshSurface>() ??
                                 areaRoot.GetComponentInChildren<NavMeshSurface>(true);
            if (undergroundSurface == null)
            {
                undergroundSurface = areaRoot.AddComponent<NavMeshSurface>();
            }

            ConfigureUndergroundSurface(undergroundSurface);
            QueueRebuild(undergroundSurface);
        }

        private void QueueRebuild(NavMeshSurface targetSurface)
        {
            if (targetSurface == null || !isActiveAndEnabled)
            {
                return;
            }

            // The newest area request wins while a short coalescing delay is active.
            pendingSurface = targetSurface;
            if (pendingRebuild == null)
            {
                pendingRebuild = StartCoroutine(RebuildAfterDelay());
            }
        }

        private IEnumerator RebuildAfterDelay()
        {
            yield return new WaitForSeconds(rebuildCoalesceDelay);

            float earliestBuild = lastBuildTime + minimumSecondsBetweenBuilds;
            if (Time.time < earliestBuild)
            {
                yield return new WaitForSeconds(earliestBuild - Time.time);
            }

            NavMeshSurface targetSurface = pendingSurface != null ? pendingSurface : surface;
            pendingSurface = null;
            pendingRebuild = null;
            Build(targetSurface);
        }

        /// <summary>Bakes the surface immediately. Prefer <see cref="RequestRebuild"/> in gameplay code.</summary>
        public void Build()
        {
            Build(surface);
        }

        private void Build(NavMeshSurface targetSurface)
        {
            if (targetSurface == null)
            {
                Debug.LogWarning($"{nameof(MiningNavMeshBuilder)} has no NavMeshSurface assigned.", this);
                return;
            }

            ConfigureRuntimeBuildGeometry(targetSurface);
            lastBuildTime = Time.time;

            // UpdateNavMesh reuses the existing NavMeshData instance, so agents and in-flight
            // path queries keep working across the swap. BuildNavMesh would allocate fresh data
            // and momentarily invalidate them.
            if (targetSurface.navMeshData != null)
            {
                targetSurface.UpdateNavMesh(targetSurface.navMeshData);
            }
            else
            {
                targetSurface.BuildNavMesh();
            }

            HasNavMesh = HasNavMeshData();
        }

        private static void ConfigureUndergroundSurface(NavMeshSurface targetSurface)
        {
            if (targetSurface == null)
            {
                return;
            }

            // The user's UnderGround root owns the platform. Children collection prevents the
            // underground bake from pulling Ground or other unrelated scene geometry into it.
            targetSurface.collectObjects = CollectObjects.Children;
            targetSurface.layerMask = ~0;
            ConfigureRuntimeBuildGeometry(targetSurface);
        }

        private static void ConfigureRuntimeBuildGeometry(NavMeshSurface targetSurface)
        {
            if (targetSurface == null)
            {
                return;
            }

            ExcludeMineablesFromBuild();

            // Player builds cannot read every imported render mesh. The playable ground
            // already has colliders, so baking from them prevents unreadable ore meshes
            // from breaking the runtime NavMesh update.
            targetSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }

        /// <summary>
        /// Keeps dynamic rocks out of source collection. Marking them Not Walkable would still
        /// create a hole; ignoring their hierarchy lets the floor collider underneath be baked.
        /// This runs again before every rebuild so newly spawned mineables are covered too.
        /// </summary>
        private static void ExcludeMineablesFromBuild()
        {
            foreach (Ore ore in Object.FindObjectsByType<Ore>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                EnsureIgnoredByNavMeshBuild(ore.gameObject);
            }

            foreach (LuckyBlock luckyBlock in Object.FindObjectsByType<LuckyBlock>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                EnsureIgnoredByNavMeshBuild(luckyBlock.gameObject);
            }
        }

        private static void EnsureIgnoredByNavMeshBuild(GameObject target)
        {
            NavMeshModifier modifier = target.GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = target.AddComponent<NavMeshModifier>();
            }

            modifier.enabled = true;
            modifier.overrideArea = false;
            modifier.ignoreFromBuild = true;

            MiningNavMeshObstacle obstacle = target.GetComponent<MiningNavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = target.AddComponent<MiningNavMeshObstacle>();
            }

            // A few old prefab variants may still contain an additional box obstacle. Disable
            // every legacy obstacle first, then let the single owner below re-enable its capsule.
            foreach (NavMeshObstacle legacyObstacle in
                     target.GetComponentsInChildren<NavMeshObstacle>(true))
            {
                legacyObstacle.carving = false;
                legacyObstacle.enabled = false;
            }

            obstacle.EnableCircularCarving();
        }

        private static bool HasNavMeshData()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            return triangulation.indices != null && triangulation.indices.Length > 0;
        }
    }
}
