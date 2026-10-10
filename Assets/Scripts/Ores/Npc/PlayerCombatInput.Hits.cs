using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private void ApplyHit()
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        MiningCharacterHealth closest = null;
        Vector3 closestPoint = origin;
        float closestDistance = float.PositiveInfinity;
        foreach (var collider in CombatHitQuery.Overlap(origin, HitHalfHeight, AttackRange, targetLayers))
        {
            if (!CombatHitQuery.TryContact(collider, transform, origin, StrikeForward,
                HitHalfHeight, AttackRange, Mathf.Min(30f, AttackAngle),
                out var target, out var point, out float distance)) continue;
            if (target.GetComponentInParent<TreasureChest>() != null || !HasClearStrikePath(collider,target.transform)) continue;
            if (distance < closestDistance)
            {
                closest = target;
                closestDistance = distance;
                closestPoint = point;
            }
        }
        if (closest != null) DamageTarget(closest, closestPoint);
    }

    private void ApplySweepHit(float damageMultiplier = 1f)
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        hitTargets.Clear();
        foreach (var collider in CombatHitQuery.Overlap(origin, HitHalfHeight, AttackRange, targetLayers))
        {
            if (!CombatHitQuery.TryContact(collider, transform, origin, StrikeForward,
                HitHalfHeight, AttackRange, AttackAngle, out var target, out var point, out _) ||
                target.GetComponentInParent<TreasureChest>() != null || !HasClearStrikePath(collider,target.transform) || !hitTargets.Add(target)) continue;
            DamageTarget(target, point, damageMultiplier);
        }
    }

}
