using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Shared game settings that do not belong to one ore, NPC, or spawn system.</summary>
    [CreateAssetMenu(fileName = "MiningGameData", menuName = "Mining Simulator/Game Data/Shared")]
    public sealed class MiningGameData : ScriptableObject
    {
        [Header("Click Mining")]
        [SerializeField] private LayerMask clickableLayers = ~0;
        [Min(0.1f), SerializeField] private float clickMaximumDistance = 500f;

        [Header("Money Count Animation")]
        [Min(1f), SerializeField] private float moneyCountUnitsPerSecond = 25f;
        [Min(0.05f), SerializeField] private float moneyCountMaximumDuration = 1.25f;

        [Header("Gem Currency")]
        [Tooltip("Gem balance used for a player who has no saved Gem data yet.")]
        [Min(0f), SerializeField] private float startingGems;
        [Tooltip("PlayerPrefs key used only by the Gem wallet.")]
        [SerializeField] private string gemSaveKey = "MiningSimulator.Gems.v1";

        [Header("Camera")]
        [SerializeField] private Vector3 cameraFocusPoint;
        [Min(0.1f), SerializeField] private float cameraDistance = 18f;
        [SerializeField] private float cameraYaw = 45f;
        [SerializeField] private float cameraPitch = 38f;
        [Range(-89f, 89f), SerializeField] private float cameraMinimumPitch = -85f;
        [Range(-89f, 89f), SerializeField] private float cameraMaximumPitch = 85f;
        [Min(0f), SerializeField] private float cameraMoveSpeed = 10f;
        [Min(1f), SerializeField] private float cameraFastMoveMultiplier = 2f;
        [Min(0f), SerializeField] private float cameraRotationDegreesPerPixel = 0.2f;
        [Min(0f), SerializeField] private float cameraKeyboardRotationSpeed = 90f;
        [Min(0f), SerializeField] private float cameraMousePanSpeed = 0.0025f;
        [Min(0f), SerializeField] private float cameraZoomSpeed = 0.012f;
        [Min(0.1f), SerializeField] private float cameraMinimumDistance = 4f;
        [Min(0.1f), SerializeField] private float cameraMaximumDistance = 45f;

        public LayerMask ClickableLayers => clickableLayers;
        public float ClickMaximumDistance => clickMaximumDistance;
        public float MoneyCountUnitsPerSecond => moneyCountUnitsPerSecond;
        public float MoneyCountMaximumDuration => moneyCountMaximumDuration;
        public float StartingGems => startingGems;
        public string GemSaveKey => string.IsNullOrWhiteSpace(gemSaveKey)
            ? "MiningSimulator.Gems.v1"
            : gemSaveKey;
        public Vector3 CameraFocusPoint => cameraFocusPoint;
        public float CameraDistance => cameraDistance;
        public float CameraYaw => cameraYaw;
        public float CameraPitch => cameraPitch;
        public float CameraMinimumPitch => cameraMinimumPitch;
        public float CameraMaximumPitch => cameraMaximumPitch;
        public float CameraMoveSpeed => cameraMoveSpeed;
        public float CameraFastMoveMultiplier => cameraFastMoveMultiplier;
        public float CameraRotationDegreesPerPixel => cameraRotationDegreesPerPixel;
        public float CameraKeyboardRotationSpeed => cameraKeyboardRotationSpeed;
        public float CameraMousePanSpeed => cameraMousePanSpeed;
        public float CameraZoomSpeed => cameraZoomSpeed;
        public float CameraMinimumDistance => cameraMinimumDistance;
        public float CameraMaximumDistance => cameraMaximumDistance;

        private void OnValidate()
        {
            clickMaximumDistance = Mathf.Max(0.1f, clickMaximumDistance);
            moneyCountUnitsPerSecond = Mathf.Max(1f, moneyCountUnitsPerSecond);
            moneyCountMaximumDuration = Mathf.Max(0.05f, moneyCountMaximumDuration);
            startingGems = Mathf.Max(0f, startingGems);
            if (string.IsNullOrWhiteSpace(gemSaveKey))
            {
                gemSaveKey = "MiningSimulator.Gems.v1";
            }
            cameraMinimumDistance = Mathf.Max(0.1f, cameraMinimumDistance);
            cameraMaximumDistance = Mathf.Max(cameraMinimumDistance, cameraMaximumDistance);
            cameraDistance = Mathf.Clamp(cameraDistance, cameraMinimumDistance, cameraMaximumDistance);
            cameraMinimumPitch = Mathf.Clamp(cameraMinimumPitch, -89f, 89f);
            cameraMaximumPitch = Mathf.Clamp(cameraMaximumPitch, cameraMinimumPitch, 89f);
            cameraPitch = Mathf.Clamp(cameraPitch, cameraMinimumPitch, cameraMaximumPitch);
            cameraMoveSpeed = Mathf.Max(0f, cameraMoveSpeed);
            cameraFastMoveMultiplier = Mathf.Max(1f, cameraFastMoveMultiplier);
            cameraRotationDegreesPerPixel = Mathf.Max(0f, cameraRotationDegreesPerPixel);
            cameraKeyboardRotationSpeed = Mathf.Max(0f, cameraKeyboardRotationSpeed);
            cameraMousePanSpeed = Mathf.Max(0f, cameraMousePanSpeed);
            cameraZoomSpeed = Mathf.Max(0f, cameraZoomSpeed);
        }
    }
}
