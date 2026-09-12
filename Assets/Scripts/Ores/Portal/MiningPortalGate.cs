using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Marks the Portal as a shared hover/F interaction target. Interacting opens the
    /// "under maintenance" notice -
    /// this gate has no destination wired up yet, unlike a finished feature.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningPortalGate : MonoBehaviour, IMiningInteractable
    {
        [Tooltip("Left empty on purpose: resolved automatically the first time the player " +
                 "presses the interaction key, so this object needs no manual wiring.")]
        [SerializeField] private MiningPortalMaintenancePanel maintenancePanel;

        public string InteractionLabel => MiningLocalization.Text("Portal", "Cổng dịch chuyển");
        public bool CanInteract => isActiveAndEnabled;
        public void Interact()
        {
            if (maintenancePanel == null)
            {
                maintenancePanel = FindFirstObjectByType<MiningPortalMaintenancePanel>(
                    FindObjectsInactive.Include);
            }

            if (maintenancePanel == null)
            {
                Debug.LogWarning("Portal maintenance panel is missing. Run Mining Simulator/" +
                                 "Setup/Create Portal Gate in Edit Mode.", this);
                return;
            }

            maintenancePanel.Show();
        }

        public void SetInteractionFocused(bool focused)
        {
            // The shared prompt is the portal's hover feedback. Kept intentionally empty so
            // the imported portal materials and particle values are never modified at runtime.
        }
    }
}
