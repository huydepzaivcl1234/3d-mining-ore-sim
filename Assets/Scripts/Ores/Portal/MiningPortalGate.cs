using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Shared hover/F interaction target for purchasing and travelling to Underground.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningPortalGate : MonoBehaviour, IMiningInteractable
    {
        [Tooltip("Left empty on purpose: resolved automatically the first time the player " +
                 "presses the interaction key, so this object needs no manual wiring.")]
        [SerializeField] private MiningPortalMaintenancePanel maintenancePanel;

        private MiningWorldAreaController areaController;

        public string InteractionLabel
        {
            get
            {
                areaController ??= MiningWorldAreaController.EnsureRuntime();
                if (areaController.CurrentArea == MiningWorldArea.Underground)
                    return MiningLocalization.Text("Return to Ground", "Về mặt đất");
                return areaController.UndergroundUnlocked
                    ? MiningLocalization.Text("Enter Underground", "Vào lòng đất")
                    : MiningLocalization.Text("Unlock Underground", "Mở khóa lòng đất");
            }
        }
        public bool CanInteract => isActiveAndEnabled;
        public void Interact()
        {
            areaController ??= MiningWorldAreaController.EnsureRuntime();
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

            maintenancePanel.Show(areaController);
        }

        public void SetInteractionFocused(bool focused)
        {
            // The shared prompt is the portal's hover feedback. Kept intentionally empty so
            // the imported portal materials and particle values are never modified at runtime.
        }
    }
}
