using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Gold-trimmed amethyst disc used by the interactive Gem HUD plus button.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGemPlusGraphic : MaskableGraphic
    {
        private const int Segments = 40;

        protected override void Awake()
        {
            base.Awake();
            color = Color.white;
            raycastTarget = true;
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect bounds = rectTransform.rect;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
            Vector2 center = bounds.center;
            DrawCircle(vertices, center, radius,
                new Color(0.20f, 0.09f, 0f, 1f), new Color(1f, 0.94f, 0.62f, 1f));
            DrawCircle(vertices, center, radius * 0.84f,
                new Color(0.24f, 0.025f, 0.32f, 1f), new Color(0.64f, 0.14f, 0.82f, 1f));
            DrawEllipse(vertices, center + Vector2.up * radius * 0.35f,
                new Vector2(radius * 0.55f, radius * 0.17f),
                new Color(1f, 1f, 1f, 0.34f), new Color(1f, 1f, 1f, 0.12f));
        }

        private static void DrawCircle(VertexHelper vertices, Vector2 center, float radius,
            Color bottom, Color top) => DrawEllipse(vertices, center,
            new Vector2(radius, radius), bottom, top);

        private static void DrawEllipse(VertexHelper vertices, Vector2 center, Vector2 radius,
            Color bottom, Color top)
        {
            int start = vertices.currentVertCount;
            AddVertex(vertices, center, Color.Lerp(bottom, top, 0.5f));
            for (int index = 0; index < Segments; index++)
            {
                float angle = index * Mathf.PI * 2f / Segments;
                Vector2 position = center + new Vector2(Mathf.Cos(angle) * radius.x,
                    Mathf.Sin(angle) * radius.y);
                float vertical = Mathf.InverseLerp(center.y - radius.y,
                    center.y + radius.y, position.y);
                AddVertex(vertices, position, Color.Lerp(bottom, top, vertical));
            }
            for (int index = 0; index < Segments; index++)
            {
                int next = (index + 1) % Segments;
                vertices.AddTriangle(start, start + 1 + index, start + 1 + next);
            }
        }

        private static void AddVertex(VertexHelper vertices, Vector2 position, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            vertex.uv0 = new Vector2(0.5f, 0.5f);
            vertices.AddVert(vertex);
        }
    }
}
