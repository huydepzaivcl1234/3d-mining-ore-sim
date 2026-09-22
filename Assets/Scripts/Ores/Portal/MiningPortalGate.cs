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

        [Header("Camera Walk-In Trigger")]
        [Tooltip("Treats the orbit camera focus like the player position in this camera-driven game.")]
        [SerializeField] private bool enterWhenCameraFocusCrossesPortal = true;
        [Min(0.25f), SerializeField] private float cameraEntryRadius = 1.75f;
        [Min(0.25f), SerializeField] private float cameraRearmRadius = 3f;

        private MiningWorldAreaController areaController;
        private MiningOrbitCamera orbitCamera;
        private bool cameraEntryArmed = true;

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

        private void Update()
        {
            if (!enterWhenCameraFocusCrossesPortal)
            {
                return;
            }

            areaController ??= MiningWorldAreaController.EnsureRuntime();
            orbitCamera ??= FindFirstObjectByType<MiningOrbitCamera>(FindObjectsInactive.Include);
            if (orbitCamera == null || orbitCamera.CinematicOverrideActive ||
                areaController.IsTransitioning ||
                areaController.CurrentArea != MiningWorldArea.Ground)
            {
                return;
            }

            Vector3 offset = orbitCamera.FocusPoint - transform.position;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared > cameraRearmRadius * cameraRearmRadius)
            {
                cameraEntryArmed = true;
                return;
            }

            if (!cameraEntryArmed || distanceSquared > cameraEntryRadius * cameraEntryRadius)
            {
                return;
            }

            cameraEntryArmed = false;
            if (areaController.UndergroundUnlocked)
            {
                areaController.TryUnlockAndEnter();
            }
            else
            {
                Interact();
            }
        }

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

        private void OnValidate()
        {
            cameraEntryRadius = Mathf.Max(0.25f, cameraEntryRadius);
            cameraRearmRadius = Mathf.Max(cameraEntryRadius + 0.25f, cameraRearmRadius);
        }
    }
}

