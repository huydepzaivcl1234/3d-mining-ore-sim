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
    /// Ores are NOT baked into the NavMesh. They carve it at runtime through
    /// <see cref="MiningNavMeshObstacle"/> (NavMeshObstacle with Carve enabled). Carving is
    /// applied by Unity every frame on a background job and is far cheaper than a surface
    /// rebuild, so ores can spawn, deplete, and return to the pool freely without ever
    /// triggering a bake. This surface therefore only needs to cover the *static* geometry -
    /// the floor/terrain the miners walk on.
    ///
    /// The explicit rebuild path below exists for the cases carving can't cover: the walkable
    /// floor itself changing (a new area unlocked, a rebirth layout swap, a portal room opening).
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
        [Tooltip("Bakes the surface once on Start. Turn this off if you bake the surface in the editor and your floor never changes at runtime - the baked data is then used as-is.")]
        [SerializeField] private bool buildOnStart = true;

        [Header("Rebuild Throttling")]
        [Tooltip("Requests arriving within this window are merged into a single bake, so a burst of level changes in one frame doesn't trigger many bakes.")]
        [Min(0f)] [SerializeField] private float rebuildCoalesceDelay = 0.1f;
        [Tooltip("Hard floor on the time between two bakes. A full bake stalls the main thread, so keep this comfortably large.")]
        [Min(0f)] [SerializeField] private float minimumSecondsBetweenBuilds = 1f;

        private float lastBuildTime = float.NegativeInfinity;
        private Coroutine pendingRebuild;

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

            ConfigureRuntimeBuildGeometry();
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
            if (pendingRebuild != null || !isActiveAndEnabled)
            {
                return;
            }

            pendingRebuild = StartCoroutine(RebuildAfterDelay());
        }

        private IEnumerator RebuildAfterDelay()
        {
            yield return new WaitForSeconds(rebuildCoalesceDelay);

            float earliestBuild = lastBuildTime + minimumSecondsBetweenBuilds;
            if (Time.time < earliestBuild)
            {
                yield return new WaitForSeconds(earliestBuild - Time.time);
            }

            pendingRebuild = null;
            Build();
        }

        /// <summary>Bakes the surface immediately. Prefer <see cref="RequestRebuild"/> in gameplay code.</summary>
        public void Build()
        {
            if (surface == null)
            {
                Debug.LogWarning($"{nameof(MiningNavMeshBuilder)} has no NavMeshSurface assigned.", this);
                return;
            }

            ConfigureRuntimeBuildGeometry();
            lastBuildTime = Time.time;

            // UpdateNavMesh reuses the existing NavMeshData instance, so agents and in-flight
            // path queries keep working across the swap. BuildNavMesh would allocate fresh data
            // and momentarily invalidate them.
            if (surface.navMeshData != null)
            {
                surface.UpdateNavMesh(surface.navMeshData);
            }
            else
            {
                surface.BuildNavMesh();
            }

            HasNavMesh = HasNavMeshData();
        }

        private void ConfigureRuntimeBuildGeometry()
        {
            if (surface == null)
            {
                return;
            }

            // Player builds cannot read every imported render mesh. The playable ground
            // already has colliders, so baking from them prevents unreadable ore meshes
            // from breaking the runtime NavMesh update.
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }

        private static bool HasNavMeshData()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            return triangulation.indices != null && triangulation.indices.Length > 0;
        }
    }
}
