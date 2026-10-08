using Unity.AI.Navigation;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Existing scene bootstrap, retained by GUID. NavMeshSurface still defines mining/spawn bounds.</summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(NavMeshSurface))]
    public sealed class MiningNavMeshBuilder : MonoBehaviour
    {
        public static MiningNavMeshBuilder Instance { get; private set; }
        // Keep scene serialization intact; the runtime route backend is now Terrain A*.
        [SerializeField, HideInInspector] private NavMeshSurface surface;
        [SerializeField, HideInInspector] private bool buildOnStart = true;
        [SerializeField, HideInInspector] private float rebuildCoalesceDelay = .1f, minimumSecondsBetweenBuilds = 1;
        [Header("Terrain A* settings")]
        [Min(.1f)] [SerializeField] private float gridCellSize = .5f;
        [Min(.05f)] [SerializeField] private float gridActorRadius = .55f;
        [Min(.1f)] [SerializeField] private float gridActorHeight = 1.8f;
        [Range(0, 89)] [SerializeField] private float gridMaximumSlope = 45;
        [Min(.01f)] [SerializeField] private float gridMaximumStep = .5f;
        [SerializeField] private LayerMask gridObstacleLayers = ~0;
        [Tooltip("Opt in only if these bounds include monster spawn/entry routes as well as the mining field.")]
        [SerializeField] private bool useSurfaceBounds;
        [Min(1)] [SerializeField] private int gridBakeBudget = 512, gridSearchBudget = 256;
        private MiningNavGrid grid;
        public float AdditionalMinerPadding => 0;
        public bool HasNavMesh => false;
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            surface = GetComponent<NavMeshSurface>();
            grid = GetComponent<MiningNavGrid>();
            if (grid == null)
            {
                grid = gameObject.AddComponent<MiningNavGrid>();
                grid.Configure(gridCellSize, gridActorRadius, gridActorHeight, gridMaximumSlope,
                    gridMaximumStep, gridObstacleLayers, gridBakeBudget, gridSearchBudget);
                Vector3 center = surface.transform.TransformPoint(surface.center);
                Vector3 size = Vector3.Scale(surface.size, surface.transform.lossyScale);
                if (useSurfaceBounds) grid.ConfigureArea(center, new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.z)));
            }
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void EnsureMinerClearance(float radius) => grid?.EnsureClearance(radius);
        public void RequestRebuild() => grid?.RequestFullRebake();
        public void Build() => RequestRebuild();
    }
}
