using UnityEngine;
using UnityEngine.AI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Makes an ore (or Lucky Block) cut a hole in the NavMesh while it exists, so every path
    /// query routes around it automatically.
    ///
    /// This is the piece that makes NavMesh viable for this game. Ores spawn, get mined out, and
    /// go back into a pool constantly - baking them into the surface would mean a NavMesh rebuild
    /// per ore, which is far too expensive. A carving NavMeshObstacle instead punches the hole at
    /// runtime, cheaply, and Unity automatically invalidates and re-routes agent paths that ran
    /// through the carved region.
    ///
    /// Put this on the ore prefab. It sizes itself from the ore's colliders and disables carving
    /// the moment the ore is depleted or pooled, so a pooled (invisible) ore never keeps blocking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshObstacle))]
    public sealed class MiningNavMeshObstacle : MonoBehaviour
    {
        [Tooltip("Extra clearance added around the ore's bounds when carving, so miners don't clip the rock while walking past it. Roughly the NPC capsule radius is a good starting point.")]
        [Min(0f)] [SerializeField] private float carvePadding = 0.25f;
        [Tooltip("Re-measures the ore's bounds every time it is enabled. Needed if pooled ores are re-scaled or swap meshes between ore types; can be turned off for a fixed-size prefab.")]
        [SerializeField] private bool resizeOnEnable = true;

        private NavMeshObstacle obstacle;
        private Ore ore;
        private LuckyBlock luckyBlock;
        private bool measured;

        private void Awake()
        {
            obstacle = GetComponent<NavMeshObstacle>();
            ore = GetComponentInParent<Ore>();
            luckyBlock = GetComponentInParent<LuckyBlock>();

            obstacle.shape = NavMeshObstacleShape.Box;
            // Carving is what actually cuts the NavMesh. Without it the obstacle only pushes
            // agents aside locally, which is exactly the reactive behaviour we're replacing.
            obstacle.carving = true;
            // These ores don't move once placed, so let Unity carve once and stay still rather
            // than re-carving as the object jitters from hit-punch tweens.
            obstacle.carveOnlyStationary = true;
        }

        private void OnEnable()
        {
            if (resizeOnEnable || !measured)
            {
                Measure();
            }

            obstacle.enabled = true;
        }

        private void OnDisable()
        {
            // A pooled or destroyed ore must stop carving immediately, otherwise the hole
            // outlives the rock and miners keep walking around empty ground.
            if (obstacle != null)
            {
                obstacle.enabled = false;
            }
        }

        private void Update()
        {
            // Depleted ores are often kept alive for a frame or two (drop FX, pool return delay).
            // Stop blocking as soon as the rock is logically gone, not when the object disappears.
            bool shouldBlock = (ore == null || !ore.IsDepleted) &&
                               (luckyBlock == null || !luckyBlock.IsResolved);
            if (obstacle.enabled != shouldBlock)
            {
                obstacle.enabled = shouldBlock;
            }
        }

        /// <summary>Re-reads the ore's collider bounds and resizes the carve box to match.</summary>
        public void Measure()
        {
            if (!TryGetBounds(out Bounds bounds))
            {
                return;
            }

            // NavMeshObstacle center/size are in the obstacle's local space.
            obstacle.center = transform.InverseTransformPoint(bounds.center);
            Vector3 lossyScale = transform.lossyScale;
            Vector3 worldSize = bounds.size + new Vector3(carvePadding * 2f, 0f, carvePadding * 2f);
            obstacle.size = new Vector3(
                worldSize.x / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x)),
                worldSize.y / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y)),
                worldSize.z / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z)));
            measured = true;
        }

        private bool TryGetBounds(out Bounds bounds)
        {
            if (ore != null && ore.TryGetWorldBounds(out bounds))
            {
                return true;
            }

            bounds = default;
            bool hasBounds = false;
            foreach (Collider candidate in GetComponentsInChildren<Collider>())
            {
                if (candidate == null || !candidate.enabled || candidate.isTrigger)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = candidate.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(candidate.bounds);
                }
            }

            return hasBounds;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-time mirror of the <see cref="Awake"/> setup. Awake only ever runs on a live
        /// instantiated object, so without this the prefab asset keeps showing Unity's raw
        /// component defaults - Carve unchecked - even though this component's whole point is
        /// that carving is always on. That made the component look broken when inspected in the
        /// Project window, and left the actual behaviour depending on Awake never being missed.
        /// Writing the flags here keeps the Inspector honest and the asset correct on disk.
        /// </summary>
        private void OnValidate()
        {
            NavMeshObstacle target = GetComponent<NavMeshObstacle>();
            if (target == null ||
                (target.shape == NavMeshObstacleShape.Box && target.carving &&
                 target.carveOnlyStationary))
            {
                return;
            }

            target.shape = NavMeshObstacleShape.Box;
            target.carving = true;
            target.carveOnlyStationary = true;
            UnityEditor.EditorUtility.SetDirty(target);
        }
#endif
    }
}
