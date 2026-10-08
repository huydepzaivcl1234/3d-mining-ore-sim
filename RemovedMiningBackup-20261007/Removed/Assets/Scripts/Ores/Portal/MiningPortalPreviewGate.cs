using UnityEngine;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Opens a scene-authored portal preview panel via the existing raycast interaction system.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningPortalPreviewGate : MonoBehaviour, IMiningInteractable
    {
        [Header("Scene references")]
        [SerializeField] private MiningPortalPreviewPanel previewPanel;
        [SerializeField] private MiningDynamicRadialMaskTransition radialTransition;

        [Header("Interaction")]
        [Tooltip("Enter opens the panel while the shared interaction raycast is hovering this gate. The shared F key also works.")]
        [SerializeField] private bool allowEnterWhileFocused = true;
        [SerializeField] private string englishInteractionLabel = "View portal travel";
        [SerializeField] private string vietnameseInteractionLabel = "Xem cổng dịch chuyển";

        private bool isFocused;

        public string InteractionLabel => MiningLocalization.Text(
            englishInteractionLabel, vietnameseInteractionLabel);

        public bool CanInteract => isActiveAndEnabled &&
            previewPanel != null && radialTransition != null &&
            !radialTransition.IsPlaying && !previewPanel.IsOpen;

        private void Update()
        {
            if (!allowEnterWhileFocused || !isFocused || !CanInteract) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame ||
                                     keyboard.numpadEnterKey.wasPressedThisFrame))
                Interact();
        }

        public void Interact()
        {
            if (CanInteract) previewPanel.Show(radialTransition, GetComponent<MiningLavaWorldController>());
        }

        public void SetInteractionFocused(bool focused)
        {
            isFocused = focused;
        }

        private void OnDisable()
        {
            isFocused = false;
        }

        /// <summary>Called by the editor setup tool. Never rewrites assigned scene references.</summary>
        public void ConfigureIfMissing(MiningPortalPreviewPanel panel,
            MiningDynamicRadialMaskTransition transition)
        {
            if (previewPanel == null) previewPanel = panel;
            if (radialTransition == null) radialTransition = transition;
        }
    }
}
