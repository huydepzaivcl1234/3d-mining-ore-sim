using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>A reusable world-space disk. Progress is driven by the attack's normalized contact frame.</summary>
    public sealed class MonsterAttackWarning : MonoBehaviour
    {
        private GameObject disk;
        private Material material;
        private static readonly int Progress = Shader.PropertyToID("_Progress");
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        public void Show(Vector3 center, float radius, Color color, Shader shader, Transform owner)
        {
            if (shader == null) return;
            if (disk == null)
            {
                disk = GameObject.CreatePrimitive(PrimitiveType.Quad);
                disk.name = "Monster Area Warning (world space)";
                Destroy(disk.GetComponent<Collider>());
                material = new Material(shader);
                var renderer = disk.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            // Ignore the monster and other actors: the marker belongs on ground, never on a head.
            int count = Physics.RaycastNonAlloc(center + Vector3.up * 3f, Vector3.down, groundHits, 8f, ~0, QueryTriggerInteraction.Ignore);
            float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.transform.IsChildOf(owner) || hit.collider.GetComponentInParent<MiningCharacterHealth>() != null || hit.collider.GetComponentInParent<MiningNpc>() != null || hit.normal.y < .6f) continue;
                if (hit.distance < distance) { distance = hit.distance; center.y = hit.point.y; }
            }
            disk.transform.SetPositionAndRotation(center + Vector3.up * .035f, Quaternion.Euler(90f, 0f, 0f));
            disk.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            material.SetColor("_Color", color);
            SetProgress(0f);
            disk.SetActive(true);
        }
        public void SetProgress(float progress) { if (material != null) material.SetFloat(Progress, Mathf.Clamp01(progress)); }
        public void Hide() { if (disk != null) disk.SetActive(false); }
        private void OnDisable() => Hide();
        private void OnDestroy()
        {
            if (disk != null) Destroy(disk);
            if (material != null) Destroy(material);
        }
    }
}
