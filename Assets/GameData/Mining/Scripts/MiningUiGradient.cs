using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Vertical color treatment for authored uGUI surfaces.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Graphic))]
    public sealed class MiningUiGradient : UnityEngine.UI.BaseMeshEffect
    {
        [SerializeField] private Color topColor = Color.white;
        [SerializeField] private Color bottomColor = Color.white;

        public bool HasColors(Color top, Color bottom) =>
            topColor == top && bottomColor == bottom;

        public void SetColors(Color top, Color bottom)
        {
            topColor = top;
            bottomColor = bottom;
            graphic?.SetVerticesDirty();
        }

        public override void ModifyMesh(UnityEngine.UI.VertexHelper vertices)
        {
            if (!IsActive() || vertices.currentVertCount == 0) return;

            UIVertex vertex = default;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }

            float height = Mathf.Max(0.0001f, maxY - minY);
            for (int index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                float ratio = Mathf.Clamp01((vertex.position.y - minY) / height);
                Color tint = Color.Lerp(bottomColor, topColor, ratio);
                Color original = vertex.color;
                vertex.color = new Color(tint.r * original.r, tint.g * original.g,
                    tint.b * original.b, tint.a * original.a);
                vertices.SetUIVertex(vertex, index);
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
