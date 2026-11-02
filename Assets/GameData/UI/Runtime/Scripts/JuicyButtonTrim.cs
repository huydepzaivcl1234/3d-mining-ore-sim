using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Small stitched seam and metal corner accents on a standard uGUI Image.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class JuicyButtonTrim : BaseMeshEffect
    {
        [SerializeField] private bool medalRivets;

        public void SetMedalRivets(bool value)
        {
            medalRivets = value;
            graphic?.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || graphic == null) return;
            Rect bounds = graphic.rectTransform.rect;
            UIVertex[] quad = new UIVertex[4];
            if (medalRivets)
            {
                for (int index = 0; index < 8; index++)
                {
                    float angle = index * Mathf.PI * 0.25f;
                    AddDot(vertices, quad, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                        Mathf.Min(bounds.width, bounds.height) * 0.41f, 1.6f,
                        new Color(0.32f, 0.17f, 0.03f, 1f));
                }
                return;
            }

            Color thread = new(0.95f, 0.81f, 0.62f, 0.92f);
            for (float x = bounds.xMin + 8f; x <= bounds.xMax - 8f; x += 9f)
            {
                AddDot(vertices, quad, new Vector2(x, bounds.yMax - 4f), 1.3f, thread);
                AddDot(vertices, quad, new Vector2(x, bounds.yMin + 4f), 1.3f, thread);
            }
            for (float y = bounds.yMin + 9f; y <= bounds.yMax - 9f; y += 9f)
            {
                AddDot(vertices, quad, new Vector2(bounds.xMin + 4f, y), 1.3f, thread);
                AddDot(vertices, quad, new Vector2(bounds.xMax - 4f, y), 1.3f, thread);
            }
            Color iron = new(0.23f, 0.23f, 0.22f, 1f);
            AddDot(vertices, quad, new Vector2(bounds.xMin + 5f, bounds.yMin + 5f), 2.7f, iron);
            AddDot(vertices, quad, new Vector2(bounds.xMax - 5f, bounds.yMin + 5f), 2.7f, iron);
            AddDot(vertices, quad, new Vector2(bounds.xMin + 5f, bounds.yMax - 5f), 2.7f, iron);
            AddDot(vertices, quad, new Vector2(bounds.xMax - 5f, bounds.yMax - 5f), 2.7f, iron);
        }

        private static void AddDot(VertexHelper vertices, UIVertex[] quad, Vector2 center,
            float radius, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.uv0 = new Vector2(0.5f, 0.5f);
            vertex.position = new Vector3(center.x - radius, center.y - radius); quad[0] = vertex;
            vertex.position = new Vector3(center.x - radius, center.y + radius); quad[1] = vertex;
            vertex.position = new Vector3(center.x + radius, center.y + radius); quad[2] = vertex;
            vertex.position = new Vector3(center.x + radius, center.y - radius); quad[3] = vertex;
            vertices.AddUIVertexQuad(quad);
        }
    }
}
