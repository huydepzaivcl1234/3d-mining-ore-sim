using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Low-cost procedural amethyst motes and geode shockwave for the Main Menu.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningMenuAmbientFx : MaskableGraphic
    {
        [Range(8, 64), SerializeField] private int moteCount = 36;
        [Min(0.5f), SerializeField] private float shockwaveSeconds = 3.8f;
        [SerializeField] private Color purple = new(0.90f, 0.48f, 1f, 1f);
        [SerializeField] private Color cyan = new(0.39f, 0.90f, 1f, 1f);

        private float elapsed;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect bounds = rectTransform.rect;
            DrawShockwave(vertices, bounds);
            DrawMotes(vertices, bounds);
        }

        private void DrawMotes(VertexHelper vertices, Rect bounds)
        {
            int count = Mathf.Clamp(moteCount, 8, 64);
            for (int index = 0; index < count; index++)
            {
                float seedA = Hash(index * 3 + 1);
                float seedB = Hash(index * 3 + 2);
                float seedC = Hash(index * 3 + 3);
                float speed = Mathf.Lerp(0.035f, 0.105f, seedB);
                float normalizedY = Mathf.Repeat(seedC + elapsed * speed, 1.18f) - 0.09f;
                float fade = Mathf.Clamp01(normalizedY * 8f) *
                             Mathf.Clamp01((1f - normalizedY) * 7f);
                float x = Mathf.Lerp(0.14f, 0.86f, seedA) * bounds.width + bounds.xMin;
                x += Mathf.Sin(elapsed * Mathf.Lerp(0.55f, 1.2f, seedC) + seedA * 9f) * 13f;
                float y = bounds.yMin + normalizedY * bounds.height;
                float size = Mathf.Lerp(1.8f, 4.8f, seedB);
                Color tint = Color.Lerp(purple, cyan, seedC);
                tint.a *= fade * Mathf.Lerp(0.35f, 0.82f, seedA);
                AddDiamond(vertices, new Vector2(x, y), size, tint);
            }
        }

        private void DrawShockwave(VertexHelper vertices, Rect bounds)
        {
            float t = Mathf.Repeat(elapsed, shockwaveSeconds) / shockwaveSeconds;
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            Vector2 center = new(bounds.xMin + bounds.width * 0.53f,
                bounds.yMin + bounds.height * 0.54f);
            Vector2 radius = new(Mathf.Lerp(10f, bounds.width * 0.54f, eased),
                Mathf.Lerp(5f, bounds.height * 0.30f, eased));
            float thickness = Mathf.Lerp(4f, 0.6f, eased);
            Color tint = new(0.88f, 0.47f, 1f, Mathf.Pow(1f - t, 2f) * 0.72f);
            const int segments = 64;
            for (int index = 0; index < segments; index++)
            {
                float a0 = index * Mathf.PI * 2f / segments;
                float a1 = (index + 1) * Mathf.PI * 2f / segments;
                Vector2 normal0 = new(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector2 normal1 = new(Mathf.Cos(a1), Mathf.Sin(a1));
                int start = vertices.currentVertCount;
                AddVertex(vertices, center + Vector2.Scale(normal0,
                    radius - Vector2.one * thickness), tint);
                AddVertex(vertices, center + Vector2.Scale(normal0,
                    radius + Vector2.one * thickness), tint);
                AddVertex(vertices, center + Vector2.Scale(normal1,
                    radius + Vector2.one * thickness), tint);
                AddVertex(vertices, center + Vector2.Scale(normal1,
                    radius - Vector2.one * thickness), tint);
                vertices.AddTriangle(start, start + 1, start + 2);
                vertices.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static void AddDiamond(VertexHelper vertices, Vector2 center, float radius,
            Color tint)
        {
            int start = vertices.currentVertCount;
            AddVertex(vertices, center + Vector2.up * radius, tint);
            AddVertex(vertices, center + Vector2.right * radius * 0.7f, tint);
            AddVertex(vertices, center + Vector2.down * radius, tint);
            AddVertex(vertices, center + Vector2.left * radius * 0.7f, tint);
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
            moteCount = Mathf.Clamp(moteCount, 8, 64);
            shockwaveSeconds = Mathf.Max(0.5f, shockwaveSeconds);
            SetVerticesDirty();
        }
#endif
    }
}
