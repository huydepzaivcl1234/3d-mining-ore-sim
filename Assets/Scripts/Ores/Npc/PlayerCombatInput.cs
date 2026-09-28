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
    private static readonly string[] AttackStates = { "Sword Attack 1", "Sword Attack 2", "Sword Attack 3" };
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
    [Min(0.1f), SerializeField] private float aimRange = 15f;
    [Min(0f), SerializeField] private float aimTurnSpeed = 720f;
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
    private bool wasAttacking;
    private bool queuedAttack;
    private int queuedAttackStateHash;
    private float queuedAttackUntil;
    private const float ComboBufferSeconds = 0.25f;
    private int lastAttackStateHash;
    private int lastAttackIndex = -1;
    private bool returningFromAttack;
    private MiningAudioManager feedbackAudio;
    private LineRenderer attackArc;
    private Material attackArcMaterial;
    private float arcRemaining;
    private Color arcColor;
    private float arcDuration;
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
        arcRemaining = 0f;
        if (attackArc != null) attackArc.enabled = false;
    }

    private void LateUpdate()
    {
        if (CanUseGameplay() && IsAimValid(aimedMonster))
        {
            Vector3 direction = aimedMonster.transform.position - transform.position;
            direction.y = 0f;
            if (movement != null) movement.ExternalFacing = true;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), aimTurnSpeed * Time.deltaTime);
        }
        else ClearAim();
        if (attackArc != null && attackArc.enabled)
        {
            arcRemaining -= Time.deltaTime;
            Color color = arcColor;
            color.a *= Mathf.Clamp01(arcRemaining / arcDuration);
            attackArc.startColor = attackArc.endColor = color;
            attackArc.enabled = arcRemaining > 0f;
        }
    }
    private void OnDestroy()
    {
        toggleCombat?.Dispose();
        attack?.Dispose();
        autoAim?.Dispose();
        if (attackArcMaterial != null) Destroy(attackArcMaterial);
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
        if (autoAim != null && autoAim.WasPressedThisFrame())
        {
            if (IsAimValid(aimedMonster)) ClearAim();
            else aimedMonster = FindNearestMonster();
            if (movement != null) movement.ExternalFacing = IsAimValid(aimedMonster);
        }
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int layer = animator.GetLayerIndex(CombatLayerName);
        if (HasParameter("AttackSpeed", AnimatorControllerParameterType.Float)) animator.SetFloat("AttackSpeed", AttackSpeed);
        if (toggleCombat != null && toggleCombat.WasPressedThisFrame())
        {
            SetCombatMode(!combatMode);
        }
        if (layer >= 0) TrackAttack(layer);
        if (layer < 0) return;
        bool pressed = attack != null && attack.WasPressedThisFrame() &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        var state = animator.GetCurrentAnimatorStateInfo(layer);
        bool swinging = IsAttackState(state);
        // Only a fresh click near the end of this swing may queue one follow-up.
        // A click during a transition or long before the combo window must not
        // become an extra attack after the player has released the button.
        if (combatMode && pressed && swinging && !animator.IsInTransition(layer) &&
            state.normalizedTime >= 0.55f && state.normalizedTime < 0.9f)
        {
            queuedAttack = true;
            queuedAttackStateHash = state.shortNameHash;
            queuedAttackUntil = Time.time + ComboBufferSeconds;
        }
        if (swinging && !animator.IsInTransition(layer))
        {
            if (queuedAttack && (queuedAttackStateHash != state.shortNameHash ||
                Time.time > queuedAttackUntil)) queuedAttack = false;
            if (combatMode && queuedAttack && state.normalizedTime >= 0.7f)
            {
                queuedAttack = false;
                PlayRandomAttack(layer);
            }
            else if (state.shortNameHash == FirstAttackState && !queuedAttack &&
                     !returningFromAttack && state.normalizedTime >= 0.85f)
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
        PlayRandomAttack(layer);
    }

    private void PlayRandomAttack(int layer)
    {
        queuedAttack = false;
        queuedAttackStateHash = 0;
        int choice = Random.Range(0, AttackStates.Length);
        if (choice == lastAttackIndex)
            choice = (choice + Random.Range(1, AttackStates.Length)) % AttackStates.Length;
        lastAttackIndex = choice;
        hitApplied = false;
        returningFromAttack = false;
        animator.ResetTrigger("attack");
        animator.CrossFadeInFixedTime(AttackStates[choice], 0.08f, layer, 0f);
    }

    private bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        foreach (var parameter in animator.parameters)
            if (parameter.name == name && parameter.type == type) return true;
        return false;
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

    private bool IsAimValid(MushroomMonster monster) => monster != null &&
        monster.isActiveAndEnabled && monster.Health != null && monster.Health.Health > 0f &&
        (monster.transform.position - transform.position).sqrMagnitude <= aimRange * aimRange;

    private MushroomMonster FindNearestMonster()
    {
        MushroomMonster nearest = null;
        float distance = float.PositiveInfinity;
        foreach (var collider in Physics.OverlapSphere(transform.position, aimRange, targetLayers,
                     QueryTriggerInteraction.Ignore))
        {
            var monster = collider.GetComponentInParent<MushroomMonster>();
            if (!IsAimValid(monster)) continue;
            float candidate = (monster.transform.position - transform.position).sqrMagnitude;
            if (candidate < distance) { distance = candidate; nearest = monster; }
        }
        return nearest;
    }

    private void ClearAim()
    {
        aimedMonster = null;
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
        ShowAttackEffect(sweep);
        if (sweep) ApplySweepHit();
        else ApplyHit();
    }

    private void ShowAttackEffect(bool sweep)
    {
        var data = Stats;
        if (data == null) return;
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        GameObject prefab = data.attackVfxPrefab;
        if (prefab != null)
        {
            // The pack's slash is authored relative to the character root, not
            // as a forward-facing effect at the end of the damage range.
            var slash = Instantiate(prefab, transform);
            slash.transform.localPosition = data.slashVfxLocalPosition;
            slash.transform.localRotation = Quaternion.Euler(data.slashVfxLocalEuler);
            // The vendor slash prefab embeds an Impact child. A miss must not
            // play that particle; real impacts are spawned by DamageTarget.
            var embeddedImpact = slash.transform.Find("Impact");
            if (embeddedImpact != null) embeddedImpact.gameObject.SetActive(false);
            PlayAndDestroyEffect(slash);
            return;
        }
        if (!data.showAttackArc) return;
        if (attackArc == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) { Debug.LogWarning("Attack arc shader unavailable. Assign Attack Vfx Prefab in PlayerStatsData.", this); return; }
            var visual = new GameObject("Attack Arc (visual only)");
            visual.transform.SetParent(transform, false);
            attackArc = visual.AddComponent<LineRenderer>();
            attackArcMaterial = new Material(shader);
            attackArc.sharedMaterial = attackArcMaterial;
            attackArc.useWorldSpace = true;
            attackArc.positionCount = 25;
            attackArc.numCapVertices = 3;
            attackArc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            attackArc.receiveShadows = false;
        }
        arcColor = data.attackArcColor;
        arcDuration = Mathf.Max(0.03f, data.attackArcSeconds / AttackSpeed);
        arcRemaining = arcDuration;
        attackArc.widthMultiplier = Mathf.Max(0.01f, data.attackArcWidth);
        attackArc.startColor = attackArc.endColor = arcColor;
        // The upswing shows the same sector that can damage multiple targets.
        for (int i = 0; i < attackArc.positionCount; i++)
        {
            float progress = i / (float)(attackArc.positionCount - 1);
            Vector3 direction = sweep
                ? Quaternion.AngleAxis(Mathf.Lerp(-AttackAngle * 0.5f, AttackAngle * 0.5f, progress), Vector3.up) * StrikeForward
                : StrikeForward;
            attackArc.SetPosition(i, origin + direction * (sweep ? AttackRange : Mathf.Lerp(AttackRange * 0.3f, AttackRange, progress)));
        }
        attackArc.enabled = true;
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
        target.ApplyDamage(Damage);
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
        float lifetime = Stats != null ? Stats.attackVfxLifetime : 2f;
        Destroy(effect, Mathf.Max(0.1f, lifetime));
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
