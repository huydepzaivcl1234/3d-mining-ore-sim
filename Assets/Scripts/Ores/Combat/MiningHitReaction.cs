using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Presentation only: never locks movement, attacks, or their damage timing.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(MiningCharacterHealth))]
    public sealed class MiningHitReaction : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private MiningCharacterHealth health;
        private bool configured;
        private int hitLayer = -1;
        private static readonly int Hit = Animator.StringToHash("Hit");

        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            configured = false;
            hitLayer = animator != null && animator.runtimeAnimatorController != null
                ? animator.GetLayerIndex("Hit Reaction") : -1;
            if (animator != null && animator.runtimeAnimatorController != null &&
                hitLayer >= 0)
                foreach (var parameter in animator.parameters)
                    if (parameter.nameHash == Hit && parameter.type == AnimatorControllerParameterType.Trigger)
                    {
                        configured = true;
                        break;
                    }
            if (configured) animator.SetLayerWeight(hitLayer, 0f);
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            if (configured && animator != null)
            {
                animator.ResetTrigger(Hit);
                animator.SetLayerWeight(hitLayer, 0f);
            }
        }

        private void LateUpdate()
        {
            if (!configured || animator == null || !animator.isActiveAndEnabled) return;
            bool reacting = animator.GetCurrentAnimatorStateInfo(hitLayer).IsName("HitReaction");
            if (animator.IsInTransition(hitLayer))
                reacting = animator.GetNextAnimatorStateInfo(hitLayer).IsName("HitReaction");
            // An empty state is not relied on to erase the previous state's
            // curves: release the entire overlay when no reaction is active.
            float target = health.Health > 0f && reacting ? 1f : 0f;
            animator.SetLayerWeight(hitLayer, health.Health <= 0f ? 0f :
                Mathf.MoveTowards(animator.GetLayerWeight(hitLayer), target, Time.deltaTime / 0.1f));
        }

        private void OnDamaged()
        {
            // Lethal damage belongs to Death, not the hit overlay.
            if (!configured || health.Health <= 0f || !animator.isActiveAndEnabled) return;
            animator.ResetTrigger(Hit);
            animator.SetTrigger(Hit);
        }
    }
}
