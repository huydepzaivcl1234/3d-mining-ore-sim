using UnityEngine;

public partial class PlayerCombatInput
{
    [Header("Directional movement blending")]
    [Min(.01f), SerializeField] private float directionBlendSeconds = .1f;
    [Range(0f, 1f), SerializeField] private float strafeSideThreshold = .35f;
    [Range(0f, 1f), SerializeField] private float strafeBackwardThreshold = .15f;
    [Range(0f, 1f), SerializeField] private float lungeContactPhase = .45f;
    [Range(0f, 1f), SerializeField] private float turnContactPhase = .5f;
    private int locomotionStateHash;
    private static readonly int DirectionX = Animator.StringToHash("FootworkX");
    private static readonly int DirectionZ = Animator.StringToHash("FootworkZ");

    private void BindLocomotionAnimation()
    {
        if (movement == null) return;
        movement.MovementAnimationRequested -= PrepareLocomotionAnimation;
        movement.MovementAnimationRequested += PrepareLocomotionAnimation;
    }

    private void UnbindLocomotionAnimation()
    {
        if (movement != null) movement.MovementAnimationRequested -= PrepareLocomotionAnimation;
        ClearLocomotionAnimation(false);
    }

    private bool IsLocomotionState(int hash) => hash == locomotionStateHash && hash != 0;

    // The motor owns rotation and movement; only locked side/back input needs
    // an animation override. Forward and unlocked movement retain Base Walk/Run.
    private void PrepareLocomotionAnimation(Vector3 direction, bool sprint, float deltaTime)
    {
        if (!CanUseGameplay() || movement == null || !movement.Grounded || animator == null)
        { ClearLocomotionAnimation(true); return; }
        int layer = animator.GetLayerIndex(CombatLayerName);
        if (layer < 0) return;
        // A queued Animator trigger has not changed the visible state yet. The
        // motor's later callback must not replace the attack legs in that gap,
        // or crossfade back into strafe during strike recovery.
        if (freeFlowEnabled && ControlsStrikeFacing)
        { ClearLocomotionAnimation(false); return; }
        var state = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) :
            animator.GetCurrentAnimatorStateInfo(layer);
        if (IsAttackState(state)) { ClearLocomotionAnimation(false); return; }
        int arms = animator.GetLayerIndex("Arms Layer");
        if (combatMode && arms >= 0 && (animator.IsInTransition(arms) ||
            animator.GetCurrentAnimatorStateInfo(arms).shortNameHash != ArmedState)) return;

        bool forwardInput = locomotionInput != null && locomotionInput.move.y > strafeBackwardThreshold &&
            Mathf.Abs(locomotionInput.move.x) < strafeSideThreshold;
        if (!IsShiftLocked || direction.sqrMagnitude <= .01f || forwardInput)
        { ClearLocomotionAnimation(true); return; }

        Vector3 local = transform.InverseTransformDirection(direction.normalized);
        animator.SetFloat(DirectionX, local.x, directionBlendSeconds, deltaTime);
        animator.SetFloat(DirectionZ, local.z, directionBlendSeconds, deltaTime);
        const string stateName = "Directional Movement";
        int hash = Animator.StringToHash(stateName);
        if (locomotionStateHash == hash || footworkLayer < 0 || !animator.HasState(footworkLayer, hash)) return;
        locomotionStateHash = hash;
        if (combatMode && state.shortNameHash != CombatMoveState)
            animator.CrossFadeInFixedTime("Combat", BlendSeconds, layer, 0f);
        animator.CrossFadeInFixedTime(stateName, BlendSeconds, footworkLayer, 0f);
    }

    private void ClearLocomotionAnimation(bool returnToCombat)
    {
        if (returnToCombat && locomotionStateHash != 0 && animator != null)
        {
            int layer = animator.GetLayerIndex(CombatLayerName);
            if (layer >= 0 && (IsLocomotionState(animator.GetCurrentAnimatorStateInfo(layer).shortNameHash) ||
                animator.IsInTransition(layer) && IsLocomotionState(animator.GetNextAnimatorStateInfo(layer).shortNameHash)))
                animator.CrossFadeInFixedTime("Combat", BlendSeconds, layer, 0f);
        }
        locomotionStateHash = 0;
        if (movement != null)
        {
            movement.ExternalRotationSmoothTime = 0f;
            movement.LocomotionMoveMultiplier = 1f;
        }
    }

    // Retain the receiver for old imported clips; locomotion Turn is no longer routed.
    public void OnLocomotionTurnComplete() { }
}
