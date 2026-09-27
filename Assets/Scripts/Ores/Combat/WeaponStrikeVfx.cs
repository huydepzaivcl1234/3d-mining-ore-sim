using UnityEngine;

namespace MiningSimulator.Ores
{
    // Presentation only. Contact VFX are spawned exclusively by successful damage.
    public sealed class WeaponStrikeVfx : MonoBehaviour
    {
        public bool impact;
        [Min(0.03f)] public float duration = 0.2f;
        public bool sword;
        public float radius = 1.75f;
        public float angle = 110f;
        private LineRenderer[] lines;
        private float[] widths;
        private Color[] colors;
        private float age;
        private void Awake()
        {
            lines = GetComponentsInChildren<LineRenderer>();
            widths = new float[lines.Length];
            colors = new Color[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                widths[i] = lines[i].widthMultiplier;
                colors[i] = lines[i].startColor;
            }
            Draw(0f);
        }
        public void Configure(float range, float arc, float speed, bool sweep)
        {
            radius = range;
            angle = arc;
            sword = sweep;
            duration /= Mathf.Max(0.1f, speed);
            Draw(0f);
        }
        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            Draw(t);
            for (int i = 0; i < lines.Length; i++)
            {
                Color color = colors[i];
                color.a *= (1f - t) * (1f - t);
                lines[i].startColor = lines[i].endColor = color;
                lines[i].widthMultiplier = widths[i] * Mathf.Lerp(1f, 0.1f, t);
            }
            if (t >= 1f) Destroy(gameObject);
        }
        private void Draw(float t)
        {
            if (lines == null) return;
            for (int j = 0; j < lines.Length; j++)
            {
                var line = lines[j];
                for (int i = 0; i < line.positionCount; i++)
                {
                    float f = i / (float)(line.positionCount - 1);
                    Vector3 point;
                    if (impact)
                    {
                        float a = f * Mathf.PI * 2f;
                        float r = Mathf.Lerp(0.06f, radius, t) * (1f + j * 0.12f);
                        point = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r;
                    }
                    else if (sword)
                    {
                        float a = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, f) * Mathf.Deg2Rad;
                        point = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius;
                    }
                    else point = Vector3.forward * Mathf.Lerp(radius * (0.12f + t * 0.35f), radius, f);
                    line.SetPosition(i, point);
                }
            }
        }
    }
}
