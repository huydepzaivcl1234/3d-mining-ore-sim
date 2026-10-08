using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Stencil-only circular mask used to crop the existing Inventory sprite.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInventoryIconMaskGraphic : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            color = Color.white;
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            float radius = Mathf.Min(r.width, r.height) * 0.5f;
            Vector2 center = r.center;
            const int segments = 32;
            int middle = vh.currentVertCount;
            vh.AddVert(center, Color.white, new Vector2(0.5f, 0.5f));
            for (int index = 0; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(center + direction * radius, Color.white,
                    new Vector2(direction.x * 0.5f + 0.5f, direction.y * 0.5f + 0.5f));
                if (index > 0) vh.AddTriangle(middle, middle + index, middle + index + 1);
            }
        }
    }
}
