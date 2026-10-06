using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class ForestGolemImpactEmitter : MonoBehaviour
    {
        [SerializeField] private ForestGrassShockwave impactPrefab;
        [Tooltip("Optional point between the fists. If empty, use the golem's root position.")]
        [SerializeField] private Transform impactPoint;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(.1f)] private float rayHeight = 2f;
        [SerializeField, Min(.1f)] private float rayDistance = 6f;
        private readonly RaycastHit[] hits = new RaycastHit[32];

        // Add an Animation Event with this name at the ground-contact frame of the slam.
        public void GroundSlamImpact()
        {
            if (impactPrefab == null) return;
            Vector3 position = impactPoint != null ? impactPoint.position : transform.position;
            Vector3 normal = Vector3.up;
            int count = Physics.RaycastNonAlloc(position + Vector3.up * rayHeight, Vector3.down,
                hits, rayHeight + rayDistance, groundLayers, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.normal.y <= .2f || hit.distance >= closest) continue;
                closest = hit.distance; position = hit.point; normal = hit.normal;
            }
            var effect = Instantiate(impactPrefab, position, Quaternion.FromToRotation(Vector3.up, normal));
            effect.Play();
        }
    }
}
