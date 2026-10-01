using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class PlayerCombatInput : MonoBehaviour
{
    // Names match the player's authored Player controller.controller on main.
    private const string CombatLayerName = "combat layer";
    private static readonly int CombatMoveState = Animator.StringToHash("Combat");
    private static readonly int FirstAttackState = Animator.StringToHash("Sword Attack 1");
    private static readonly int SecondAttackState = Animator.StringToHash("Sword Attack 2");
    private static readonly int ThirdAttackState = Animator.StringToHash("Sword Attack 3");
    // This controller currently authors two sword attacks. Start every combo
    // with 1, then alternate to 2 only when the player queues another click.
    private static readonly string[] AttackStates = { "Sword Attack 1", "Sword Attack 2" };
    private static readonly int ArmedState = Animator.StringToHash("Combat");
    [SerializeField] private Animator animator;
    [Header("Input bindings - keyboard or mouse")]
    [SerializeField] private InputAction toggleCombat = new InputAction(
        "Toggle Combat", InputActionType.Button, "<Keyboard>/e");
    [SerializeField] private InputAction attack = new InputAction(
        "Attack", InputActionType.Button, "<Mouse>/leftButton");
    [SerializeField] private InputAction autoAim = new InputAction(
        "Auto Aim", InputActionType.Button, "<Keyboard>/f");
    [SerializeField] private string drawWeaponParameter = "DrawWeapon";
    [SerializeField] private string sheathWeaponParameter = "SheathWeapon";
    public bool IsCombatMode => combatMode;
    [HideInInspector, Min(0.1f), SerializeField] private float aimRange = 15f; // Legacy; targeting now uses AttackRange.
    [Min(0f), SerializeField] private float aimTurnSpeed = 720f;
    [Header("Soft aim while attacking (does not control the camera)")]
    [HideInInspector, Min(0.1f), SerializeField] private float softAimRadius = 8f; // Retained for scene compatibility only.
    [Tooltip("Maximum time to turn toward the target at the start of each strike.")]
    [Min(0.01f), SerializeField] private float softAimDuration = 0.15f;
    private readonly Collider[] softAimHits = new Collider[64];
    private bool softAimActive;
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
    [Header("Slash SFX per attack (empty = PlayerStatsData.attackSfx)")]
    [SerializeField] private AudioClip slash1;
    [SerializeField] private AudioClip slash2;
    [SerializeField] private AudioClip slash3;

    [SerializeField] private ParticleSystem slashDownVfx;
    [SerializeField] private ParticleSystem slashUpVfx;

    [System.Serializable]
    private struct SlashVfxCue
    {
        public ParticleSystem effect;
        [Min(0f)] public float delay;
    }

    [Header("Additional slash VFX (add as many scene effects as needed)")]
    [SerializeField] private List<SlashVfxCue> slashDownEffects = new List<SlashVfxCue>();
    [SerializeField] private List<SlashVfxCue> slashUpEffects = new List<SlashVfxCue>();


    private bool wasAttacking;
    private bool queuedAttack;
    private int queuedAttackStateHash;
    private int lastAttackStateHash;
    private bool returningFromAttack;
    private MiningAudioManager feedbackAudio;
    private MiningPlayerStatsData Stats => MiningPlayerStats.For(this);
    public float Damage => Stats != null ? GetComponent<MiningPlayerStats>().Damage : Mathf.Max(0, damage);
    public float AttackRange => Mathf.Max(0.1f, Stats != null ? Stats.attackRange : attackRange);
    public float AttackAngle => Mathf.Clamp(Stats != null ? Stats.attackAngle : attackAngle, 1, 180);
    public float AttackSpeed => Mathf.Max(0.1f, Stats != null ? Stats.attackSpeed : attackSpeed);
    private float BlendSeconds => Mathf.Max(0.01f, Stats != null ? Stats.combatBlendSeconds : combatBlendSeconds);
    private Vector3 HitOriginOffset => Stats != null ? Stats.hitOriginOffset : hitOriginOffset;
    private float HitHalfHeight => 0.9f;
    private Vector3 StrikeForward => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    private bool hitApplied;
    private void OnEnable()
    {
        toggleCombat?.Enable();
        attack?.Enable();
        autoAim?.Enable();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
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
        if (!CanUseGameplay())
        {
            ClearAim();
            return;
        }
        if (aimedMonster != null && !IsAimValid(aimedMonster)) aimedMonster = null;
        int layer = animator != null ? animator.GetLayerIndex(CombatLayerName) : -1;
        bool swinging = layer >= 0 && (IsAttackState(animator.GetCurrentAnimatorStateInfo(layer)) ||
            animator.IsInTransition(layer) && IsAttackState(animator.GetNextAnimatorStateInfo(layer)));
        if (!combatMode || !swinging)
        {
            StopSoftAim();
            return;
        }

        // Re-evaluate the nearest reachable enemy, not a sticky distant lock.
        attackAimTarget = FindBestSoftAimTarget();
        if (attackAimTarget == null) { StopSoftAim(); return; }
        softAimActive = true;
        if (movement != null) movement.ExternalFacing = true;
        Vector3 direction = attackAimTarget.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        float turnSpeed = Mathf.Max(aimTurnSpeed, angle / Mathf.Max(0.01f, softAimDuration));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
            turnSpeed * Time.deltaTime);
    }
    private void OnDestroy()
    {
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
        panels = FindFirstObjectByType<MiningUiPanelCoordinator>();
    }
    private void Update()
    {
        if (!CanUseGameplay()) { ClearAim(); return; }
        aimedMonster = combatMode ? FindNearestMonster() : null;
        if (combatMode && autoAim != null && autoAim.WasPressedThisFrame())
            aimedMonster = FindNearestMonster();
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
            state.normalizedTime >= 0.3f && state.normalizedTime < 0.97f)
        {
            queuedAttack = true;
            queuedAttackStateHash = state.shortNameHash;
        }
        if (swinging && !animator.IsInTransition(layer))
        {
            if (queuedAttack && queuedAttackStateHash != state.shortNameHash) queuedAttack = false;
            if (combatMode && queuedAttack && state.normalizedTime >= 0.7f)
            {
                queuedAttack = false;
                PlayAttack(layer, state.shortNameHash == FirstAttackState ? 1 : 0);
            }
            else if (state.shortNameHash == FirstAttackState && !queuedAttack &&
                     !returningFromAttack && state.normalizedTime >= 0.98f)
            {
                returningFromAttack = true;
                if (HasParameter("Move", AnimatorControllerParameterType.Trigger)) animator.SetTrigger("Move");
            }
            // The third imported clip has no authored contact event. Use its
            // normalized strike frame once; existing clips retain their events.
            if (state.shortNameHash == ThirdAttackState &&
                state.normalizedTime >= 0.45f && !hitApplied)
                ApplyAnimationHit(ThirdAttackState, true);
        }
        int armsLayer = animator.GetLayerIndex("Arms Layer");
        bool swordReady = armsLayer < 0 || (!animator.IsInTransition(armsLayer) &&
            animator.GetCurrentAnimatorStateInfo(armsLayer).shortNameHash == ArmedState);
        if (!combatMode || !swordReady || swinging || animator.IsInTransition(layer) ||
            state.shortNameHash != CombatMoveState || !pressed) return;
        queuedAttack = false;
        PlayAttack(layer, 0);
    }

    private void PlayAttack(int layer, int index)
    {
        queuedAttack = false;
        queuedAttackStateHash = 0;
        hitApplied = false;
        returningFromAttack = false;
        BeginSoftAim();
        animator.ResetTrigger("attack");
        animator.CrossFadeInFixedTime(AttackStates[index], 0.08f, layer, 0f);
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
        bool changed = combatMode != enabled;
        combatMode = enabled;
        if (!enabled) { ClearAim(); queuedAttack = false; queuedAttackStateHash = 0; }
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

    private bool IsAimValid(MushroomMonster monster)
    {
        if (monster == null || !monster.isActiveAndEnabled || monster.Health == null ||
            monster.Health.Health <= 0f) return false;
        return IsAimColliderInRange(monster.GetComponent<Collider>(), out _);
    }

    private bool IsAimColliderInRange(Collider collider, out float distanceSquared)
    {
        distanceSquared = float.PositiveInfinity;
        if (collider == null || !collider.enabled || collider.isTrigger ||
            (targetLayers.value & (1 << collider.gameObject.layer)) == 0) return false;
        Vector3 direction = collider.ClosestPoint(transform.TransformPoint(HitOriginOffset)) -
            transform.TransformPoint(HitOriginOffset);
        if (Mathf.Abs(direction.y) > HitHalfHeight) return false;
        direction.y = 0f;
        distanceSquared = direction.sqrMagnitude;
        return distanceSquared <= AttackRange * AttackRange;
    }

    private MushroomMonster FindNearestMonster()
    {
        MushroomMonster nearest = null;
        float distance = float.PositiveInfinity;
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        int count = Physics.OverlapCapsuleNonAlloc(origin - Vector3.up * HitHalfHeight,
            origin + Vector3.up * HitHalfHeight, AttackRange, softAimHits, targetLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var collider = softAimHits[i];
            var monster = collider.GetComponentInParent<MushroomMonster>();
            if (monster == null || !monster.isActiveAndEnabled || monster.Health == null ||
                monster.Health.Health <= 0f || !IsAimColliderInRange(collider, out float candidate)) continue;
            if (candidate < distance) { distance = candidate; nearest = monster; }
        }
        // A full NonAlloc buffer is not guaranteed to contain the nearest collider.
        // The existing monster registry provides a non-allocating overflow fallback.
        if (count == softAimHits.Length)
            foreach (var monster in MushroomMonster.Monsters)
            {
                if (!IsAimValid(monster) || !IsAimColliderInRange(monster.GetComponent<Collider>(),
                        out float candidate) || candidate >= distance) continue;
                distance = candidate;
                nearest = monster;
            }
        return nearest;
    }

    private void BeginSoftAim()
    {
        attackAimTarget = FindBestSoftAimTarget();
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
        if (movement != null) movement.ExternalFacing = false;
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
            AudioClip slash = state.shortNameHash == FirstAttackState ? slash1 :
                state.shortNameHash == SecondAttackState ? slash2 : slash3;
            if (feedbackAudio != null)
                feedbackAudio.PlaySfx(slash != null ? slash : Stats != null ? Stats.attackSfx : null,
                    Stats != null ? Stats.attackSfxVolume : 1f);
        }
        wasAttacking = active;
        lastAttackStateHash = active ? state.shortNameHash : 0;
        if (!active)
        {
            if (movement != null && !softAimActive) movement.ExternalFacing = false;
            returningFromAttack = false;
            queuedAttack = false;
            queuedAttackStateHash = 0;
        }
    }

    private static bool IsAttackState(AnimatorStateInfo state) =>
        state.shortNameHash == FirstAttackState || state.shortNameHash == SecondAttackState ||
        state.shortNameHash == ThirdAttackState;

    // Called by events on the imported sword clips, exactly when the blade reaches the target.
    public void OnSwordStrikeDown() => ApplyAnimationHit(FirstAttackState, true);
    public void OnSwordSweepUp() => ApplyAnimationHit(SecondAttackState, true);

    private void ApplyAnimationHit(int expectedState, bool sweep)
    {
        if (!combatMode || hitApplied || !CanUseGameplay() || animator == null) return;
        int layer = animator.GetLayerIndex(CombatLayerName);
        if (layer < 0) return;
        bool current = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == expectedState;
        bool next = animator.IsInTransition(layer) &&
            animator.GetNextAnimatorStateInfo(layer).shortNameHash == expectedState;
        if (!current && !next) return;
        hitApplied = true;
        if (sweep) ApplySweepHit();
        else ApplyHit();
    }

    private void ApplyHit()
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        MiningCharacterHealth closest = null;
        Vector3 closestPoint = origin;
        float closestDistance = float.PositiveInfinity;
        // Broad phase covers a vertical band; horizontal angle must not reject
        // mushrooms simply because their collider is below the player's fist.
        foreach (var collider in Physics.OverlapCapsule(origin - Vector3.up * HitHalfHeight,
                     origin + Vector3.up * HitHalfHeight, AttackRange, targetLayers,
                     QueryTriggerInteraction.Ignore))
        {
            var target = collider.GetComponentInParent<MiningCharacterHealth>();
            if (target == null || target.Health <= 0 || target.transform == transform || target.transform.IsChildOf(transform)) continue;
            Vector3 point = collider.ClosestPoint(origin);
            Vector3 direction = point - origin;
            if (Mathf.Abs(direction.y) > HitHalfHeight) continue;
            direction.y = 0f;
            if (direction.sqrMagnitude > AttackRange * AttackRange) continue;
            if (direction.sqrMagnitude > 0.0001f &&
                Vector3.Angle(StrikeForward, direction) > Mathf.Min(30f, AttackAngle) * 0.5f) continue;
            if (direction.sqrMagnitude < closestDistance)
            {
                closest = target;
                closestDistance = direction.sqrMagnitude;
                closestPoint = point;
            }
        }
        if (closest != null) DamageTarget(closest, closestPoint);
    }

    private void ApplySweepHit()
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        var hitTargets = new HashSet<MiningCharacterHealth>();
        foreach (var collider in Physics.OverlapCapsule(origin - Vector3.up * HitHalfHeight,
                     origin + Vector3.up * HitHalfHeight, AttackRange, targetLayers,
                     QueryTriggerInteraction.Ignore))
        {
            var target = collider.GetComponentInParent<MiningCharacterHealth>();
            if (target == null || target.Health <= 0 || target.transform == transform ||
                target.transform.IsChildOf(transform) || hitTargets.Contains(target)) continue;
            Vector3 point = collider.ClosestPoint(origin);
            Vector3 direction = point - origin;
            if (Mathf.Abs(direction.y) > HitHalfHeight) continue;
            direction.y = 0f;
            if (direction.sqrMagnitude > AttackRange * AttackRange) continue;
            if (direction.sqrMagnitude > 0.0001f &&
                Vector3.Angle(StrikeForward, direction) > AttackAngle * 0.5f) continue;
            hitTargets.Add(target);
            DamageTarget(target, point);
        }
    }

    private bool CanUseGameplay() => Time.timeScale > 0f &&
        (ownHealth == null || ownHealth.Health > 0f) &&
        (panels == null || !panels.BlocksGameplay);

    private void DamageTarget(MiningCharacterHealth target, Vector3 point)
    {
        if (Damage <= 0 || target == null || target.Health <= 0f) return;
        float dealt = target.DealDamage(Damage);
        if (dealt <= 0f) return;
        if (Stats != null)
        {
            if (ownHealth != null && ownHealth.Health > 0f)
                ownHealth.Heal(dealt * Mathf.Clamp(Stats.lifeStealPercent, 0f, 100f) * 0.01f);
            target.ApplyBurn(Stats.burnDamagePerTick, Stats.burnTickSeconds, Stats.burnDurationSeconds);
        }
        var impactPrefab = Stats != null ? Stats.attackImpactVfxPrefab : null;
        if (impactPrefab == null) return;
        Vector3 towardPlayer = transform.position - point;
        towardPlayer.y = 0f;
        if (towardPlayer.sqrMagnitude < 0.0001f) towardPlayer = -StrikeForward;
        SpawnEffect(impactPrefab, point, Quaternion.LookRotation(towardPlayer, Vector3.up));
    }

    private GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var effect = Instantiate(prefab, position, rotation);
        PlayAndDestroyEffect(effect);
        return effect;
    }

    private void PlayAndDestroyEffect(GameObject effect)
    {
        // The imported Free Slash VFX prefab disables Play On Awake on its
        // particle systems; explicitly start it at the animation contact event.
        var particles = effect.GetComponent<ParticleSystem>();
        if (particles != null) particles.Play(true);
        float lifetime = Stats != null ? Stats.impactVfxLifetime : 2f;
        Destroy(effect, Mathf.Max(0.1f, lifetime));
    }

    public void OnSlashDownStart()
    {
        PlaySlashEffects(slashDownVfx, slashDownEffects);
    }

    public void OnSlashUpStart()
    {
        PlaySlashEffects(slashUpVfx, slashUpEffects);
    }

    private void PlaySlashEffects(ParticleSystem first, List<SlashVfxCue> additional)
    {
        if (!combatMode || !CanUseGameplay()) return;
        if (first != null) PlaySlashEffect(first);
        if (additional == null) return;
        foreach (var cue in additional)
        {
            if (cue.effect == null) continue;
            if (cue.delay <= 0f) PlaySlashEffect(cue.effect);
            else StartCoroutine(PlaySlashEffectAfterDelay(cue.effect, cue.delay / AttackSpeed));
        }
    }

    private System.Collections.IEnumerator PlaySlashEffectAfterDelay(ParticleSystem effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (combatMode && CanUseGameplay() && effect != null) PlaySlashEffect(effect);
    }

    private static void PlaySlashEffect(ParticleSystem effect)
    {
        // World-space particles keep the emitted slash in place even when the
        // scene emitter is parented to the moving hand or player.
        foreach (var system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }
        if (!effect.gameObject.activeSelf) effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
    }




    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        Gizmos.color = Application.isPlaying && wasAttacking && !hitApplied ? Color.red : Color.yellow;
        // Display the same height band used by the fist hit query.
        Gizmos.DrawLine(origin - Vector3.up * HitHalfHeight, origin + Vector3.up * HitHalfHeight);
        Vector3 previous = origin + Quaternion.AngleAxis(-AttackAngle * 0.5f, Vector3.up) * StrikeForward * AttackRange;
        Gizmos.DrawLine(origin, previous);
        Vector3 up = Vector3.up * HitHalfHeight;
        Gizmos.DrawLine(origin - up, previous - up);
        Gizmos.DrawLine(origin + up, previous + up);
        for (int i = 1; i <= 32; i++)
        {
            Vector3 point = origin + Quaternion.AngleAxis(-AttackAngle * 0.5f + AttackAngle * i / 32f,
                Vector3.up) * StrikeForward * AttackRange;
            Gizmos.DrawLine(previous, point);
            Gizmos.DrawLine(previous + up, point + up);
            Gizmos.DrawLine(previous - up, point - up);
            if (i % 8 == 0) Gizmos.DrawLine(point - up, point + up);
            previous = point;
        }
        Gizmos.DrawLine(origin, previous);
        Gizmos.DrawLine(origin - up, previous - up);
        Gizmos.DrawLine(origin + up, previous + up);
    }
}
