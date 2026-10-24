using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Resolution-independent amethyst counter frame matching the supplied HUD sample.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGemHudFrameGraphic : MaskableGraphic
    {
        private const int RoundSegments = 32;

        public void Configure()
        {
            raycastTarget = false;
            color = Color.white;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect bounds = rectTransform.rect;
            float width = bounds.width;
            float height = bounds.height;
            if (width <= 1f || height <= 1f)
            {
                return;
            }

            // Ore casing, amethyst slab, recessed number well and inset highlight.
            DrawRoundedRect(vertices, Region(bounds, 0.015f, 0.07f, 0.985f, 0.93f),
                height * 0.43f, new Color(0.075f, 0.035f, 0.10f, 1f),
                new Color(0.24f, 0.18f, 0.28f, 1f));
            DrawRoundedRect(vertices, Region(bounds, 0.025f, 0.105f, 0.975f, 0.895f),
                height * 0.39f, new Color(0.83f, 0.35f, 1f, 1f),
                new Color(0.31f, 0.06f, 0.43f, 1f));
            DrawRoundedRect(vertices, Region(bounds, 0.033f, 0.135f, 0.967f, 0.865f),
                height * 0.36f, new Color(0.10f, 0.005f, 0.15f, 1f),
                new Color(0.42f, 0.095f, 0.56f, 1f));

            Rect well = Region(bounds, 0.225f, 0.22f, 0.81f, 0.78f);
            DrawRoundedRect(vertices, Region(bounds, 0.218f, 0.20f, 0.817f, 0.80f),
                height * 0.145f, new Color(0.43f, 0.10f, 0.57f, 1f),
                new Color(0.22f, 0.025f, 0.30f, 1f));
            DrawRoundedRect(vertices, well, height * 0.13f,
                new Color(0.11f, 0.02f, 0.15f, 1f),
                new Color(0.035f, 0.005f, 0.055f, 1f));
            DrawRoundedRect(vertices, Region(well, 0.015f, 0.57f, 0.985f, 0.96f),
                height * 0.07f, new Color(0.025f, 0f, 0.035f, 0.72f),
                new Color(0.055f, 0.005f, 0.075f, 0.16f));
            DrawLine(vertices, Point(bounds, 0.255f, 0.245f), Point(bounds, 0.78f, 0.245f),
                Mathf.Max(0.8f, height * 0.012f), new Color(0.83f, 0.38f, 1f, 0.34f));

            // Crystal facets inside the purple slab.
            DrawQuad(vertices, Point(bounds, 0.10f, 0.84f), Point(bounds, 0.34f, 0.84f),
                Point(bounds, 0.25f, 0.64f), Point(bounds, 0.075f, 0.64f),
                new Color(1f, 0.66f, 1f, 0.17f));
            DrawQuad(vertices, Point(bounds, 0.31f, 0.84f), Point(bounds, 0.69f, 0.84f),
                Point(bounds, 0.64f, 0.64f), Point(bounds, 0.25f, 0.64f),
                new Color(1f, 1f, 1f, 0.07f));
            DrawQuad(vertices, Point(bounds, 0.24f, 0.35f), Point(bounds, 0.64f, 0.35f),
                Point(bounds, 0.69f, 0.14f), Point(bounds, 0.18f, 0.14f),
                new Color(0.04f, 0f, 0.06f, 0.34f));

            // Left icon socket: gold trim, dark depth and purple glow.
            Vector2 socket = Point(bounds, 0.127f, 0.5f);
            float socketRadius = height * 0.37f;
            DrawCircle(vertices, socket, socketRadius,
                new Color(0.24f, 0.12f, 0.015f, 1f), new Color(1f, 0.90f, 0.48f, 1f));
            DrawCircle(vertices, socket, socketRadius * 0.89f,
                new Color(0.11f, 0.01f, 0.15f, 1f), new Color(0.33f, 0.12f, 0.40f, 1f));
            DrawCircle(vertices, socket, socketRadius * 0.70f,
                new Color(0.38f, 0.04f, 0.53f, 0.12f), new Color(0.62f, 0.10f, 0.80f, 0.44f));

            // Four small gold rivets around the number trench.
            float rivet = Mathf.Max(1.5f, height * 0.035f);
            Color rivetDark = new(0.27f, 0.12f, 0f, 1f);
            Color rivetLight = new(1f, 0.83f, 0.35f, 1f);
            DrawCircle(vertices, Point(bounds, 0.25f, 0.745f), rivet, rivetDark, rivetLight);
            DrawCircle(vertices, Point(bounds, 0.25f, 0.255f), rivet, rivetDark, rivetLight);
            DrawCircle(vertices, Point(bounds, 0.785f, 0.745f), rivet, rivetDark, rivetLight);
            DrawCircle(vertices, Point(bounds, 0.785f, 0.255f), rivet, rivetDark, rivetLight);
        }

        private static Rect Region(Rect parent, float xMin, float yMin, float xMax, float yMax)
        {
            return Rect.MinMaxRect(parent.xMin + parent.width * xMin,
                parent.yMin + parent.height * yMin,
                parent.xMin + parent.width * xMax,
                parent.yMin + parent.height * yMax);
        }

        private static Vector2 Point(Rect bounds, float x, float y) =>
            new(bounds.xMin + bounds.width * x, bounds.yMin + bounds.height * y);

        private static void DrawRoundedRect(VertexHelper vertices, Rect rect, float radius,
            Color bottom, Color top)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            int start = vertices.currentVertCount;
            AddVertex(vertices, rect.center, Color.Lerp(bottom, top, 0.5f));
            for (int index = 0; index < RoundSegments; index++)
            {
                float angle = (-90f + index * 360f / RoundSegments) * Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 corner = new(direction.x >= 0f ? rect.xMax - radius : rect.xMin + radius,
                    direction.y >= 0f ? rect.yMax - radius : rect.yMin + radius);
                Vector2 position = corner + direction * radius;
                float vertical = Mathf.InverseLerp(rect.yMin, rect.yMax, position.y);
                AddVertex(vertices, position, Color.Lerp(bottom, top, vertical));
            }
            for (int index = 0; index < RoundSegments; index++)
            {
                int next = (index + 1) % RoundSegments;
                vertices.AddTriangle(start, start + 1 + index, start + 1 + next);
            }
        }

        private static void DrawCircle(VertexHelper vertices, Vector2 center, float radius,
            Color bottom, Color top) => DrawEllipse(vertices, center,
            new Vector2(radius, radius), bottom, top);

        private static void DrawEllipse(VertexHelper vertices, Vector2 center, Vector2 radius,
            Color bottom, Color top)
        {
            int start = vertices.currentVertCount;
            AddVertex(vertices, center, Color.Lerp(bottom, top, 0.5f));
            for (int index = 0; index < RoundSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / RoundSegments;
                Vector2 position = center + new Vector2(Mathf.Cos(angle) * radius.x,
                    Mathf.Sin(angle) * radius.y);
                AddVertex(vertices, position,
                    Color.Lerp(bottom, top, Mathf.InverseLerp(center.y - radius.y,
                        center.y + radius.y, position.y)));
            }
            for (int index = 0; index < RoundSegments; index++)
            {
                int next = (index + 1) % RoundSegments;
                vertices.AddTriangle(start, start + 1 + index, start + 1 + next);
            }
        }

        private static void DrawLine(VertexHelper vertices, Vector2 from, Vector2 to,
            float thickness, Color tint)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 normal = new(-direction.y, direction.x);
            DrawQuad(vertices, from - normal * thickness, from + normal * thickness,
                to + normal * thickness, to - normal * thickness, tint);
        }

        private static void DrawQuad(VertexHelper vertices, Vector2 a, Vector2 b,
            Vector2 c, Vector2 d, Color tint)
        {
            int start = vertices.currentVertCount;
            AddVertex(vertices, a, tint);
            AddVertex(vertices, b, tint);
            AddVertex(vertices, c, tint);
            AddVertex(vertices, d, tint);
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
    }
}
