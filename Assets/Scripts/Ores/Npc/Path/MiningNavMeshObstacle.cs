using UnityEngine;
using UnityEngine.AI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Represents a mineable as a compact circular obstacle for global NavMesh path queries.
    /// Mineable geometry is excluded from the bake separately, so this capsule only carves a
    /// small round footprint in the ground instead of a rectangular box or a walkable rock top.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshObstacle))]
    public sealed class MiningNavMeshObstacle : MonoBehaviour
    {
        [Tooltip("Small world-space clearance added outside the ore collider footprint.")]
        [SerializeField, Min(0f)] private float radiusPadding = 0.04f;
        [Tooltip("Prevents tiny ores from producing an unusably small path obstacle.")]
        [SerializeField, Min(0.05f)] private float minimumRadius = 0.2f;
        [Tooltip("Minimum capsule height used by the NavMesh obstacle.")]
        [SerializeField, Min(0.1f)] private float minimumHeight = 0.5f;

        private NavMeshObstacle obstacle;

        private void Awake()
        {
            obstacle = GetComponent<NavMeshObstacle>();
            EnableCircularCarving();
        }

        private void OnEnable()
        {
            EnableCircularCarving();
        }

        private void OnDisable()
        {
            if (obstacle != null)
            {
                obstacle.enabled = false;
            }
        }

        /// <summary>
        /// Configures and enables the circular carve. Safe to call repeatedly for pooled ores.
        /// </summary>
        public void EnableCircularCarving()
        {
            if (obstacle == null)
            {
                obstacle = GetComponent<NavMeshObstacle>();
            }

            if (obstacle == null)
            {
                return;
            }

            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            // Hit-punch only lifts the ore by about 0.12 units. A larger threshold keeps that
            // cosmetic motion from repeatedly removing and rebuilding the carved circle.
            obstacle.carvingMoveThreshold = 0.5f;
            obstacle.carvingTimeToStationary = 0.1f;
            RefreshSizeFromColliders();
            obstacle.enabled = true;
        }

        private void RefreshSizeFromColliders()
        {
            if (!TryGetWorldBounds(out Bounds bounds))
            {
                return;
            }

            obstacle.center = transform.InverseTransformPoint(bounds.center);
            Vector3 scale = transform.lossyScale;
            float horizontalScale = Mathf.Max(0.0001f,
                Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
            float verticalScale = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
            float worldRadius = Mathf.Max(minimumRadius,
                Mathf.Max(bounds.extents.x, bounds.extents.z) + radiusPadding);
            obstacle.radius = worldRadius / horizontalScale;
            float worldHeight = Mathf.Max(minimumHeight, bounds.size.y, worldRadius * 2f);
            obstacle.height = worldHeight / verticalScale;
        }

        private bool TryGetWorldBounds(out Bounds bounds)
        {
            Ore ore = GetComponentInParent<Ore>();
            if (ore != null && ore.TryGetWorldBounds(out bounds))
            {
                return true;
            }

            bounds = default;
            bool hasBounds = false;
            foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
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
        private void OnValidate()
        {
            radiusPadding = Mathf.Max(0f, radiusPadding);
            minimumRadius = Mathf.Max(0.05f, minimumRadius);
            minimumHeight = Mathf.Max(0.1f, minimumHeight);

            NavMeshObstacle target = GetComponent<NavMeshObstacle>();
            if (target == null)
            {
                return;
            }

            obstacle = target;
            target.shape = NavMeshObstacleShape.Capsule;
            target.carving = true;
            target.carveOnlyStationary = true;
            target.carvingMoveThreshold = 0.5f;
            target.carvingTimeToStationary = 0.1f;
            RefreshSizeFromColliders();
            target.enabled = true;
        }
#endif
    }
}
