using UnityEngine;

namespace ChestDefense.Fps
{
    public static class FpsHitscan
    {
        public static bool Closest(Ray ray, float distance, LayerMask layers, Transform owner,
            Transform viewRoot, RaycastHit[] buffer, out RaycastHit result, out bool saturated)
        {
            result = default;
            int count = Physics.RaycastNonAlloc(ray, buffer, distance, layers, QueryTriggerInteraction.Ignore);
            saturated = count >= buffer.Length;
            if (saturated) return false; // Never apply damage using an incomplete obstruction query.
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = buffer[i]; var t = hit.collider.transform;
                if (owner != null && t.IsChildOf(owner) || viewRoot != null && t.IsChildOf(viewRoot)) continue;
                if (hit.distance < nearest) { result = hit; nearest = hit.distance; }
            }
            return nearest < float.PositiveInfinity;
        }
    }
}
