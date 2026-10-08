using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Scene-editable flat rounded HUD background with no texture or shader dependency.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MiningPcRoundedGraphic : Graphic
    {
        [SerializeField, Min(0f)] private float cornerRadius = 16f;
        [SerializeField, Range(2, 16)] private int cornerSegments = 6;

        public void Configure(float radius, Color fill)
        {
            cornerRadius = Mathf.Max(0f, radius);
            color = fill;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * .5f);
            int segments = Mathf.Max(2, cornerSegments);
            Vector2 midpoint = rect.center;
            Add(mesh, midpoint, rect);
            int vertices = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = corner switch
                {
                    0 => new Vector2(rect.xMax - radius, rect.yMax - radius),
                    1 => new Vector2(rect.xMin + radius, rect.yMax - radius),
                    2 => new Vector2(rect.xMin + radius, rect.yMin + radius),
                    _ => new Vector2(rect.xMax - radius, rect.yMin + radius)
                };
                for (int step = 0; step <= segments; step++)
                {
                    // Start at the top-right edge, then wind counterclockwise.
                    float angle = (corner * 90f + step * 90f / segments) * Mathf.Deg2Rad;
                    Add(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, rect);
                    vertices++;
                    if (vertices > 1) mesh.AddTriangle(0, vertices - 1, vertices);
                }
            }
            mesh.AddTriangle(0, vertices, 1);
        }

        private void Add(VertexHelper mesh, Vector2 position, Rect rect)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = new Vector2((position.x - rect.xMin) / rect.width,
                (position.y - rect.yMin) / rect.height);
            mesh.AddVert(vertex);
        }
    }
}
