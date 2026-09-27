using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using StarterAssets;

[DisallowMultipleComponent]
public class PlayerCombatInput : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Input bindings - keyboard or mouse")]
    [SerializeField] private InputAction toggleCombat = new InputAction(
        "Toggle Combat", InputActionType.Button, "<Keyboard>/e");
    [SerializeField] private InputAction attack = new InputAction(
        "Attack", InputActionType.Button, "<Mouse>/leftButton");
    [SerializeField] private InputAction aim = new InputAction(
        "Lock Target", InputActionType.Button, "<Mouse>/rightButton");
    [Header("Target lock")]
    [SerializeField] private Camera aimCamera;
    [Min(1f), SerializeField] private float lockDistance = 30f;
    [Min(1f), SerializeField] private float aimTurnSpeed = 720f;
    [SerializeField] private LayerMask aimRayLayers = ~0;
    [Header("Weapon model (optional hand override)")]
    [SerializeField] private Transform weaponHand;
    private MiningCharacterHealth lockedTarget;
    private MiningCharacterHealth ownHealth;
    private ThirdPersonController movement;
    private MiningUiPanelCoordinator panels;
    private WeaponAttackData visualWeapon;
    private GameObject heldModel;
    public MiningCharacterHealth LockedTarget => IsLockValid(lockedTarget) ? lockedTarget : null;
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
    private WeaponAttackData equippedWeapon;
    private bool hasEquipmentOverride;
    private RuntimeAnimatorController originalController;
    public WeaponAttackData Weapon => hasEquipmentOverride ? equippedWeapon : Stats != null ? Stats.defaultWeapon : null;
    public bool HitsMultipleTargets => Weapon != null && Weapon.hitMode == WeaponHitMode.ForwardSweep;
    private MiningPlayerStatsData Stats => MiningPlayerStats.For(this);
    public float Damage => (Stats != null ? GetComponent<MiningPlayerStats>().Damage : Mathf.Max(0, damage)) * (Weapon != null ? Mathf.Max(0, Weapon.damageMultiplier) : 1f);
    public float AttackRange => Mathf.Max(0.1f, Weapon != null ? Weapon.range : Stats != null ? Stats.attackRange : attackRange);
    public float AttackAngle => Mathf.Clamp(Weapon != null ? Weapon.angle : Mathf.Min(30f, Stats != null ? Stats.attackAngle : attackAngle), 1, 180);
    public float AttackSpeed => Mathf.Max(0.1f, (Stats != null ? Stats.attackSpeed : attackSpeed) * (Weapon != null ? Weapon.animationSpeedMultiplier : 1f));
    private float BlendSeconds => Mathf.Max(0.01f, Stats != null ? Stats.combatBlendSeconds : combatBlendSeconds);
    private float HitTime => Mathf.Clamp01(Weapon != null ? Weapon.hitTime : Stats != null ? Stats.hitTime : hitTime);
    private Vector3 HitOriginOffset => Weapon != null ? Weapon.hitOriginOffset : Stats != null ? Stats.hitOriginOffset : hitOriginOffset;
    private float HitHalfHeight => Weapon != null ? Mathf.Max(0.1f, Weapon.hitHalfHeight) : 0.9f;
    private Vector3 StrikeForward => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    private bool hitApplied;
    private readonly HashSet<MiningCharacterHealth> hitTargets = new();
    private void OnEnable()
    {
        // Reserve right mouse for lock; migrate only the old built-in binding.
        if (attack != null && aim != null)
            for (int i = 0; i < attack.bindings.Count; i++)
                if (attack.bindings[i].effectivePath == "<Mouse>/rightButton" &&
                    aim.bindings.Count > 0 && aim.bindings[0].effectivePath == "<Mouse>/rightButton")
                    attack.ApplyBindingOverride(i, "<Mouse>/leftButton");
        toggleCombat?.Enable();
        attack?.Enable();
        aim?.Enable();
        if (heldModel != null) heldModel.SetActive(true);
    }
    private void OnDisable()
    {
        toggleCombat?.Disable();
        attack?.Disable();
        aim?.Disable();
        ClearTargetLock();
        if (heldModel != null) heldModel.SetActive(false);
        wasAttacking = false;
        hitApplied = false;
        combatMode = false;
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
        FaceLockedTarget(false);
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
        aim?.Dispose();
        if (heldModel != null) Destroy(heldModel);
        if (attackArcMaterial != null) Destroy(attackArcMaterial);
    }
    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        originalController = animator != null ? animator.runtimeAnimatorController : null;
        feedbackAudio = FindFirstObjectByType<MiningAudioManager>();
        ownHealth = GetComponent<MiningCharacterHealth>();
        movement = GetComponent<ThirdPersonController>();
        panels = FindFirstObjectByType<MiningUiPanelCoordinator>();
    }
    private void Start()
    {
        if (Weapon != null && Weapon.animationOverrides != null && animator != null)
            animator.runtimeAnimatorController = Weapon.animationOverrides;
        RefreshHeldModel();
    }

    // Future weapon inventory calls this; do not change equipment mid-animation.
    public bool TryEquipWeapon(WeaponAttackData weapon)
    {
        if (wasAttacking) return false;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            int layer = animator.GetLayerIndex("combat layer");
            if (layer >= 0 && (animator.IsInTransition(layer) || animator.GetCurrentAnimatorStateInfo(layer).IsName("Attack"))) return false;
        }
        equippedWeapon = weapon;
        hasEquipmentOverride = true;
        if (animator != null)
        {
            animator.runtimeAnimatorController = weapon != null && weapon.animationOverrides != null ? weapon.animationOverrides : originalController;
            if (animator.runtimeAnimatorController != null) animator.SetBool("CombatMode", combatMode);
        }
        RefreshHeldModel();
        return true;
    }
    private void Update()
    {
        if (visualWeapon != Weapon) RefreshHeldModel();
        if (!CanUseGameplay())
        {
            ClearTargetLock();
            return;
        }
        ValidateTargetLock();
        bool aimPressed = aim != null && aim.WasPressedThisFrame();
        if (aimPressed && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            AimFromPointer();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int layer = animator.GetLayerIndex("combat layer");
        if (layer < 0) return;
        animator.SetFloat("AttackSpeed", AttackSpeed);
        if (toggleCombat != null && toggleCombat.WasPressedThisFrame())
        {
            combatMode = !combatMode;
            animator.SetBool("CombatMode", combatMode);
            animator.ResetTrigger("Attack");
            if (!combatMode) ClearTargetLock();
        }
        TrackAttack(layer);
        // A legacy right-click Attack binding must not also attack on lock/cancel.
        if (aimPressed || !combatMode || attack == null || !attack.WasPressedThisFrame()) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (layer >= 0 && !animator.IsInTransition(layer) &&
            animator.GetCurrentAnimatorStateInfo(layer).IsName("CombatIdle"))
            animator.SetTrigger("Attack");
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
                feedbackAudio.PlaySfx(Weapon != null && Weapon.swingSfx != null ? Weapon.swingSfx : Stats != null ? Stats.attackSfx : null,
                    Weapon != null && Weapon.swingSfx != null ? Weapon.swingVolume : Stats != null ? Stats.attackSfxVolume : 1f);
        }
        if (active && !hitApplied && state.normalizedTime >= HitTime)
        {
            hitApplied = true;
            FaceLockedTarget(true);
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
        GameObject prefab = Weapon != null && Weapon.strikeVfxPrefab != null ? Weapon.strikeVfxPrefab : data.attackVfxPrefab;
        if (prefab != null)
        {
            bool aligned = prefab.GetComponent<WeaponStrikeVfx>() != null;
            var effect = SpawnEffect(prefab, aligned ? origin : origin + StrikeForward * AttackRange * 0.6f,
                Quaternion.LookRotation(StrikeForward, Vector3.up));
            if (effect.TryGetComponent<WeaponStrikeVfx>(out var visual))
                visual.Configure(AttackRange, AttackAngle, AttackSpeed, HitsMultipleTargets);
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
        arcColor = Weapon != null ? Weapon.trailColor : data.attackArcColor;
        arcDuration = Mathf.Max(0.03f, data.attackArcSeconds / AttackSpeed);
        arcRemaining = arcDuration;
        attackArc.widthMultiplier = Mathf.Max(0.01f, data.attackArcWidth);
        attackArc.startColor = attackArc.endColor = arcColor;
        // Draw the strike's actual forward sector, not a full circle around Player.
        for (int i = 0; i < attackArc.positionCount; i++)
        {
            float angle = Mathf.Lerp(-AttackAngle * 0.5f, AttackAngle * 0.5f, i / 24f);
            attackArc.SetPosition(i, HitsMultipleTargets
                ? origin + Quaternion.AngleAxis(angle, Vector3.up) * StrikeForward * AttackRange
                : origin + StrikeForward * Mathf.Lerp(AttackRange * 0.3f, AttackRange, i / 24f));
        }
        attackArc.enabled = true;
    }

    private void ApplyHit()
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        hitTargets.Clear();
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
            if (HitsMultipleTargets)
            {
                if (hitTargets.Add(target)) DamageTarget(target, point);
            }
            else if ((LockedTarget != null && target == LockedTarget) ||
                     ((LockedTarget == null || closest != LockedTarget) && direction.sqrMagnitude < closestDistance))
            {
                closest = target;
                closestDistance = direction.sqrMagnitude;
                closestPoint = point;
            }
        }
        if (!HitsMultipleTargets && closest != null) DamageTarget(closest, closestPoint);
    }

    private bool CanUseGameplay() => Time.timeScale > 0f &&
        (ownHealth == null || ownHealth.Health > 0f) &&
        (panels == null || !panels.BlocksGameplay);

    private bool IsLockValid(MiningCharacterHealth target) => target != null &&
        target != ownHealth && target.transform != transform && !target.transform.IsChildOf(transform) &&
        target.isActiveAndEnabled && target.Health > 0f &&
        target.GetComponentInParent<MushroomMonster>() != null &&
        (target.transform.position - transform.position).sqrMagnitude <= lockDistance * lockDistance;

    public bool TryLockTarget(MiningCharacterHealth target)
    {
        if (!isActiveAndEnabled || !CanUseGameplay() || !IsLockValid(target))
        {
            ClearTargetLock();
            return false;
        }
        lockedTarget = target;
        combatMode = true;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetBool("CombatMode", true);
        if (movement != null) movement.ExternalFacing = true;
        return true;
    }

    public void ClearTargetLock()
    {
        lockedTarget = null;
        if (movement != null) movement.ExternalFacing = false;
    }

    private void ValidateTargetLock()
    {
        if (lockedTarget != null && !IsLockValid(lockedTarget)) ClearTargetLock();
        // Unity's destroyed-object null also needs to release movement ownership.
        if (movement != null) movement.ExternalFacing = LockedTarget != null;
    }

    private void AimFromPointer()
    {
        if (Mouse.current == null) return;
        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null) { ClearTargetLock(); return; }
        Vector2 pointer = Cursor.lockState == CursorLockMode.Locked
            ? aimCamera.pixelRect.center : Mouse.current.position.ReadValue();
        Ray ray = aimCamera.ScreenPointToRay(pointer);
        TryAimRay(ray, aimCamera.farClipPlane);
    }

    public bool TryAimRay(Ray ray, float maxDistance)
    {
        Collider nearest = null;
        float distance = float.PositiveInfinity;
        // Click-only allocation. Ignore our own capsule/blade, never other walls.
        foreach (var hit in Physics.RaycastAll(ray, Mathf.Max(0f, maxDistance), aimRayLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.distance < distance) { nearest = hit.collider; distance = hit.distance; }
        }
        return TryLockTarget(nearest != null ? nearest.GetComponentInParent<MiningCharacterHealth>() : null);
    }

    private void FaceLockedTarget(bool contact)
    {
        if (!CanUseGameplay()) { ClearTargetLock(); return; }
        ValidateTargetLock();
        var target = LockedTarget;
        if (target == null) return;
        Vector3 direction = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = contact ? rotation : Quaternion.RotateTowards(transform.rotation,
            rotation, aimTurnSpeed * Time.deltaTime);
    }

    private void RefreshHeldModel()
    {
        if (heldModel != null) { heldModel.SetActive(false); Destroy(heldModel); }
        visualWeapon = Weapon;
        if (visualWeapon == null || visualWeapon.modelPrefab == null) return;
        Transform hand = weaponHand;
        if (hand == null && animator != null && animator.isHuman && animator.avatar != null)
            hand = animator.GetBoneTransform(visualWeapon.handBone);
        if (hand == null)
        {
            Debug.LogWarning("Weapon needs a Humanoid hand bone or Weapon Hand override.", this);
            return;
        }
        heldModel = Instantiate(visualWeapon.modelPrefab, hand, false);
        heldModel.name = "Equipped " + visualWeapon.name;
        heldModel.transform.localPosition = visualWeapon.modelLocalPosition;
        heldModel.transform.localRotation = Quaternion.Euler(visualWeapon.modelLocalEulerAngles);
        heldModel.transform.localScale = visualWeapon.modelLocalScale;
        // Hit queries remain authoritative; the decorative blade cannot push Player.
        foreach (var collider in heldModel.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in heldModel.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic = true; body.useGravity = false; }
        heldModel.SetActive(isActiveAndEnabled);
    }

    private void DamageTarget(MiningCharacterHealth target, Vector3 point)
    {
        if (Damage <= 0) return;
        target.ApplyDamage(Damage);
        if (Weapon != null && Weapon.impactVfxPrefab != null)
            SpawnEffect(Weapon.impactVfxPrefab, point, Quaternion.LookRotation(-transform.forward, transform.up));
    }

    private GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var effect = Instantiate(prefab, position, rotation);
        float lifetime = Weapon != null ? Weapon.effectLifetime : Stats != null ? Stats.attackVfxLifetime : 2f;
        Destroy(effect, Mathf.Max(0.1f, lifetime));
        return effect;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        Gizmos.color = Application.isPlaying && wasAttacking && !hitApplied ? Color.red : Color.yellow;
        // Display the same height band used by both weapon hit queries.
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
