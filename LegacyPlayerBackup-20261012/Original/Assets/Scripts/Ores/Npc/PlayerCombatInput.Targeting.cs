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
        // Movement during an existing combo is footwork, not a request to abandon
        // its victim. Reacquire only when opening or when that victim is invalid.
        MushroomMonster comboTarget = stepTarget;
        bool keepComboTarget = freeFlowEnabled && wasAttacking && IsLungeTargetValid(comboTarget);
        attackIntentDirection = ResolveAttackDirection(out bool hasInput);
        stepTarget = freeFlowEnabled ? keepComboTarget ? comboTarget : FindDirectionalTarget(attackIntentDirection, hasInput) : null;
        if (freeFlowEnabled && stepTarget == null && hasInput)
        {
            // Strafing/backpedalling must not turn an otherwise reachable enemy
            // in front of the camera into an air strike. Directional targets still
            // win when another enemy really exists in the requested direction.
            Vector3 facing = attackCamera != null ? Vector3.ProjectOnPlane(attackCamera.forward, Vector3.up).normalized : StrikeForward;
            stepTarget = FindDirectionalTarget(facing, false);
        }
        lungeHasTarget = stepTarget != null;
        lungeDirection = attackIntentDirection;
        if (stepTarget == null)
        {
            lungePlannedDistance = freeFlowEnabled ? Mathf.Max(0f, airStrikeStepDistance) : 0f;
            lungeTravelRemaining = lungePlannedDistance;
            return;
        }
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        float gap = Vector3.ProjectOnPlane(stepTarget.GetComponent<Collider>().ClosestPoint(origin) - origin,
            Vector3.up).magnitude;
        lungeDirection = Vector3.ProjectOnPlane(stepTarget.transform.position - transform.position, Vector3.up).normalized;
        attackIntentDirection = lungeDirection;
        lungePlannedDistance = Mathf.Min(Mathf.Max(0f, freeFlowTravelDistance), Mathf.Max(0f, gap - LungeStopDistance));
        lungeTravelRemaining = lungePlannedDistance;
    }

    private bool IsLungeTargetValid(MushroomMonster monster)
    {
        if (monster == null || !monster.isActiveAndEnabled || !monster.IsTargetVisible || monster.Health == null || monster.Health.Health <= 0f)
            return false;
        var collider = monster.GetComponent<Collider>();
        float range = Mathf.Max(AttackRange, freeFlowSearchRadius) + (lungeTracking && monster == stepTarget ? Mathf.Max(0f, lungeTrackingDistance) : 0f);
        if (!IsColliderInRange(collider, range, out _)) return false;
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        Vector3 delta = collider.ClosestPoint(origin) - origin;
        if (Vector3.Angle(attackIntentDirection, Vector3.ProjectOnPlane(delta, Vector3.up)) > 120f) return false;
        return HasClearStrikePath(collider, monster.transform);
    }

    private bool HasClearStrikePath(Collider collider, Transform target)
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        Vector3 delta = collider.ClosestPoint(origin) - origin;
        if (delta.sqrMagnitude < .0001f) return true;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, lungeOcclusionHits, delta.magnitude,
            lungeBlockingLayers, QueryTriggerInteraction.Ignore);
        if (count == lungeOcclusionHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform obstacle = lungeOcclusionHits[i].collider.transform;
            if (obstacle == transform || obstacle.IsChildOf(transform) || obstacle == target ||
                obstacle.IsChildOf(target) || obstacle.GetComponentInParent<TowerRuntime>() != null) continue;
            return false;
        }
        return true;
    }

    private void BeginSoftAim()
    {
        softAimElapsed = 0f;
        softAimStartRotation = transform.rotation;
        attackAimTarget = freeFlowEnabled ? stepTarget : FindBestSoftAimTarget();
        softAimActive = attackAimTarget != null;
        if (movement != null) movement.ExternalFacing = IsShiftLocked || ControlsStrikeFacing;
    }

    private MushroomMonster FindBestSoftAimTarget()
    {
        return FindNearestMonster();
    }

    private Vector3 ResolveAttackDirection(out bool hasInput)
    {
        Vector2 input = locomotionInput != null ? locomotionInput.move : Vector2.zero;
        hasInput = input.sqrMagnitude > .01f;
        if (!hasInput) return StrikeForward;
        if (attackCamera == null && Camera.main != null) attackCamera = Camera.main.transform;
        float yaw = attackCamera != null ? attackCamera.eulerAngles.y : transform.eulerAngles.y;
        return Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y).normalized;
    }

    private MushroomMonster FindDirectionalTarget(Vector3 intent, bool hasInput)
    {
        MushroomMonster best = null;
        float bestScore = float.PositiveInfinity;
        float range = Mathf.Min(Mathf.Max(AttackRange, freeFlowSearchRadius), AttackRange + Mathf.Max(0f, freeFlowTravelDistance));
        foreach (var monster in MushroomMonster.Monsters)
        {
            if (monster == null || !monster.isActiveAndEnabled || !monster.IsTargetVisible ||
                monster.Health == null || monster.Health.Health <= 0f) continue;
            var collider = monster.GetComponent<Collider>();
            if (!IsColliderInRange(collider, range, out float distance) ||
                !CombatTargetSelection.TryScore(intent, monster.transform.position-transform.position,
                    hasInput ? directionalAcquireAngle : idleAcquireAngle, Mathf.Sqrt(distance),
                    targetDistanceWeight, out float score) || !HasClearStrikePath(collider, monster.transform)) continue;
            // Registry insertion order is stable; equal scores retain the first candidate.
            if (score < bestScore)
            { best=monster;bestScore=score; }
        }
        return best;
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
        if (movement != null) movement.ExternalFacing = IsShiftLocked || ControlsStrikeFacing;
    }

}
