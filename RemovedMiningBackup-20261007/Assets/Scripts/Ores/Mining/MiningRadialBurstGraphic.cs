using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Procedural, texture-free radial flare or rotating light-ray mesh for uGUI.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningRadialBurstGraphic : MaskableGraphic
    {
        public enum BurstStyle
        {
            Flare,
            Rays
        }

        [SerializeField] private BurstStyle style;
        [Range(8, 96), SerializeField] private int segments = 64;
        [Range(4, 32), SerializeField] private int rayCount = 16;
        [SerializeField] private Color centerColor = Color.white;
        [SerializeField] private Color middleColor = new(0.96f, 0.56f, 1f, 0.88f);
        [SerializeField] private Color edgeColor = new(0.52f, 0.08f, 0.9f, 0f);

        public void Configure(BurstStyle configuredStyle, Color center, Color middle, Color edge)
        {
            style = configuredStyle;
            centerColor = center;
            middleColor = middle;
            edgeColor = edge;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (style == BurstStyle.Rays)
            {
                BuildRays(vertices);
            }
            else
            {
                BuildFlare(vertices);
            }
        }

        private void BuildFlare(VertexHelper vertices)
        {
            int count = Mathf.Clamp(segments, 8, 96);
            float[] radii = { 0f, 0.18f, 0.48f, 0.76f, 1f };
            Color[] colors =
            {
                centerColor,
                Color.Lerp(centerColor, middleColor, 0.45f),
                middleColor,
                Color.Lerp(middleColor, edgeColor, 0.62f),
                edgeColor
            };
            Rect bounds = rectTransform.rect;
            Vector2 radius = bounds.size * 0.5f;

            for (int ring = 0; ring < radii.Length; ring++)
            {
                for (int index = 0; index < count; index++)
                {
                    float angle = index * Mathf.PI * 2f / count;
                    Vector2 position = new(Mathf.Cos(angle) * radius.x * radii[ring],
                        Mathf.Sin(angle) * radius.y * radii[ring]);
                    AddVertex(vertices, position, colors[ring]);
                }
            }

            for (int ring = 0; ring < radii.Length - 1; ring++)
            {
                int inner = ring * count;
                int outer = (ring + 1) * count;
                for (int index = 0; index < count; index++)
                {
                    int next = (index + 1) % count;
                    vertices.AddTriangle(inner + index, outer + index, outer + next);
                    vertices.AddTriangle(inner + index, outer + next, inner + next);
                }
            }
        }

        private void BuildRays(VertexHelper vertices)
        {
            int count = Mathf.Clamp(rayCount, 4, 32);
            Rect bounds = rectTransform.rect;
            float radius = Mathf.Max(bounds.width, bounds.height) * 0.55f;
            Color inner = middleColor;
            Color outer = edgeColor;

            for (int index = 0; index < count; index++)
            {
                float center = index * Mathf.PI * 2f / count;
                float halfWidth = Mathf.Lerp(0.035f, 0.11f, (index % 4) / 3f);
                float length = radius * (index % 3 == 0 ? 1f : 0.78f);
                float innerRadius = radius * 0.06f;
                int start = vertices.currentVertCount;
                AddVertex(vertices, Direction(center - halfWidth) * innerRadius, inner);
                AddVertex(vertices, Direction(center + halfWidth) * innerRadius, inner);
                AddVertex(vertices, Direction(center + halfWidth * 0.35f) * length, outer);
                AddVertex(vertices, Direction(center - halfWidth * 0.35f) * length, outer);
                vertices.AddTriangle(start, start + 1, start + 2);
                vertices.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static Vector2 Direction(float angle) =>
            new(Mathf.Cos(angle), Mathf.Sin(angle));

        private static void AddVertex(VertexHelper vertices, Vector2 position, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            vertex.uv0 = new Vector2(0.5f, 0.5f);
            vertices.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            segments = Mathf.Clamp(segments, 8, 96);
            rayCount = Mathf.Clamp(rayCount, 4, 32);
            SetVerticesDirty();
        }
#endif
    }
}
