using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Adds a lightweight vertical candy-style gradient to an existing uGUI Graphic.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class MiningCandyGradient : BaseMeshEffect
    {
        [SerializeField] private Color topColor = Color.white;
        [SerializeField] private Color bottomColor = Color.gray;

        public void SetColors(Color top, Color bottom)
        {
            topColor = top;
            bottomColor = bottom;
            graphic?.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
            {
                return;
            }

            UIVertex vertex = default;
            float minimumY = float.MaxValue;
            float maximumY = float.MinValue;
            for (int index = 0; index < vertexHelper.currentVertCount; index++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, index);
                minimumY = Mathf.Min(minimumY, vertex.position.y);
                maximumY = Mathf.Max(maximumY, vertex.position.y);
            }

            float height = Mathf.Max(0.0001f, maximumY - minimumY);
            for (int index = 0; index < vertexHelper.currentVertCount; index++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, index);
                float normalized = Mathf.Clamp01((vertex.position.y - minimumY) / height);
                Color source = vertex.color;
                Color color = Color.Lerp(bottomColor, topColor, normalized);
                color.r *= source.r;
                color.g *= source.g;
                color.b *= source.b;
                color.a *= source.a;
                vertex.color = color;
                vertexHelper.SetUIVertex(vertex, index);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            graphic?.SetVerticesDirty();
        }
#endif
    }
}
