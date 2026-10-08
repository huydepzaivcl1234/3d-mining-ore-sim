using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private bool IsAimValid(MushroomMonster monster)
    {
        if (monster == null || !monster.isActiveAndEnabled || !monster.IsTargetVisible || monster.Health == null ||
            monster.Health.Health <= 0f) return false;
        return IsAimColliderInRange(monster.GetComponent<Collider>(), out _);
    }

    private bool IsAimColliderInRange(Collider collider, out float distanceSquared)
        => IsColliderInRange(collider, AttackRange, out distanceSquared);

    private bool IsColliderInRange(Collider collider, float range, out float distanceSquared)
    {
        distanceSquared = float.PositiveInfinity;
        if (collider == null || !collider.enabled || collider.isTrigger ||
            (targetLayers.value & (1 << collider.gameObject.layer)) == 0) return false;
        Vector3 direction = collider.ClosestPoint(transform.TransformPoint(HitOriginOffset)) -
            transform.TransformPoint(HitOriginOffset);
        if (Mathf.Abs(direction.y) > HitHalfHeight) return false;
        direction.y = 0f;
        distanceSquared = direction.sqrMagnitude;
        return distanceSquared <= range * range;
    }

    private MushroomMonster FindNearestMonster()
        => FindNearestMonsterInRange(AttackRange, false);

    private MushroomMonster FindNearestMonsterInRange(float range, bool forLunge)
    {
        MushroomMonster nearest = null;
        float distance = float.PositiveInfinity;
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        int count = Physics.OverlapCapsuleNonAlloc(origin - Vector3.up * HitHalfHeight,
            origin + Vector3.up * HitHalfHeight, range, softAimHits, targetLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var collider = softAimHits[i];
            var monster = collider.GetComponentInParent<MushroomMonster>();
            if (monster == null || !monster.isActiveAndEnabled || !monster.IsTargetVisible || monster.Health == null ||
                monster.Health.Health <= 0f || !IsColliderInRange(collider, range, out float candidate) ||
                forLunge && !IsLungeTargetValid(monster)) continue;
            if (candidate < distance) { distance = candidate; nearest = monster; }
        }
        // A full NonAlloc buffer is not guaranteed to contain the nearest collider.
        // The existing monster registry provides a non-allocating overflow fallback.
        if (count == softAimHits.Length)
            foreach (var monster in MushroomMonster.Monsters)
            {
                if (monster == null || !monster.isActiveAndEnabled || !monster.IsTargetVisible || monster.Health == null || monster.Health.Health <= 0f ||
                    forLunge && !IsLungeTargetValid(monster) || !IsColliderInRange(monster.GetComponent<Collider>(), range,
                        out float candidate) || candidate >= distance) continue;
                distance = candidate;
                nearest = monster;
            }
        return nearest;
    }

    private float LungeStopDistance => Mathf.Max(targetClearance, AttackRange * lungeStopRangeFraction);

    private void BeginLunge()
    {
        lungeProgress = lungePlannedDistance = lungeTravelRemaining = 0f;
        stepTarget = freeFlowEnabled ? FindNearestMonsterInRange(LungeAcquireRange, true) : null;
        lungeDirection = StrikeForward;
        if (stepTarget == null) return;
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        float gap = Vector3.ProjectOnPlane(stepTarget.GetComponent<Collider>().ClosestPoint(origin) - origin,
            Vector3.up).magnitude;
        lungePlannedDistance = Mathf.Min(Mathf.Max(0f, strikeStepDistance), Mathf.Max(0f, gap - LungeStopDistance));
        lungeTravelRemaining = lungePlannedDistance;
    }

    private bool IsLungeTargetValid(MushroomMonster monster)
    {
        if (monster == null || !monster.isActiveAndEnabled || !monster.IsTargetVisible || monster.Health == null || monster.Health.Health <= 0f)
            return false;
        var collider = monster.GetComponent<Collider>();
        float range = LungeAcquireRange + (lungeTracking && monster == stepTarget ? Mathf.Max(0f, lungeTrackingDistance) : 0f);
        if (!IsColliderInRange(collider, range, out _)) return false;
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        Vector3 delta = collider.ClosestPoint(origin) - origin;
        if (Vector3.Angle(StrikeForward, Vector3.ProjectOnPlane(delta, Vector3.up)) > maximumStepAngle) return false;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, lungeOcclusionHits, delta.magnitude,
            lungeBlockingLayers, QueryTriggerInteraction.Ignore);
        if (count == lungeOcclusionHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform obstacle = lungeOcclusionHits[i].collider.transform;
            if (obstacle == transform || obstacle.IsChildOf(transform) || obstacle == monster.transform ||
                obstacle.IsChildOf(monster.transform)) continue;
            return false;
        }
        return true;
    }

    private void BeginSoftAim()
    {
        if (IsShiftLocked) { StopSoftAim(); return; }
        softAimElapsed = 0f;
        softAimStartRotation = transform.rotation;
        attackAimTarget = stepTarget != null ? stepTarget : FindBestSoftAimTarget();
        softAimActive = attackAimTarget != null;
        if (movement != null) movement.ExternalFacing = softAimActive;
    }

    private MushroomMonster FindBestSoftAimTarget()
    {
        return FindNearestMonster();
    }

    private void ClearAim()
    {
        aimedMonster = null;
        StopSoftAim();
    }

    private void StopSoftAim()
    {
        attackAimTarget = null;
        softAimActive = false;
        if (movement != null) movement.ExternalFacing = IsShiftLocked || wasAttacking && !hitApplied;
    }

}
