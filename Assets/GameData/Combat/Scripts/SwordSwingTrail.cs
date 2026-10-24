using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class SwordTrailSettings
    {
        public bool enabled = true;
        public Material material;
        [Min(.01f)] public float lifetime = .12f;
        [Min(0)] public float followThroughSeconds = .06f;
        [Range(.05f, 1)] public float bladeFraction = .65f;
        public Color color = new Color(.75f, .9f, 1f, .65f);
    }

    // Short world-space history of the blade's two endpoints, not a particle emitter.
    public sealed class SwordSwingTrail : IDisposable
    {
        private const int Capacity = 32;
        private readonly Vector3[] bases = new Vector3[Capacity], tips = new Vector3[Capacity];
        private readonly float[] times = new float[Capacity];
        private readonly Vector3[] vertices = new Vector3[Capacity * 2];
        private readonly Color[] colors = new Color[Capacity * 2];
        private readonly Vector2[] uv = new Vector2[Capacity * 2];
        private readonly int[] triangles = new int[(Capacity - 1) * 6];
        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly MeshRenderer renderer;
        private int count;
        public bool Visible => renderer != null && renderer.enabled;

        public SwordSwingTrail(Transform owner)
        {
            root = new GameObject("Sword swing trail");
            root.transform.SetParent(owner, false);
            mesh = new Mesh { name = "Short sword ribbon" };
            mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = root.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
        }

        public void Sample(Vector3 bladeBase, Vector3 tip, float now, SwordTrailSettings settings)
        {
            if (settings == null || !settings.enabled || settings.material == null) { Clear(); return; }
            float lifetime = Mathf.Max(.01f, settings.lifetime);
            int expired = 0;
            while (expired < count && now - times[expired] > lifetime) expired++;
            if (expired > 0)
            {
                count -= expired;
                Array.Copy(bases, expired, bases, 0, count);
                Array.Copy(tips, expired, tips, 0, count);
                Array.Copy(times, expired, times, 0, count);
            }
            if (count == Capacity)
            {
                Array.Copy(bases, 1, bases, 0, --count);
                Array.Copy(tips, 1, tips, 0, count);
                Array.Copy(times, 1, times, 0, count);
            }
            bases[count] = Vector3.Lerp(tip, bladeBase, Mathf.Clamp01(settings.bladeFraction));
            tips[count] = tip;
            times[count++] = now;
            for (int i = 0; i < count; i++)
            {
                int v = i * 2;
                vertices[v] = root.transform.InverseTransformPoint(bases[i]);
                vertices[v + 1] = root.transform.InverseTransformPoint(tips[i]);
                Color tint = settings.color;
                tint.a *= Mathf.Clamp01(1 - (now - times[i]) / lifetime);
                colors[v] = colors[v + 1] = tint;
                uv[v] = new Vector2((float)i / Mathf.Max(1, count - 1), 0);
                uv[v + 1] = new Vector2(uv[v].x, 1);
                if (i == 0) continue;
                int t = (i - 1) * 6;
                triangles[t] = v - 2; triangles[t + 1] = v; triangles[t + 2] = v - 1;
                triangles[t + 3] = v - 1; triangles[t + 4] = v; triangles[t + 5] = v + 1;
            }
            mesh.Clear();
            mesh.SetVertices(vertices, 0, count * 2);
            mesh.SetColors(colors, 0, count * 2);
            mesh.SetUVs(0, uv, 0, count * 2);
            mesh.SetTriangles(triangles, 0, Mathf.Max(0, count - 1) * 6, 0);
            renderer.sharedMaterial = settings.material;
            renderer.enabled = count > 1;
        }

        public void Clear() { count = 0; renderer.enabled = false; mesh.Clear(); }
        public void Dispose() { UnityEngine.Object.Destroy(mesh); UnityEngine.Object.Destroy(root); }
    }
}
