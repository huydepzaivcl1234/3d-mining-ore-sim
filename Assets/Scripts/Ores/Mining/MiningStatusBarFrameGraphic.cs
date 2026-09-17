using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Resolution-independent leather frame for the active-effects HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningStatusBarFrameGraphic : MaskableGraphic
    {
        private static readonly Color Shadow = new(0.055f, 0.025f, 0.018f, 0.98f);
        private static readonly Color Leather = new(0.20f, 0.075f, 0.035f, 0.98f);
        private static readonly Color LeatherLight = new(0.38f, 0.15f, 0.065f, 1f);
        private static readonly Color Gold = new(0.92f, 0.56f, 0.08f, 1f);
        private static readonly Color Stitch = new(1f, 0.76f, 0.30f, 0.72f);
        private static readonly Color Steel = new(0.29f, 0.31f, 0.34f, 1f);

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 1f || r.height <= 1f) return;

            float radius = Mathf.Clamp(r.height * 0.12f, 8f, 16f);
            AddRoundedRect(vh, r, radius, Shadow);
            AddRoundedRect(vh, Inset(r, 3f), radius - 2f, Gold);
            Rect body = Inset(r, 5f);
            AddRoundedRect(vh, body, radius - 4f, Leather);
            Rect highlight = new(body.x, body.center.y, body.width, body.height * 0.5f);
            Color highlightColor = LeatherLight; highlightColor.a = 0.46f;
            AddRoundedRect(vh, highlight, radius * 0.45f, highlightColor);

            float seam = Mathf.Clamp(r.height * 0.07f, 5f, 9f);
            Rect inner = Inset(body, seam);
            AddDashedLine(vh, inner.xMin, inner.xMax, inner.yMin, true, Stitch, 4f, 3f, 1.2f);
            AddDashedLine(vh, inner.xMin, inner.xMax, inner.yMax, true, Stitch, 4f, 3f, 1.2f);
            AddDashedLine(vh, inner.yMin, inner.yMax, inner.xMin, false, Stitch, 4f, 3f, 1.2f);
            AddDashedLine(vh, inner.yMin, inner.yMax, inner.xMax, false, Stitch, 4f, 3f, 1.2f);

            float bracket = Mathf.Clamp(r.height * 0.16f, 13f, 21f);
            AddCorner(vh, body.xMin, body.yMax, bracket, -1f, -1f);
            AddCorner(vh, body.xMax, body.yMax, bracket, 1f, -1f);
            AddCorner(vh, body.xMin, body.yMin, bracket, -1f, 1f);
            AddCorner(vh, body.xMax, body.yMin, bracket, 1f, 1f);

            Rect badge = new(r.center.x - r.width * 0.24f, r.yMax - r.height * 0.235f,
                r.width * 0.48f, r.height * 0.22f);
            AddRoundedRect(vh, Expand(badge, 1.5f), 5f, Gold);
            AddRoundedRect(vh, badge, 4f, new Color(0.075f, 0.02f, 0.012f, 1f));
        }

        private static void AddCorner(VertexHelper vh, float x, float y, float size, float sx, float sy)
        {
            Rect horizontal = new(Mathf.Min(x, x - sx * size), Mathf.Min(y, y + sy * 4f), size, 4f);
            Rect vertical = new(Mathf.Min(x, x - sx * 4f), Mathf.Min(y, y + sy * size), 4f, size);
            AddQuad(vh, horizontal, Steel);
            AddQuad(vh, vertical, Steel);
            const float rivet = 4f;
            Rect dot = new(x - sx * 7f - rivet * 0.5f, y + sy * 7f - rivet * 0.5f,
                rivet, rivet);
            AddQuad(vh, dot, Gold);
        }

        private static void AddDashedLine(VertexHelper vh, float from, float to, float fixedAxis,
            bool horizontal, Color color, float dash, float gap, float thickness)
        {
            for (float p = from; p < to; p += dash + gap)
            {
                float length = Mathf.Min(dash, to - p);
                Rect segment = horizontal
                    ? new Rect(p, fixedAxis - thickness * 0.5f, length, thickness)
                    : new Rect(fixedAxis - thickness * 0.5f, p, thickness, length);
                AddQuad(vh, segment, color);
            }
        }

        private static Rect Inset(Rect r, float value) => new(r.x + value, r.y + value,
            Mathf.Max(0f, r.width - value * 2f), Mathf.Max(0f, r.height - value * 2f));

        private static Rect Expand(Rect r, float value) => new(r.x - value, r.y - value,
            r.width + value * 2f, r.height + value * 2f);

        private static void AddRoundedRect(VertexHelper vh, Rect r, float radius, Color color)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(r.width, r.height) * 0.5f);
            if (radius <= 0.1f)
            {
                AddQuad(vh, r, color);
                return;
            }

            const int cornerSegments = 5;
            int centerIndex = vh.currentVertCount;
            vh.AddVert(r.center, color, new Vector2(0.5f, 0.5f));
            int first = -1;
            int previous = -1;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = corner switch
                {
                    0 => new Vector2(r.xMax - radius, r.yMax - radius),
                    1 => new Vector2(r.xMin + radius, r.yMax - radius),
                    2 => new Vector2(r.xMin + radius, r.yMin + radius),
                    _ => new Vector2(r.xMax - radius, r.yMin + radius)
                };
                float startAngle = corner * 90f;
                for (int segment = 0; segment <= cornerSegments; segment++)
                {
                    float angle = (startAngle + segment * 90f / cornerSegments) * Mathf.Deg2Rad;
                    Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    int current = vh.currentVertCount;
                    vh.AddVert(point, color, Vector2.zero);
                    if (first < 0) first = current;
                    if (previous >= 0) vh.AddTriangle(centerIndex, previous, current);
                    previous = current;
                }
            }
            vh.AddTriangle(centerIndex, previous, first);
        }

        private static void AddVerticalGradient(VertexHelper vh, Rect r, Color top, Color bottom)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin, r.yMin), bottom, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), top, Vector2.up);
            vh.AddVert(new Vector3(r.xMax, r.yMax), top, Vector2.one);
            vh.AddVert(new Vector3(r.xMax, r.yMin), bottom, Vector2.right);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
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
    }
}
