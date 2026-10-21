using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>The same horizontal sector is used by single hits and sword sweeps.</summary>
    public static class CombatHitQuery
    {
        public static Collider[] Overlap(Vector3 origin, float halfHeight, float range, LayerMask layers)
        {
            // Keep an unbounded query: a crowded fight must not silently lose hits.
            return Physics.OverlapCapsule(origin - Vector3.up * halfHeight,
                origin + Vector3.up * halfHeight, range, layers, QueryTriggerInteraction.Ignore);
        }

        public static bool TryContact(Collider collider, Transform attacker, Vector3 origin,
            Vector3 forward, float halfHeight, float range, float angle,
            out MiningCharacterHealth target, out Vector3 point, out float distanceSquared)
        {
            target = collider.GetComponentInParent<MiningCharacterHealth>();
            point = origin;
            distanceSquared = float.PositiveInfinity;
            if (target == null || target.Health <= 0f || target.transform == attacker ||
                target.transform.IsChildOf(attacker)) return false;

            point = collider.ClosestPoint(origin);
            Vector3 direction = point - origin;
            if (Mathf.Abs(direction.y) > halfHeight) return false;
            direction.y = 0f;
            distanceSquared = direction.sqrMagnitude;
            return distanceSquared <= range * range &&
                (distanceSquared <= 0.0001f || Vector3.Angle(forward, direction) <= angle * 0.5f);
        }
    }
}
