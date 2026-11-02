using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Short allocation-free sparkle burst used when the Gem balance increases.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGemSparkleGraphic : MaskableGraphic
    {
        [Min(0.1f), SerializeField] private float duration = 0.85f;
        private float elapsed = 1f;
        private int sparkleCount = 8;

        public void PlayBurst(bool strong)
        {
            sparkleCount = strong ? 16 : 8;
            elapsed = 0f;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            if (elapsed >= duration)
            {
                return;
            }
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (elapsed >= duration)
            {
                return;
            }

            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));
            float travel = 1f - Mathf.Pow(1f - t, 2f);
            float alpha = 1f - t;
            Rect bounds = rectTransform.rect;
            Vector2 origin = new(bounds.xMin + bounds.width * 0.18f, bounds.center.y);
            for (int index = 0; index < sparkleCount; index++)
            {
                float seed = Hash(index + 1);
                float angle = Mathf.Lerp(-0.35f, Mathf.PI * 2.15f, seed);
                float distance = Mathf.Lerp(bounds.height * 0.34f,
                    bounds.height * 1.35f, Hash(index + 41)) * travel;
                Vector2 center = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                float radius = Mathf.Lerp(1.8f, 4.2f, Hash(index + 81)) *
                               Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                Color tint = Color.Lerp(new Color(1f, 0.55f, 1f, alpha),
                    new Color(1f, 0.93f, 0.48f, alpha), Hash(index + 121));
                AddDiamond(vertices, center, radius, tint);
            }
        }

        private static void AddDiamond(VertexHelper vertices, Vector2 center, float radius,
            Color tint)
        {
            int start = vertices.currentVertCount;
            AddVertex(vertices, center + Vector2.up * radius * 1.5f, tint);
            AddVertex(vertices, center + Vector2.right * radius, tint);
            AddVertex(vertices, center + Vector2.down * radius * 1.5f, tint);
            AddVertex(vertices, center + Vector2.left * radius, tint);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddVertex(VertexHelper vertices, Vector2 position, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            vertex.uv0 = new Vector2(0.5f, 0.5f);
            vertices.AddVert(vertex);
        }

        private static float Hash(int value)
        {
            float raw = Mathf.Sin(value * 12.9898f + 78.233f) * 43758.5453f;
            return raw - Mathf.Floor(raw);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            duration = Mathf.Max(0.1f, duration);
        }
#endif
    }
}
