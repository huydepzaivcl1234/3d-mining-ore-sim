using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class PlayerCombatInput : MonoBehaviour
{
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
    [Tooltip("Normalized animation time of contact. 0.45 = 45% through the attack.")]
    [Range(0f, 1f), SerializeField] private float hitTime = 0.45f;
    [SerializeField] private Vector3 hitOriginOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private LayerMask targetLayers = ~0;
    private bool wasAttacking;
    private MiningAudioManager feedbackAudio;
    private LineRenderer attackArc;
    private Material attackArcMaterial;
    private float arcRemaining;
    private Color arcColor;
    private float arcDuration;
    private MiningPlayerStatsData Stats => MiningPlayerStats.For(this);
    public float Damage => Stats != null ? GetComponent<MiningPlayerStats>().Damage : Mathf.Max(0, damage);
    public float AttackRange => Mathf.Max(0.1f, Stats != null ? Stats.attackRange : attackRange);
    public float AttackAngle => Mathf.Clamp(Mathf.Min(30f, Stats != null ? Stats.attackAngle : attackAngle), 1, 180);
    public float AttackSpeed => Mathf.Max(0.1f, Stats != null ? Stats.attackSpeed : attackSpeed);
    private float BlendSeconds => Mathf.Max(0.01f, Stats != null ? Stats.combatBlendSeconds : combatBlendSeconds);
    private float HitTime => Mathf.Clamp01(Stats != null ? Stats.hitTime : hitTime);
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
        SetCombatMode(false);
        arcRemaining = 0f;
        if (attackArc != null) attackArc.enabled = false;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            int layer = animator.GetLayerIndex("combat layer");
            if (layer >= 0) animator.SetLayerWeight(layer, 0f);
        }
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
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int layer = animator.GetLayerIndex("combat layer");
        if (layer < 0) return;
        var current = animator.GetCurrentAnimatorStateInfo(layer);
        bool attacking = current.IsName("Attack");
        if (animator.IsInTransition(layer))
            attacking |= animator.GetNextAnimatorStateInfo(layer).IsName("Attack");

        // Leave locomotion's torso and arms untouched while travelling. The masked
        // attack overlays it only during a strike; legs keep their running cycle.
        bool idleCombat = animator.GetBool("CombatMode") && animator.GetFloat("Speed") < 0.1f;
        int hitLayer = animator.GetLayerIndex("Hit Reaction");
        bool reacting = hitLayer >= 0 &&
            (animator.GetCurrentAnimatorStateInfo(hitLayer).IsName("HitReaction") ||
             (animator.IsInTransition(hitLayer) && animator.GetNextAnimatorStateInfo(hitLayer).IsName("HitReaction")));
        // CombatIdle must not hide the lower-priority hit reaction. Attack is
        // still evaluated on its own layer and always has priority over Hit.
        float target = attacking || (idleCombat && !reacting) ? 1f : 0f;
        animator.SetLayerWeight(layer, Mathf.MoveTowards(animator.GetLayerWeight(layer),
            target, Time.deltaTime / BlendSeconds));
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
        int layer = animator.GetLayerIndex("combat layer");
        if (HasParameter("AttackSpeed", AnimatorControllerParameterType.Float)) animator.SetFloat("AttackSpeed", AttackSpeed);
        if (toggleCombat != null && toggleCombat.WasPressedThisFrame())
        {
            SetCombatMode(!combatMode);
        }
        if (layer >= 0) TrackAttack(layer);
        if (!combatMode || attack == null || !attack.WasPressedThisFrame()) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (HasParameter("Attack", AnimatorControllerParameterType.Trigger) &&
            (layer < 0 || (!animator.IsInTransition(layer) &&
            animator.GetCurrentAnimatorStateInfo(layer).IsName("CombatIdle"))))
            animator.SetTrigger("Attack");
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
        if (!enabled) ClearAim();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (HasParameter(drawWeaponParameter, AnimatorControllerParameterType.Bool))
            animator.SetBool(drawWeaponParameter, enabled);
        if (HasParameter("CombatMode", AnimatorControllerParameterType.Bool))
            animator.SetBool("CombatMode", enabled);
        if (HasParameter(drawWeaponParameter, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(drawWeaponParameter);
        if (HasParameter(sheathWeaponParameter, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(sheathWeaponParameter);
        if (changed)
        {
            string trigger = enabled ? drawWeaponParameter : sheathWeaponParameter;
            if (HasParameter(trigger, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(trigger);
        }
        if (HasParameter("Attack", AnimatorControllerParameterType.Trigger)) animator.ResetTrigger("Attack");
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
            if (next.IsName("Attack")) state = next;
        }
        bool active = combatMode && state.IsName("Attack");
        if (active && !wasAttacking)
        {
            hitApplied = false;
            if (feedbackAudio != null)
                feedbackAudio.PlaySfx(Stats != null ? Stats.attackSfx : null,
                    Stats != null ? Stats.attackSfxVolume : 1f);
        }
        if (active && !hitApplied && state.normalizedTime >= HitTime)
        {
            hitApplied = true;
            ShowAttackEffect();
            ApplyHit();
        }
        wasAttacking = active;
    }

    private void ShowAttackEffect()
    {
        var data = Stats;
        if (data == null) return;
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        GameObject prefab = data.attackVfxPrefab;
        if (prefab != null)
        {
            SpawnEffect(prefab, origin + StrikeForward * AttackRange * 0.6f,
                Quaternion.LookRotation(StrikeForward, Vector3.up));
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
        // Draw the strike's actual forward sector, not a full circle around Player.
        for (int i = 0; i < attackArc.positionCount; i++)
        {
            attackArc.SetPosition(i, origin + StrikeForward * Mathf.Lerp(AttackRange * 0.3f, AttackRange, i / 24f));
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
                Vector3.Angle(StrikeForward, direction) > AttackAngle * 0.5f) continue;
            if (direction.sqrMagnitude < closestDistance)
            {
                closest = target;
                closestDistance = direction.sqrMagnitude;
                closestPoint = point;
            }
        }
        if (closest != null) DamageTarget(closest, closestPoint);
    }

    private bool CanUseGameplay() => Time.timeScale > 0f &&
        (ownHealth == null || ownHealth.Health > 0f) &&
        (panels == null || !panels.BlocksGameplay);

    private void DamageTarget(MiningCharacterHealth target, Vector3 point)
    {
        if (Damage <= 0) return;
        target.ApplyDamage(Damage);
    }

    private GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var effect = Instantiate(prefab, position, rotation);
        float lifetime = Stats != null ? Stats.attackVfxLifetime : 2f;
        Destroy(effect, Mathf.Max(0.1f, lifetime));
        return effect;
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
