using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Procedural recessed effect card with an accent socket and live duration bar.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningStatusEffectCardGraphic : MaskableGraphic
    {
        [SerializeField] private Color accent = new(1f, 0.65f, 0.1f, 1f);
        [SerializeField, Range(0f, 1f)] private float progress = 1f;
        [SerializeField] private bool urgent;
        private float lastPulse = -1f;

        public void SetState(Color newAccent, float normalizedProgress, bool isUrgent)
        {
            newAccent.a = 1f;
            float nextProgress = Mathf.Clamp01(normalizedProgress);
            if (accent == newAccent && Mathf.Approximately(progress, nextProgress) && urgent == isUrgent)
                return;
            accent = newAccent;
            progress = nextProgress;
            urgent = isUrgent;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            if (!urgent) return;
            float pulse = Mathf.Floor(Time.unscaledTime * 12f) / 12f;
            if (Mathf.Approximately(pulse, lastPulse)) return;
            lastPulse = pulse;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 1f || r.height <= 1f) return;

            Color liveAccent = urgent
                ? Color.Lerp(new Color(0.95f, 0.12f, 0.08f, 1f), Color.white,
                    (Mathf.Sin(Time.unscaledTime * 9f) + 1f) * 0.12f)
                : accent;
            float radius = Mathf.Clamp(r.height * 0.16f, 5f, 12f);
            Color shadow = liveAccent; shadow.a = 0.24f;
            AddRoundedRect(vh, Expand(r, 2f), radius + 2f, shadow);
            AddRoundedRect(vh, r, radius, liveAccent);
            Rect inner = Inset(r, 3f);
            AddRoundedRect(vh, inner, radius - 2f, new Color(0.075f, 0.025f, 0.085f, 1f));
            Rect topLight = new(inner.x, inner.center.y, inner.width, inner.height * 0.5f);
            AddRoundedRect(vh, topLight, radius * 0.45f, new Color(0.18f, 0.08f, 0.18f, 0.55f));

            float socketSize = Mathf.Min(r.height * 0.62f, r.width * 0.32f);
            Rect socket = new(r.xMin + r.width * 0.06f, r.center.y - socketSize * 0.5f,
                socketSize, socketSize);
            Color glow = liveAccent; glow.a = 0.24f;
            AddRoundedRect(vh, Expand(socket, 3f), socketSize * 0.5f + 3f, glow);
            AddRoundedRect(vh, socket, socketSize * 0.5f, liveAccent);
            AddRoundedRect(vh, Inset(socket, 2f), socketSize * 0.5f - 2f,
                new Color(0.035f, 0.025f, 0.045f, 1f));

            float barX = r.xMin + r.width * 0.42f;
            float barWidth = r.width * 0.52f;
            float barHeight = Mathf.Max(3f, r.height * 0.075f);
            Rect track = new(barX, r.yMin + r.height * 0.12f, barWidth, barHeight);
            AddRoundedRect(vh, Expand(track, 1f), barHeight * 0.5f + 1f,
                new Color(0f, 0f, 0f, 0.9f));
            AddRoundedRect(vh, track, barHeight * 0.5f, new Color(0.14f, 0.10f, 0.15f, 1f));
            Rect fill = track;
            fill.width *= progress;
            AddRoundedRect(vh, fill, Mathf.Min(barHeight * 0.5f, fill.width * 0.5f), liveAccent);
            if (fill.width > 2f)
            {
                Rect shine = fill;
                shine.y = fill.yMax - Mathf.Max(1f, fill.height * 0.28f);
                shine.height = Mathf.Max(1f, fill.height * 0.28f);
                Color shineColor = Color.white; shineColor.a = 0.38f;
                AddQuad(vh, shine, shineColor);
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

        private static void AddOutline(VertexHelper vh, Rect r, Color color, float thickness)
        {
            AddQuad(vh, new Rect(r.xMin, r.yMax - thickness, r.width, thickness), color);
            AddQuad(vh, new Rect(r.xMin, r.yMin, r.width, thickness), color);
            AddQuad(vh, new Rect(r.xMin, r.yMin, thickness, r.height), color);
            AddQuad(vh, new Rect(r.xMax - thickness, r.yMin, thickness, r.height), color);
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
