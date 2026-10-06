using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private void ResetFootwork()
    {
        stepTarget = null;
        lungeProgress = lungePlannedDistance = lungeTravelRemaining = 0f;
        lungeDirection = Vector3.zero;
        if (movement != null)
        {
            movement.CombatMoveMultiplier = 1f;
            movement.CombatStepVelocity = Vector3.zero;
        }
        if (animator != null && footworkLayer >= 0) animator.SetLayerWeight(footworkLayer, 0f);
    }

    private void UpdateFootwork()
    {
        if (animator == null || footworkLayer < 0 || movement == null) return;
        int layer = animator.GetLayerIndex(CombatLayerName);
        if (layer < 0) { ResetFootwork(); return; }
        var state = animator.GetCurrentAnimatorStateInfo(layer);
        if (animator.IsInTransition(layer) && IsAttackState(animator.GetNextAnimatorStateInfo(layer)))
            state = animator.GetNextAnimatorStateInfo(layer);
        bool active = freeFlowEnabled && combatMode && CanUseGameplay() && movement.Grounded && IsAttackState(state);
        float phase = Mathf.Clamp01(state.normalizedTime);
        float contact = state.shortNameHash == ThirdAttackState ? thirdStrikeContactPhase :
            state.shortNameHash == LungeAttackState ? lungeContactPhase :
            state.shortNameHash == TurnAttackState ? turnContactPhase :
            state.shortNameHash == SecondAttackState ? secondStrikeContactPhase : firstStrikeContactPhase;
        // Plant the feet through contact, then give locomotion back during recovery.
        float recoveryStart = Mathf.Min(contact + recoveryDelayPhase, recoveryEndPhase - 0.001f);
        float recovery = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(recoveryStart, recoveryEndPhase, phase));
        bool moving = locomotionInput != null && locomotionInput.move.sqrMagnitude > 0.01f;
        float desired = active ? (moving ? movingBodyWeight : 1f) * (1f - recovery) :
            locomotionStateHash != 0 ? 1f : 0f;
        float weight = Mathf.MoveTowards(animator.GetLayerWeight(footworkLayer), desired,
            Time.deltaTime / Mathf.Max(0.01f, footworkBlendSeconds));
        animator.SetLayerWeight(footworkLayer, weight);
        movement.CombatMoveMultiplier = active ? Mathf.Lerp(1f, strikeMovementMultiplier, weight) : 1f;
        movement.CombatStepVelocity = Vector3.zero;
        // Acquire only once per strike. Travel has its own short envelope rather
        // than waiting for a long attack clip, but damage still uses AttackRange.
        if (active && IsLungeTargetValid(stepTarget) && phase >= stepStartPhase && phase < contact &&
            lungeProgress < 1f && lungeTravelRemaining > 0f && Time.deltaTime > 0f)
        {
            var targetCollider = stepTarget.GetComponent<Collider>();
            Vector3 origin = transform.TransformPoint(HitOriginOffset);
            Vector3 toTarget = Vector3.ProjectOnPlane(targetCollider.ClosestPoint(origin) - origin, Vector3.up);
            float previous = lungeProgress;
            lungeProgress = Mathf.Clamp01(lungeProgress + Time.deltaTime * AttackSpeed / Mathf.Max(0.01f, lungeSeconds));
            float travel = CombatLungeMotion.TravelBetween(previous, lungeProgress, lungePlannedDistance);
            travel = Mathf.Min(Mathf.Min(travel, Mathf.Max(0f, lungeMaximumSpeed) * Time.deltaTime),
                Mathf.Min(lungeTravelRemaining, Mathf.Max(0f, toTarget.magnitude - LungeStopDistance)));
            if (toTarget.sqrMagnitude > 0.0001f)
                lungeDirection = Vector3.Slerp(lungeDirection, toTarget.normalized,
                    1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, lungeDirectionSmoothSeconds))).normalized;
            // Shift lock retains manual facing; do not pull the player sideways.
            Vector3 direction = IsShiftLocked ? StrikeForward : lungeDirection;
            if (Vector3.Angle(StrikeForward, toTarget) <= maximumStepAngle)
            {
                movement.CombatStepVelocity = direction * (travel / Time.deltaTime);
                lungeTravelRemaining -= travel;
            }
        }
    }
}
