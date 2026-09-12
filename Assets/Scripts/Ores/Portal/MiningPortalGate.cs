using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Marks a world object (the visual Portal gate) as clickable through the existing
    /// OreClickInput raycast. Interacting with it opens the "under maintenance" notice -
    /// this gate has no destination wired up yet, unlike a finished feature.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningPortalGate : MonoBehaviour
    {
        [Tooltip("Left empty on purpose: resolved automatically the first time the gate is " +
                 "clicked, so this object never needs manual wiring in the Inspector.")]
        [SerializeField] private MiningPortalMaintenancePanel maintenancePanel;

        public void Interact()
        {
            if (maintenancePanel == null)
            {
                maintenancePanel = FindFirstObjectByType<MiningPortalMaintenancePanel>(
                    FindObjectsInactive.Include);
            }

            maintenancePanel?.Show();
        }
    }
}
