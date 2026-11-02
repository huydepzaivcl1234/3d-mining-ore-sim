using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Responsive leather-and-metal frame for the authored Inventory menu button.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInventoryButtonGraphic : MaskableGraphic
    {
        private static readonly Color DeepShadow = new(0.045f, 0.014f, 0.006f, 1f);
        private static readonly Color Gold = new(0.79f, 0.47f, 0.10f, 1f);
        private static readonly Color GoldLight = new(1f, 0.83f, 0.38f, 1f);
        private static readonly Color LeatherTop = new(0.39f, 0.20f, 0.09f, 1f);
        private static readonly Color LeatherBottom = new(0.15f, 0.047f, 0.014f, 1f);
        private static readonly Color Steel = new(0.34f, 0.39f, 0.43f, 1f);
        private static readonly Color Thread = new(0.94f, 0.77f, 0.58f, 0.70f);

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect bounds = GetPixelAdjustedRect();
            if (bounds.width < 2f || bounds.height < 2f) return;

            float radius = Mathf.Clamp(bounds.height * 0.28f, 8f, 22f);
            float depth = Mathf.Clamp(bounds.height * 0.075f, 3f, 7f);
            Rect shadow = new(bounds.xMin + 1f, bounds.yMin, bounds.width - 2f,
                bounds.height - depth);
            AddRoundedRect(vh, shadow, radius, DeepShadow);

            Rect face = new(bounds.xMin + 1f, bounds.yMin + depth, bounds.width - 2f,
                bounds.height - depth - 1f);
            AddRoundedRect(vh, face, radius, Gold);
            Rect leather = Inset(face, 2.4f);
            AddVerticalGradient(vh, leather, radius - 2f, LeatherTop, LeatherBottom);

            Rect groove = Inset(leather, Mathf.Max(2f, bounds.height * 0.035f));
            AddRoundedOutline(vh, groove, radius - 4f, 1.1f,
                new Color(0.08f, 0.02f, 0.008f, 0.92f));
            Rect seam = Inset(leather, Mathf.Max(5f, bounds.height * 0.085f));
            AddStitches(vh, seam, Thread, Mathf.Max(1f, bounds.height * 0.018f));

            float socketRadius = Mathf.Min(leather.height * 0.42f, leather.width * 0.14f);
            Vector2 socketCenter = new(leather.xMin + socketRadius + leather.height * 0.055f,
                leather.center.y);
            AddDisc(vh, socketCenter + new Vector2(0f, -1.5f), socketRadius + 2.5f, DeepShadow, 24);
            AddDisc(vh, socketCenter, socketRadius + 1.5f, GoldLight, 24);
            AddDisc(vh, socketCenter, socketRadius - 2f,
                new Color(0.075f, 0.018f, 0.006f, 1f), 24);

            Rect plaque = new(leather.xMin + leather.width * 0.285f,
                leather.yMin + leather.height * 0.16f,
                leather.width * 0.665f, leather.height * 0.68f);
            AddRoundedRect(vh, plaque, Mathf.Max(5f, plaque.height * 0.25f),
                new Color(0.055f, 0.012f, 0.004f, 0.98f));
            AddRoundedOutline(vh, plaque, Mathf.Max(5f, plaque.height * 0.25f), 1.2f,
                new Color(0.30f, 0.12f, 0.025f, 1f));

            float corner = Mathf.Clamp(bounds.height * 0.13f, 5f, 11f);
            AddCorner(vh, leather.xMin, leather.yMax, corner, 1f, -1f);
            AddCorner(vh, leather.xMax, leather.yMax, corner, -1f, -1f);
            AddCorner(vh, leather.xMin, leather.yMin, corner, 1f, 1f);
            AddCorner(vh, leather.xMax, leather.yMin, corner, -1f, 1f);
        }

        private static void AddCorner(VertexHelper vh, float x, float y, float size,
            float horizontal, float vertical)
        {
            Vector2 a = new(x, y);
            Vector2 b = new(x + horizontal * size, y);
            Vector2 c = new(x, y + vertical * size);
            AddTriangle(vh, a, b, c, Steel);
            Vector2 rivet = new(x + horizontal * size * 0.36f, y + vertical * size * 0.36f);
            AddDisc(vh, rivet, Mathf.Max(1.3f, size * 0.16f), GoldLight, 10);
        }

        private static void AddStitches(VertexHelper vh, Rect r, Color color, float size)
        {
            float step = Mathf.Max(7f, size * 6f);
            for (float x = r.xMin + step; x < r.xMax - step * 0.5f; x += step)
            {
                AddQuad(vh, new Rect(x, r.yMin, size * 2.2f, size), color);
                AddQuad(vh, new Rect(x, r.yMax - size, size * 2.2f, size), color);
            }
            for (float y = r.yMin + step; y < r.yMax - step * 0.5f; y += step)
            {
                AddQuad(vh, new Rect(r.xMin, y, size, size * 2.2f), color);
                AddQuad(vh, new Rect(r.xMax - size, y, size, size * 2.2f), color);
            }
        }

        private static void AddVerticalGradient(VertexHelper vh, Rect r, float radius,
            Color top, Color bottom)
        {
            AddRoundedRect(vh, r, radius, bottom);
            Rect upper = new(r.x, r.center.y, r.width, r.height * 0.5f);
            Color highlight = top; highlight.a = 0.82f;
            AddRoundedRect(vh, upper, Mathf.Min(radius, upper.height * 0.5f), highlight);
        }

        private static void AddRoundedOutline(VertexHelper vh, Rect r, float radius,
            float thickness, Color color)
        {
            _ = radius;
            AddQuad(vh, new Rect(r.xMin, r.yMax - thickness, r.width, thickness), color);
            AddQuad(vh, new Rect(r.xMin, r.yMin, r.width, thickness), color);
            AddQuad(vh, new Rect(r.xMin, r.yMin, thickness, r.height), color);
            AddQuad(vh, new Rect(r.xMax - thickness, r.yMin, thickness, r.height), color);
        }

        private static void AddRoundedRect(VertexHelper vh, Rect r, float radius, Color color)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(r.width, r.height) * 0.5f);
            if (radius < 0.1f)
            {
                AddQuad(vh, r, color);
                return;
            }

            const int segments = 6;
            int center = vh.currentVertCount;
            vh.AddVert(r.center, color, new Vector2(0.5f, 0.5f));
            int first = -1;
            int previous = -1;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 arcCenter = corner switch
                {
                    0 => new Vector2(r.xMax - radius, r.yMax - radius),
                    1 => new Vector2(r.xMin + radius, r.yMax - radius),
                    2 => new Vector2(r.xMin + radius, r.yMin + radius),
                    _ => new Vector2(r.xMax - radius, r.yMin + radius)
                };
                float start = corner * 90f;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = (start + segment * 90f / segments) * Mathf.Deg2Rad;
                    Vector2 point = arcCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    int current = vh.currentVertCount;
                    vh.AddVert(point, color, Vector2.zero);
                    if (first < 0) first = current;
                    if (previous >= 0) vh.AddTriangle(center, previous, current);
                    previous = current;
                }
            }
            vh.AddTriangle(center, previous, first);
        }

        private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color color,
            int segments)
        {
            int middle = vh.currentVertCount;
            vh.AddVert(center, color, new Vector2(0.5f, 0.5f));
            int first = -1;
            int previous = -1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                int current = vh.currentVertCount;
                vh.AddVert(point, color, Vector2.zero);
                if (first < 0) first = current;
                if (previous >= 0) vh.AddTriangle(middle, previous, current);
                previous = current;
            }
            vh.AddTriangle(middle, previous, first);
        }

        private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.up);
            vh.AddVert(c, color, Vector2.right);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        private static void AddQuad(VertexHelper vh, Rect r, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin, r.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), color, Vector2.up);
            vh.AddVert(new Vector3(r.xMax, r.yMax), color, Vector2.one);
            vh.AddVert(new Vector3(r.xMax, r.yMin), color, Vector2.right);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

        private static Rect Inset(Rect r, float value) => new(r.x + value, r.y + value,
            Mathf.Max(0f, r.width - value * 2f), Mathf.Max(0f, r.height - value * 2f));

        private static Rect Expand(Rect r, float value) => new(r.x - value, r.y - value,
            r.width + value * 2f, r.height + value * 2f);
    }
}
