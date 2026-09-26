using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatInput : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private bool combatMode;

    private void Update()
    {
        if (animator == null) return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            combatMode = !combatMode;
            animator.SetBool("CombatMode", combatMode);
            animator.ResetTrigger("Attack");
        }

        if (combatMode && Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            int layer = animator.GetLayerIndex("combat layer");
            if (layer < 0 || animator.IsInTransition(layer)) return;

            if (animator.GetCurrentAnimatorStateInfo(layer)
                .IsName("CombatIdle"))
                animator.SetTrigger("Attack");
        }
    }
}