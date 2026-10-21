using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private void ResetFootwork()
    {
        strikeCommitted = strikeAwaitingAnimator = false;
        wasAttacking = false;
        stepTarget = null;
        lungeHasTarget = false;
        lungeTracking = false;
        lungeProgress = lungePlannedDistance = lungeTravelRemaining = 0f;
        lungeDirection = Vector3.zero;
        if (movement != null)
        {
            movement.CombatMoveMultiplier = 1f;
            movement.CombatStepVelocity = Vector3.zero;
            movement.ExternalFacing = IsShiftLocked;
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
        if (strikeAwaitingAnimator && !IsAttackState(state))
        {
            movement.CombatMoveMultiplier = 0f;
            movement.CombatStepVelocity = Vector3.zero;
            movement.ExternalFacing = true;
            return; // The launch has not started; don't cancel its budget before Animator evaluates the trigger.
        }
        bool active = freeFlowEnabled && combatMode && CanUseGameplay() && movement.Grounded && IsAttackState(state);
        float phase = Mathf.Clamp01(state.normalizedTime);
        float contact = state.shortNameHash == ThirdAttackState ? thirdStrikeContactPhase :
            state.shortNameHash == LungeAttackState ? lungeContactPhase :
            state.shortNameHash == TurnAttackState ? turnContactPhase :
            state.shortNameHash == SecondAttackState ? secondStrikeContactPhase : firstStrikeContactPhase;
        // Plant the feet through contact, then give locomotion back during recovery.
        float recoveryStart = Mathf.Min(contact + recoveryDelayPhase, recoveryEndPhase - 0.001f);
        float recovery = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(recoveryStart, recoveryEndPhase, phase));
        float desired = active ? 1f - recovery :
            locomotionStateHash != 0 ? 1f : 0f;
        float weight = Mathf.MoveTowards(animator.GetLayerWeight(footworkLayer), desired,
            Time.deltaTime / Mathf.Max(0.01f, footworkBlendSeconds));
        animator.SetLayerWeight(footworkLayer, weight);
        // Never add a newly camera-relative walk vector to a committed strike.
        movement.CombatMoveMultiplier = freeFlowEnabled && ControlsStrikeFacing ? 0f : 1f;
        movement.ExternalFacing = IsShiftLocked || ControlsStrikeFacing;
        movement.CombatStepVelocity = Vector3.zero;
        bool lunging = active && !hitApplied && phase < contact;
        if (lungeTracking && (!lunging || lungeHasTarget && !IsLungeTargetValid(stepTarget)))
        {
            // Cancellation is terminal for this strike: don't resume across a wall or a dead target.
            lungeTracking = false;
            lungeTravelRemaining = 0f;
            StopSoftAim();
        }
        if (lungeTracking && phase >= stepStartPhase && Time.deltaTime > 0f)
        {
            Vector3 origin = transform.TransformPoint(HitOriginOffset);
            Vector3 toTarget = stepTarget != null ? Vector3.ProjectOnPlane(stepTarget.GetComponent<Collider>().ClosestPoint(origin) - origin, Vector3.up)
                : lungeDirection * (lungeTravelRemaining + LungeStopDistance);
            float previous = lungeProgress;
            lungeProgress = Mathf.Clamp01(lungeProgress + Time.deltaTime * AttackSpeed / Mathf.Max(0.01f, lungeSeconds));
            float travel = CombatLungeMotion.TravelBetween(previous, lungeProgress, lungePlannedDistance);
            // After the short launch envelope, use the remaining budget to close a moving gap.
            if (previous >= 1f)
                travel = Mathf.Max(0f, toTarget.magnitude - LungeStopDistance) *
                    (1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(.01f, lungeDirectionSmoothSeconds)));
            Vector3 step = CombatLungeMotion.TrackStep(toTarget, LungeStopDistance, travel,
                lungeTravelRemaining, lungeMaximumSpeed, Time.deltaTime);
            if (toTarget.sqrMagnitude > .0001f)
            {
                // ClosestPoint controls clearance, not facing: near a collider edge
                // its direction can change abruptly. Face the selected actor's centre.
                lungeDirection = stepTarget != null
                    ? Vector3.ProjectOnPlane(stepTarget.transform.position - transform.position, Vector3.up).normalized
                    : attackIntentDirection;
                movement.ExternalFacing = true;
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(lungeDirection), aimTurnSpeed * Time.deltaTime);
            }
            // The existing CharacterController remains the only motor and resolves solid collisions.
            movement.CombatStepVelocity = step / Time.deltaTime;
            lungeTravelRemaining = Mathf.Max(0f, lungeTravelRemaining - step.magnitude);
        }
    }
}
