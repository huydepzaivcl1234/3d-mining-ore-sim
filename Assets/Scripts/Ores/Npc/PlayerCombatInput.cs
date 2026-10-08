using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public partial class PlayerCombatInput : MonoBehaviour
{
    // Names match the player's authored Player controller.controller on main.
    private const string CombatLayerName = "combat layer";
    private const string FootworkLayerName = "Combat Footwork";
    private static readonly int CombatStrikeRoute = Animator.StringToHash("CombatStrike");
    private bool usesCombatStrikeRoute;
    private static readonly int CombatMoveState = Animator.StringToHash("Combat");
    private static readonly int FirstAttackState = Animator.StringToHash("Sword Attack 1");
    private static readonly int SecondAttackState = Animator.StringToHash("Sword Attack 2");
    private static readonly int ThirdAttackState = Animator.StringToHash("Special Attack");
    private static readonly int LungeAttackState = Animator.StringToHash("lunge attack");
    private static readonly int TurnAttackState = Animator.StringToHash("turn attack");
    // Keep the designer's state names on both body layers. A new combo starts
    // at 1; each accepted follow-up advances exactly one strike.
    private static readonly string[] AttackStates = { "Sword Attack 1", "Sword Attack 2", "Special Attack" };
    private static readonly int ArmedState = Animator.StringToHash("Combat");
    [SerializeField] private Animator animator;
    [Header("Input bindings - keyboard or mouse")]
    [SerializeField] private InputAction toggleCombat = new InputAction(
        "Toggle Combat", InputActionType.Button, "<Keyboard>/e");
    [SerializeField] private InputAction attack = new InputAction(
        "Attack", InputActionType.Button, "<Mouse>/leftButton");
    [HideInInspector, SerializeField] private InputAction autoAim = new InputAction(
        "Auto Aim", InputActionType.Button, "<Keyboard>/f");
    [SerializeField] private string drawWeaponParameter = "DrawWeapon";
    [SerializeField] private string sheathWeaponParameter = "SheathWeapon";
    public bool IsCombatMode => combatMode;
    public bool IsTrackingLunge => lungeTracking;
    [HideInInspector, Min(0.1f), SerializeField] private float aimRange = 15f; // Legacy; targeting now uses AttackRange.
    [Min(0f), SerializeField] private float aimTurnSpeed = 720f;
    [Header("Soft aim while attacking (does not control the camera)")]
    [HideInInspector, Min(0.1f), SerializeField] private float softAimRadius = 8f; // Retained for scene compatibility only.
    [Tooltip("Maximum time to turn toward the target at the start of each strike.")]
    [Min(0.01f), SerializeField] private float softAimDuration = 0.15f;
    private readonly Collider[] softAimHits = new Collider[64];
    private bool softAimActive;
    private float softAimElapsed;
    private Quaternion softAimStartRotation;
    private MiningOrbitCamera orbitCamera;
    public bool IsShiftLocked => orbitCamera != null && orbitCamera.IsShiftLocked;
    private MushroomMonster attackAimTarget;
    private MushroomMonster aimedMonster;
    private StarterAssets.ThirdPersonController movement;
    public MushroomMonster AimedMonster => aimedMonster;
    private MiningCharacterHealth ownHealth;
    private MiningUiPanelCoordinator panels;
    private bool combatMode;
    [Header("Attack")]
    [Min(0f), SerializeField] private float damage = 10f;
    [Min(0.1f), SerializeField] private float attackRange = 1.75f;
    [Range(1f, 180f), SerializeField] private float attackAngle = 100f;
    [Tooltip("Animation playback multiplier: 1 = original, 2 = twice as fast.")]
    [Min(0.1f), SerializeField] private float attackSpeed = 1f;
    [Tooltip("Seconds to blend between full-body locomotion and the combat upper body.")]
    [Min(0.01f), SerializeField] private float combatBlendSeconds = 0.15f;
    [HideInInspector, Range(0f, 1f), SerializeField] private float hitTime = 0.45f; // Legacy serialized value; contact is set by Animation Events.
    [SerializeField] private Vector3 hitOriginOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private LayerMask targetLayers = ~0;

    private bool wasAttacking;
    private bool queuedAttack;
    private int queuedAttackStateHash;
    private int lastAttackStateHash;
    private bool returningFromAttack;
    private MiningAudioManager feedbackAudio;
    private MiningPlayerStats playerStats;
    private MiningPlayerStats PlayerStats => playerStats != null ? playerStats : playerStats = GetComponent<MiningPlayerStats>();
    private MiningPlayerStatsData Stats => PlayerStats != null ? PlayerStats.Data : null;
    public float Damage => Stats != null ? PlayerStats.Damage : Mathf.Max(0, damage);
    public float AttackRange => Mathf.Max(0.1f, Stats != null ? Stats.attackRange : attackRange);
    public float AttackAngle => Mathf.Clamp(Stats != null ? Stats.attackAngle : attackAngle, 1, 180);
    public float AttackSpeed => Mathf.Max(0.1f, Stats != null ? PlayerStats.AttackSpeed : attackSpeed);
    private float BlendSeconds => Mathf.Max(0.01f, Stats != null ? Stats.combatBlendSeconds : combatBlendSeconds);
    private Vector3 HitOriginOffset => Stats != null ? Stats.hitOriginOffset : hitOriginOffset;
    private float HitHalfHeight => 0.9f;
    private Vector3 StrikeForward => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    private bool hitApplied;
    private readonly HashSet<MiningCharacterHealth> hitTargets = new HashSet<MiningCharacterHealth>();
    [Header("Free-flow footwork (camera remains independent)")]
    [SerializeField] private bool freeFlowEnabled = true;
    [Range(0f, 1f), SerializeField] private float movingBodyWeight = 0.9f;
    [Range(0f, 1f), SerializeField] private float strikeMovementMultiplier = 0.15f;
    [Min(0.01f), SerializeField] private float footworkBlendSeconds = 0.08f;
    [Tooltip("Maximum travel per strike; actual travel stops before the enemy collider.")]
    [Min(0f), SerializeField] private float strikeStepDistance = 1.15f;
    [Min(0f), SerializeField] private float targetClearance = 0.3f;
    [Header("Quick smooth lunge (damage range is unchanged)")]
    [Min(0f), SerializeField] private float lungeExtraRange = 0.55f;
    [Tooltip("Time for one lunge at attack speed 1. Scales with attack speed.")]
    [Min(0.01f), SerializeField] private float lungeSeconds = 0.18f;
    [Min(0.1f), SerializeField] private float lungeMaximumSpeed = 9f;
    [Tooltip("Limits extra acquisition range at high attack speed so the lunge can still reach before contact.")]
    [Range(0.1f, 1f), SerializeField] private float lungeReachSafety = 0.5f;
    [Range(0.1f, 1f), SerializeField] private float lungeStopRangeFraction = 0.6f;
    [Min(0.01f), SerializeField] private float lungeDirectionSmoothSeconds = 0.04f;
    [Tooltip("Extra travel budget for following the selected moving enemy before contact. Does not increase damage range.")]
    [Min(0f), SerializeField] private float lungeTrackingDistance = .75f;
    [SerializeField] private LayerMask lungeBlockingLayers = ~0;
    [Range(0f, 180f), SerializeField] private float maximumStepAngle = 60f;
    [Range(0f, 1f), SerializeField] private float stepStartPhase = 0.1f;
    [Tooltip("Match the contact Animation Event in each attack. Values are normalized clip time.")]
    [Range(0f, 1f), SerializeField] private float firstStrikeContactPhase = 0.46f;
    [Range(0f, 1f), SerializeField] private float secondStrikeContactPhase = 0.67f;
    [Tooltip("Footwork recovery phase for attack 3 only. Damage is exclusively applied by OnSwordStrikeThird.")]
    [Range(0f, 1f), SerializeField] private float thirdStrikeContactPhase = 0.677f;
    [Range(0f, 1f), SerializeField] private float recoveryDelayPhase = 0.06f;
    [Range(0f, 1f), SerializeField] private float recoveryEndPhase = 0.96f;
    [Min(0.01f), SerializeField] private float attackTransitionSeconds = 0.07f;
    [Range(0f, 1f), SerializeField] private float comboQueueStart = 0.45f;
    [Range(0f, 1f), SerializeField] private float comboLinkTime = 0.78f;
    [Range(0f, 1f), SerializeField] private float comboQueueEnd = 0.97f;
    [Range(0f, 1f), SerializeField] private float attackReturnPhase = 0.98f;
    [Min(0.01f), SerializeField] private float comboBufferSeconds = 0.25f;
    private float queuedAttackUntil;
    private int footworkLayer = -1;
    private StarterAssets.StarterAssetsInputs locomotionInput;
    private MushroomMonster stepTarget;
    private readonly RaycastHit[] lungeOcclusionHits = new RaycastHit[16];
    private float lungeProgress;
    private float lungePlannedDistance;
    private float lungeTravelRemaining;
    private Vector3 lungeDirection;
    private bool lungeTracking;
    public float LungeAcquireRange => AttackRange + Mathf.Min(
        Mathf.Min(Mathf.Max(0f, lungeExtraRange), Mathf.Max(0f, strikeStepDistance)),
        Mathf.Max(0f, lungeMaximumSpeed) * Mathf.Max(0.01f, lungeSeconds) / AttackSpeed * lungeReachSafety);

    private void OnEnable()
    {
        ResetFootwork();
        BindLocomotionAnimation();
        toggleCombat?.Enable();
        attack?.Enable();
    }
    private void OnDisable()
    {
        UnbindLocomotionAnimation();
        EndSwordTrail();
        StopAllCoroutines();
        ResetFootwork();
        toggleCombat?.Disable();
        attack?.Disable();
        autoAim?.Disable();
        ClearAim();
        wasAttacking = false;
        hitApplied = false;
        queuedAttack = false;
        queuedAttackStateHash = 0;
        returningFromAttack = false;
        lastAttackStateHash = 0;
        SetCombatMode(false);
    }

    private void LateUpdate()
    {
        UpdateSwordTrail();
        UpdateFootwork();
        if (!CanUseGameplay())
        {
            ClearAim();
            return;
        }
        // Footwork owns facing during the lunge; ordinary soft aim and camera lock must not fight it.
        if (lungeTracking) return;
        if (IsShiftLocked) { StopSoftAim(); return; }
        if (aimedMonster != null && !IsAimValid(aimedMonster)) aimedMonster = null;
        int layer = animator != null ? animator.GetLayerIndex(CombatLayerName) : -1;
        bool swinging = layer >= 0 && (IsAttackState(animator.GetCurrentAnimatorStateInfo(layer)) ||
            animator.IsInTransition(layer) && IsAttackState(animator.GetNextAnimatorStateInfo(layer)));
        if (!combatMode || !swinging)
        {
            StopSoftAim();
            return;
        }

        // Acquire once per strike, not every frame: nearby enemies cannot spin us
        // between targets. Shift lock disables this assistance entirely.
        if (!softAimActive || !(attackAimTarget == stepTarget ? IsLungeTargetValid(attackAimTarget) :
            IsAimValid(attackAimTarget))) { StopSoftAim(); return; }
        softAimActive = true;
        if (movement != null) movement.ExternalFacing = true;
        Vector3 direction = attackAimTarget.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        softAimElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(softAimElapsed / Mathf.Max(.01f, softAimDuration));
        Quaternion smooth = Quaternion.Slerp(softAimStartRotation, targetRotation, Mathf.SmoothStep(0f, 1f, progress));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, smooth, aimTurnSpeed * Time.deltaTime);
        if (progress >= 1f) StopSoftAim();
    }
    private void OnDestroy()
    {
        swordTrail?.Dispose();
        toggleCombat?.Dispose();
        attack?.Dispose();
        autoAim?.Dispose();
    }
    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        feedbackAudio = FindFirstObjectByType<MiningAudioManager>();
        ownHealth = GetComponent<MiningCharacterHealth>();
        movement = GetComponent<StarterAssets.ThirdPersonController>();
        locomotionInput = GetComponent<StarterAssets.StarterAssetsInputs>();
        footworkLayer = animator != null ? animator.GetLayerIndex(FootworkLayerName) : -1;
        usesCombatStrikeRoute = animator != null && HasParameter("CombatStrike", AnimatorControllerParameterType.Int);
        ResetFootwork();
        panels = FindFirstObjectByType<MiningUiPanelCoordinator>();
        foreach (var rig in FindObjectsByType<MiningOrbitCamera>(FindObjectsSortMode.None))
            if (rig.FollowTarget == transform) { orbitCamera = rig; break; }
    }
    private void Update()
    {
        if (!CanUseGameplay()) { ClearAim(); return; }
        aimedMonster = combatMode ? FindNearestMonster() : null;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int layer = animator.GetLayerIndex(CombatLayerName);
        if (HasParameter("AttackSpeed", AnimatorControllerParameterType.Float)) animator.SetFloat("AttackSpeed", AttackSpeed);
        if (toggleCombat != null && toggleCombat.WasPressedThisFrame())
        {
            TryToggleCombat();
        }
        if (layer >= 0) TrackAttack(layer);
        if (layer < 0) return;
        bool pressed = attack != null && attack.WasPressedThisFrame() &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        var state = animator.GetCurrentAnimatorStateInfo(layer);
        bool swinging = IsAttackState(state);
        // Accept one deliberate follow-up after the first strike begins. The
        // previous 0.55-0.9 window was shorter than the player's click timing
        // at higher attack speeds, so Sword Attack 2 was often skipped.
        if (combatMode && pressed && swinging && !animator.IsInTransition(layer) &&
            state.normalizedTime >= comboQueueStart && state.normalizedTime < comboQueueEnd)
        {
            queuedAttack = true;
            queuedAttackStateHash = state.shortNameHash;
            // Long authored openers must retain a deliberate click until their link frame.
            float secondsToLink = Mathf.Max(0f, comboLinkTime - state.normalizedTime) * state.length /
                Mathf.Max(.01f, state.speed * state.speedMultiplier * animator.speed);
            queuedAttackUntil = Time.time + Mathf.Max(comboBufferSeconds, secondsToLink + attackTransitionSeconds);
        }
        if (swinging && !animator.IsInTransition(layer))
        {
            if (queuedAttack && (queuedAttackStateHash != state.shortNameHash || Time.time > queuedAttackUntil)) queuedAttack = false;
            if (combatMode && queuedAttack && hitApplied && state.normalizedTime >= comboLinkTime)
            {
                queuedAttack = false;
                PlayAttack(layer, NextAttackIndex(state.shortNameHash));
            }
            else if (!queuedAttack &&
                     !returningFromAttack && state.normalizedTime >= attackReturnPhase)
            {
                returningFromAttack = true;
                // Attack 2 has no Move transition in the authored controller.
                // Return explicitly without rewriting the user's Animator.
                animator.CrossFadeInFixedTime("Combat", attackTransitionSeconds, layer, 0f);
            }
        }
        int armsLayer = animator.GetLayerIndex("Arms Layer");
        bool swordReady = armsLayer < 0 || (!animator.IsInTransition(armsLayer) &&
            animator.GetCurrentAnimatorStateInfo(armsLayer).shortNameHash == ArmedState);
        if (!combatMode || !swordReady || swinging || animator.IsInTransition(layer) ||
            (state.shortNameHash != CombatMoveState && !IsLocomotionState(state.shortNameHash)) || !pressed) return;
        queuedAttack = false;
        PlayAttack(layer, 0, true);
    }

    private static int NextAttackIndex(int stateHash)
    {
        if (stateHash == LungeAttackState) return -1;
        if (stateHash == TurnAttackState) return 0;
        for (int i = 0; i < AttackStates.Length; i++)
            if (Animator.StringToHash(AttackStates[i]) == stateHash) return (i + 1) % AttackStates.Length;
        return 0;
    }

    private void PlayAttack(int layer, int index, bool opening = false)
    {
        queuedAttack = false;
        queuedAttackStateHash = 0;
        hitApplied = false;
        returningFromAttack = false;
        BeginLunge();
        if (opening && lungeTravelRemaining > 0.01f &&
            animator.HasState(layer, LungeAttackState)) index = -2;
        // Only the actual lunge opener travels. Turn/combo retain normal damage range.
        lungeTracking = index == -2;
        if (!lungeTracking) lungeTravelRemaining = 0f;
        else lungeTravelRemaining += Mathf.Max(0f, lungeTrackingDistance);
        ClearLocomotionAnimation(false);
        BeginSoftAim();
        animator.ResetTrigger("attack");
        animator.ResetTrigger("Move");
        string stateName = index == -2 ? "lunge attack" : index == -1 ? "turn attack" : AttackStates[index];
        if (usesCombatStrikeRoute)
        {
            animator.SetInteger(CombatStrikeRoute, index);
            animator.SetTrigger("attack");
        }
        else animator.CrossFadeInFixedTime(stateName, attackTransitionSeconds, layer, 0f);
        if (footworkLayer >= 0)
            animator.CrossFadeInFixedTime(stateName, attackTransitionSeconds, footworkLayer, 0f);
    }

    private bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        foreach (var parameter in animator.parameters)
            if (parameter.name == name && parameter.type == type) return true;
        return false;
    }

    // Draw/sheath triggers are shared by two independently evaluated layers.
    // Do not replace them midway through an equip clip or an attack. Rapid
    // presses are ignored rather than queued for a surprise later toggle.
    public bool TryToggleCombat()
    {
        if (!CanUseGameplay() || animator == null || animator.runtimeAnimatorController == null)
            return false;
        int expected = combatMode ? ArmedState : Animator.StringToHash("Default");
        foreach (string layerName in new[] { CombatLayerName, "Arms Layer" })
        {
            int layer = animator.GetLayerIndex(layerName);
            if (layer >= 0 && (animator.IsInTransition(layer) ||
                animator.GetCurrentAnimatorStateInfo(layer).shortNameHash != expected))
                return false;
        }
        SetCombatMode(!combatMode);
        return true;
    }

    public void SetCombatMode(bool enabled)
    {
        if (!enabled) EndSwordTrail();
        bool changed = combatMode != enabled;
        combatMode = enabled;
        if (!enabled) { ClearAim(); ResetFootwork(); ClearLocomotionAnimation(false); queuedAttack = false; queuedAttackStateHash = 0; }
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (HasParameter(drawWeaponParameter, AnimatorControllerParameterType.Bool))
            animator.SetBool(drawWeaponParameter, enabled);
        if (HasParameter("CombatMode", AnimatorControllerParameterType.Bool))
            animator.SetBool("CombatMode", enabled);
        if (HasParameter(drawWeaponParameter, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(drawWeaponParameter);
        string sheathTrigger = HasParameter(sheathWeaponParameter, AnimatorControllerParameterType.Trigger)
            ? sheathWeaponParameter : "ShealthWeapon";
        if (HasParameter(sheathTrigger, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(sheathTrigger);
        if (changed)
        {
            string trigger = enabled ? drawWeaponParameter : sheathTrigger;
            if (HasParameter(trigger, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(trigger);
        }
        if (HasParameter("attack", AnimatorControllerParameterType.Trigger)) animator.ResetTrigger("attack");
        int combatLayer = animator.GetLayerIndex(CombatLayerName);
        if (!enabled && combatLayer >= 0 &&
            IsAttackState(animator.GetCurrentAnimatorStateInfo(combatLayer)) &&
            HasParameter("Move", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("Move");
    }

    private void TrackAttack(int layer)
    {
        var state = animator.GetCurrentAnimatorStateInfo(layer);
        if (animator.IsInTransition(layer))
        {
            var next = animator.GetNextAnimatorStateInfo(layer);
            if (IsAttackState(next)) state = next;
        }
        bool active = combatMode && IsAttackState(state);
        if (active && (!wasAttacking || lastAttackStateHash != state.shortNameHash))
        {
            hitApplied = false;
            returningFromAttack = false;
            if (feedbackAudio != null)
                feedbackAudio.PlayWorldSfx(Stats != null ? Stats.attackSfx : null, transform.position,
                    Stats != null ? Stats.attackSfxVolume : 1f);
        }
        wasAttacking = active;
        lastAttackStateHash = active ? state.shortNameHash : 0;
        if (!active)
        {
            EndSwordTrail();
            if (movement != null && !softAimActive) movement.ExternalFacing = IsShiftLocked;
            returningFromAttack = false;
            queuedAttack = false;
            queuedAttackStateHash = 0;
        }
    }

    private static bool IsAttackState(AnimatorStateInfo state) =>
        state.shortNameHash == FirstAttackState || state.shortNameHash == SecondAttackState ||
        state.shortNameHash == ThirdAttackState || state.shortNameHash == LungeAttackState ||
        state.shortNameHash == TurnAttackState;

    // Called by events on the imported sword clips, exactly when the blade reaches the target.
    public void OnSwordStrikeDown() => ApplyAnimationHit(FirstAttackState, true);
    public void OnSwordSweepUp() => ApplyAnimationHit(SecondAttackState, true);
    public void OnLungeAttackHit() => ApplyAnimationHit(LungeAttackState, true);
    public void OnTurnAttackHit() => ApplyAnimationHit(TurnAttackState, true);
    public void OnSpecialAttackHit() => OnSwordStrikeThird();
    public void OnSwordStrikeThird()
    {
        ApplyAnimationHit(ThirdAttackState, true);
    }

    private void ApplyAnimationHit(int expectedState, bool sweep)
    {
        if (!combatMode || hitApplied || !CanUseGameplay() || animator == null) return;
        int layer = animator.GetLayerIndex(CombatLayerName);
        if (layer < 0) return;
        bool current = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == expectedState;
        bool next = animator.IsInTransition(layer) &&
            animator.GetNextAnimatorStateInfo(layer).shortNameHash == expectedState;
        if (!current && !next) return;
        FinishSwordTrail();
        hitApplied = true;
        if (expectedState == LungeAttackState)
        {
            lungeTracking = false;
            lungeTravelRemaining = 0f;
            if (movement != null) movement.CombatStepVelocity = Vector3.zero;
            StopSoftAim();
        }
        float multiplier = expectedState == ThirdAttackState && Stats != null
            ? 1f + Mathf.Max(0f, Stats.thirdAttackDamageBonusPercent) * .01f : 1f;
        if (sweep) ApplySweepHit(multiplier);
        else ApplyHit();
    }

    private bool CanUseGameplay() => Time.timeScale > 0f &&
        (ownHealth == null || ownHealth.Health > 0f) &&
        (panels == null || !panels.BlocksGameplay);

}
