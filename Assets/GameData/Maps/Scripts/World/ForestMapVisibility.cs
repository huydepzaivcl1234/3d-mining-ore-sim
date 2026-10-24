using UnityEngine;
namespace MiningSimulator.Ores
{
    /// <summary>Decoration-only distance culling; AI, collisions and normal frustum/LOD culling remain active.</summary>
    public sealed class ForestMapVisibility : MonoBehaviour
    {
        [Min(15)] public float distance = 65;
        [Min(1)] public float hysteresis = 6;
        [Min(.05f)] public float interval = .2f;
        public Transform decoration;
        private Renderer[] items;
        private bool[] original;
        private Camera view;
        private float next;
        private void OnEnable()
        {
            items = decoration != null ? decoration.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            original = new bool[items.Length];
            for (int i = 0; i < items.Length; i++) original[i] = items[i].forceRenderingOff;
        }
        private void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + interval;
            if (view == null || !view.isActiveAndEnabled) view = Camera.main;
            if (view == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                var r = items[i]; if (r == null || original[i]) continue;
                float threshold = distance + (r.forceRenderingOff ? 0 : hysteresis);
                r.forceRenderingOff = r.bounds.SqrDistance(view.transform.position) > threshold * threshold;
            }
        }
        private void OnDisable()
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++) if (items[i] != null) items[i].forceRenderingOff = original[i];
        }
    }
}
