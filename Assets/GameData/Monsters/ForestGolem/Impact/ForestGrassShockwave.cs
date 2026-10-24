using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores
{
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ForestGrassShockwave : MonoBehaviour
    {
        [Header("Wave")]
        [SerializeField, Min(.1f)] private float duration = 1.15f;
        [SerializeField, Min(.1f)] private float radius = 4f;
        [SerializeField, Min(.01f)] private float ringWidth = .17f;
        [SerializeField, Min(0)] private float groundOffset = .025f;
        [SerializeField, Range(24, 192)] private int segments = 96;
        [Header("Vegetation")]
        [SerializeField, Range(8, 160)] private int grassClumps = 80;
        [SerializeField, Min(.01f)] private float grassHeight = .32f;
        [SerializeField, Min(.005f)] private float bladeWidth = .065f;
        [SerializeField, Range(0, 96)] private int floatingLeaves = 32;
        [SerializeField] private Color grassColor = new Color(.2f, .85f, .045f, 1);
        [SerializeField] private Color waveColor = new Color(.3f, 1f, .08f, 1);
        [Header("Lifetime / Scene preview")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool destroyAfterPlay;
        [SerializeField, Range(0, 1)] private float previewProgress = .42f;
        private Mesh mesh;
        private MeshFilter filter;
        private MeshRenderer visual;
        private Vector3[] vertices;
        private Color[] colors;
        private int[] triangles;
        private float elapsed;
        private bool playing, controlled, rebuild = true;
        private int ringVertices;

        private void OnEnable()
        {
            EnsureMesh();
            if (Application.IsPlaying(gameObject) && playOnEnable) Play();
            else Draw(previewProgress);
        }

        /// <summary>Restart a placed or pooled instance. The emitter spawns it at the impact point.</summary>
        public void Play()
        {
            controlled = false;
            EnsureMesh();
            elapsed = 0f;
            playing = true;
            visual.enabled = true;
            Draw(0f);
        }

        // Combat owns the clock and radius, so the visible front matches its hit test
        // and freezes together with the monster while the player is using a rune.
        public void PlayControlled(float outerRadius, float hitScale)
        {
            controlled = true;
            playing = false;
            radius = Mathf.Max(.1f, outerRadius / Mathf.Max(.01f, hitScale));
            transform.localScale = Vector3.one * hitScale;
            EnsureMesh();
            visual.enabled = true;
            Draw(0f);
        }

        public void Present(float progress) => Draw(Mathf.Clamp01(progress));
        public void Stop()
        {
            playing = false;
            if (visual != null) visual.enabled = false;
        }

        private void Update()
        {
            if (!Application.IsPlaying(gameObject))
            {
                if (rebuild) { EnsureMesh(); Draw(previewProgress); }
                return;
            }
            if (controlled || !playing) return;
            elapsed += Time.deltaTime;
            Draw(Mathf.Clamp01(elapsed / Mathf.Max(.1f, duration)));
            if (elapsed < duration) return;
            playing = false;
            visual.enabled = false;
            if (destroyAfterPlay) Destroy(gameObject);
        }

        private void OnDisable()
        {
            playing = false;
            if (Application.IsPlaying(gameObject) && visual != null) visual.enabled = false;
        }

        private void OnValidate()
        {
            duration = Mathf.Max(.1f, duration);
            radius = Mathf.Max(.1f, radius);
            ringWidth = Mathf.Max(.01f, ringWidth);
            grassHeight = Mathf.Max(.01f, grassHeight);
            bladeWidth = Mathf.Max(.005f, bladeWidth);
            segments = Mathf.Clamp(segments, 24, 192);
            grassClumps = Mathf.Clamp(grassClumps, 8, 160);
            floatingLeaves = Mathf.Clamp(floatingLeaves, 0, 96);
            rebuild = true;
        }

        private void EnsureMesh()
        {
            if (!rebuild && mesh != null) return;
            if (filter == null) filter = GetComponent<MeshFilter>();
            if (visual == null) visual = GetComponent<MeshRenderer>();
            visual.shadowCastingMode = ShadowCastingMode.Off;
            visual.receiveShadows = false;
            ReleaseMesh();
            ringVertices = (segments + 1) * 4;
            int count = ringVertices + grassClumps * 3 * 5 + floatingLeaves * 4;
            vertices = new Vector3[count];
            colors = new Color[count];
            triangles = new int[segments * 12 + grassClumps * 3 * 9 + floatingLeaves * 6];
            int cursor = 0;
            for (int ring = 0; ring < 2; ring++)
            {
                int start = ring * (segments + 1) * 2;
                for (int i = 0; i < segments; i++)
                {
                    int a = start + i * 2;
                    Tri(ref cursor, a, a + 2, a + 1);
                    Tri(ref cursor, a + 1, a + 2, a + 3);
                }
            }
            for (int i = 0; i < grassClumps * 3; i++)
            {
                int a = ringVertices + i * 5;
                Tri(ref cursor, a, a + 1, a + 2);
                Tri(ref cursor, a + 1, a + 3, a + 2);
                Tri(ref cursor, a + 2, a + 3, a + 4);
            }
            for (int i = 0; i < floatingLeaves; i++)
            {
                int a = ringVertices + grassClumps * 15 + i * 4;
                Tri(ref cursor, a, a + 1, a + 2);
                Tri(ref cursor, a, a + 2, a + 3);
            }
            mesh = new Mesh { name = "Forest grass wave (generated)", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            filter.sharedMesh = mesh;
            rebuild = false;
        }

        private void Tri(ref int cursor, int a, int b, int c)
        {
            triangles[cursor++] = a; triangles[cursor++] = b; triangles[cursor++] = c;
        }

        private static float Noise(int index) => Mathf.Repeat(Mathf.Sin(index * 127.1f + 311.7f) * 43758.5453f, 1f);

        private void Draw(float progress)
        {
            if (mesh == null) return;
            float expansion = controlled ? progress : 1f - Mathf.Pow(1f - progress, 2.3f);
            float distance = controlled ? radius * expansion : Mathf.Lerp(.12f, radius, expansion);
            float fade = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(.48f, 1f, progress));
            float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress * 9f));
            for (int ring = 0; ring < 2; ring++)
            {
                float r = distance * (ring == 0 ? 1f : .84f);
                int start = ring * (segments + 1) * 2;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float flutter = Mathf.Sin(angle * 11f + progress * 12f) * ringWidth * .16f;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    int a = start + i * 2;
                    vertices[a] = direction * Mathf.Max(.01f, r - ringWidth * .5f + flutter) + Vector3.up * groundOffset;
                    vertices[a + 1] = direction * (r + ringWidth * .5f + flutter) + Vector3.up * groundOffset;
                    Color c = waveColor; c.a *= fade * grow * (ring == 0 ? .9f : .34f);
                    colors[a] = c; colors[a + 1] = c;
                }
            }
            for (int clump = 0; clump < grassClumps; clump++)
            {
                float angle = (clump + Noise(clump) * .4f) * Mathf.PI * 2f / grassClumps;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-radial.z, 0, radial.x);
                Vector3 center = radial * (distance + (Noise(clump + 400) - .5f) * ringWidth * 2.8f);
                center.y = groundOffset;
                for (int blade = 0; blade < 3; blade++)
                {
                    int id = clump * 3 + blade;
                    float h = grassHeight * (.65f + Noise(id + 50) * .7f) * grow * Mathf.Lerp(1f, .18f, progress);
                    float w = bladeWidth * (.65f + Noise(id + 90) * .5f);
                    Vector3 root = center + tangent * ((blade - 1) * bladeWidth * 1.3f);
                    Vector3 bend = radial * h * (.32f + progress * .6f) + tangent * Mathf.Sin(progress * 13f + id) * h * .12f;
                    int a = ringVertices + id * 5;
                    vertices[a] = root - tangent * w;
                    vertices[a + 1] = root + tangent * w;
                    vertices[a + 2] = root + Vector3.up * h * .55f + bend * .35f - tangent * w * .48f;
                    vertices[a + 3] = root + Vector3.up * h * .55f + bend * .35f + tangent * w * .48f;
                    vertices[a + 4] = root + Vector3.up * h + bend;
                    Color c = grassColor; c.a *= fade * grow;
                    for (int k = 0; k < 5; k++) colors[a + k] = c;
                    Color tip = waveColor; tip.a = c.a * .7f; colors[a + 4] = tip;
                }
            }
            for (int i = 0; i < floatingLeaves; i++)
            {
                float angle = (i + .2f) * Mathf.PI * 2f / Mathf.Max(1, floatingLeaves) + progress * .25f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-radial.z, 0, radial.x);
                Vector3 center = radial * distance * (.72f + Noise(i + 800) * .3f);
                center.y = groundOffset + Mathf.Sin(progress * Mathf.PI) * (.15f + Noise(i + 900) * .5f);
                float size = .035f + Noise(i + 1000) * .028f;
                int a = ringVertices + grassClumps * 15 + i * 4;
                vertices[a] = center - tangent * size;
                vertices[a + 1] = center + Vector3.up * size * 1.7f;
                vertices[a + 2] = center + tangent * size;
                vertices[a + 3] = center - Vector3.up * size * 1.7f;
                Color c = waveColor; c.a *= fade * grow * .65f;
                for (int k = 0; k < 4; k++) colors[a + k] = c;
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            float extent = radius + grassHeight + ringWidth * 2f + .3f;
            float height = Mathf.Max(1f, grassHeight + .8f);
            mesh.bounds = new Bounds(Vector3.up * (groundOffset + height * .5f),
                new Vector3(extent * 2f, height + .2f, extent * 2f));
        }

        private void OnDestroy() => ReleaseMesh();
        private void ReleaseMesh()
        {
            if (mesh == null) return;
            if (filter != null && filter.sharedMesh == mesh) filter.sharedMesh = null;
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            mesh = null;
        }
    }
}
