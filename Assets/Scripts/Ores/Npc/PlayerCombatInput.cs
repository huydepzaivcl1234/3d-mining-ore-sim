using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PlayerCombatInput : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Input bindings - keyboard or mouse")]
    [SerializeField] private InputAction toggleCombat = new InputAction(
        "Toggle Combat", InputActionType.Button, "<Keyboard>/e");
    [SerializeField] private InputAction attack = new InputAction(
        "Attack", InputActionType.Button, "<Mouse>/rightButton");
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
    private bool hitApplied;
    private readonly HashSet<MiningCharacterHealth> hitTargets = new();
    private void OnEnable()
    {
        toggleCombat?.Enable();
        attack?.Enable();
    }
    private void OnDisable()
    {
        toggleCombat?.Disable();
        attack?.Disable();
        wasAttacking = false;
        hitApplied = false;
        combatMode = false;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            int layer = animator.GetLayerIndex("combat layer");
            if (layer >= 0) animator.SetLayerWeight(layer, 0f);
        }
    }

    private void LateUpdate()
    {
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
        float target = attacking || idleCombat ? 1f : 0f;
        animator.SetLayerWeight(layer, Mathf.MoveTowards(animator.GetLayerWeight(layer),
            target, Time.deltaTime / Mathf.Max(0.01f, combatBlendSeconds)));
    }
    private void OnDestroy()
    {
        toggleCombat?.Dispose();
        attack?.Dispose();
    }
    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }
    private void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        int layer = animator.GetLayerIndex("combat layer");
        if (layer < 0) return;
        animator.SetFloat("AttackSpeed", Mathf.Max(0.1f, attackSpeed));
        if (toggleCombat != null && toggleCombat.WasPressedThisFrame())
        {
            combatMode = !combatMode;
            animator.SetBool("CombatMode", combatMode);
            animator.ResetTrigger("Attack");
        }
        TrackAttack(layer);
        if (!combatMode || attack == null || !attack.WasPressedThisFrame()) return;
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
        if (active && !wasAttacking) hitApplied = false;
        if (active && !hitApplied && state.normalizedTime >= hitTime)
        {
            hitApplied = true;
            ApplyHit();
        }
        wasAttacking = active;
    }

    private void ApplyHit()
    {
        Vector3 origin = transform.TransformPoint(hitOriginOffset);
        hitTargets.Clear();
        foreach (var collider in Physics.OverlapSphere(origin, attackRange, targetLayers,
                     QueryTriggerInteraction.Ignore))
        {
            var target = collider.GetComponentInParent<MiningCharacterHealth>();
            if (target == null || target.transform.root == transform.root || hitTargets.Contains(target)) continue;
            Vector3 direction = collider.ClosestPoint(origin) - origin;
            if (direction.sqrMagnitude > 0.0001f &&
                Vector3.Angle(transform.forward, direction) > attackAngle * 0.5f) continue;
            hitTargets.Add(target);
            target.ApplyDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(hitOriginOffset);
        Gizmos.color = Application.isPlaying && wasAttacking && !hitApplied ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(origin, attackRange);
        Vector3 previous = origin + Quaternion.AngleAxis(-attackAngle * 0.5f, transform.up) * transform.forward * attackRange;
        Gizmos.DrawLine(origin, previous);
        for (int i = 1; i <= 32; i++)
        {
            Vector3 point = origin + Quaternion.AngleAxis(-attackAngle * 0.5f + attackAngle * i / 32f,
                transform.up) * transform.forward * attackRange;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
        Gizmos.DrawLine(origin, previous);
    }
}
