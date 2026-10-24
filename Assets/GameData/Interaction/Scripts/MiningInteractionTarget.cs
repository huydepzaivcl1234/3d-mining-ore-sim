using UnityEngine;
using UnityEngine.Events;

namespace MiningSimulator.Ores
{
    /// <summary>Attach to a collider-backed NPC or prop to use the shared hover/F interaction.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInteractionTarget : MonoBehaviour, IMiningInteractable
    {
        [Header("Label")]
        [SerializeField] private string englishLabel = "NPC";
        [SerializeField] private string vietnameseLabel = "NPC";
        [SerializeField] private bool interactionEnabled = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onInteract;
        [SerializeField] private UnityEvent onFocusEntered;
        [SerializeField] private UnityEvent onFocusExited;

        public string InteractionLabel => MiningLocalization.Text(englishLabel, vietnameseLabel);
        public bool CanInteract => interactionEnabled && isActiveAndEnabled;
        public void Interact()
        {
            if (CanInteract)
            {
                onInteract?.Invoke();
            }
        }

        public void SetInteractionFocused(bool focused)
        {
            if (focused)
            {
                onFocusEntered?.Invoke();
            }
            else
            {
                onFocusExited?.Invoke();
            }
        }
    }
}
