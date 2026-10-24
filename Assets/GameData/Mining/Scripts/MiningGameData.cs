using UnityEngine;
using UnityEngine.Serialization;

namespace MiningSimulator.Ores
{
    /// <summary>Shared game settings that do not belong to one ore, NPC, or spawn system.</summary>
    [CreateAssetMenu(fileName = "MiningGameData", menuName = "Mining Simulator/Game Data/Shared")]
    public sealed class MiningGameData : ScriptableObject
    {
        public const string DefaultGemSaveKey = "MiningSimulator.Gems.v1";

        [Header("Loot interaction")]
        [SerializeField] private LayerMask clickableLayers = ~0;
        [Min(0.1f), SerializeField] private float clickMaximumDistance = 500f;

        [Header("Money And Gem Count Animation")]
        [FormerlySerializedAs("moneyCountUnitsPerSecond")]
        [Min(1f), SerializeField] private float currencyCountUnitsPerSecond = 25f;
        [FormerlySerializedAs("moneyCountMaximumDuration")]
        [Min(0.05f), SerializeField] private float currencyCountMaximumDuration = 1.25f;

        [Header("Gem")]
        [Tooltip("Gem balance used for a player who has no saved Gem data yet.")]
        [Min(0f), SerializeField] private float startingGems;
        [Tooltip("PlayerPrefs key used only by the Gem wallet.")]
        [SerializeField] private string gemSaveKey = DefaultGemSaveKey;

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

        [Header("Camera rotation comfort")]
        [Min(.01f), SerializeField] private float mouseSensitivityDefault = 1f;
        [Min(.01f), SerializeField] private float mouseSensitivityMinimum = .25f;
        [Min(.01f), SerializeField] private float mouseSensitivityMaximum = 4f;
        [Min(1f), SerializeField] private float shiftLockMaximumRotationSpeed = 720f;
        [Min(.001f), SerializeField] private float shiftLockRotationSmoothSeconds = .025f;
        [Min(1f), SerializeField] private float cameraMaximumRotationSpeed = 180f;
        [Min(0.01f), SerializeField] private float cameraRotationSmoothSeconds = 0.06f;
        [Tooltip("Optional subtle blur while rotating. Set strength to zero to disable.")]
        [Range(0f, 0.2f), SerializeField] private float cameraRotationBlurStrength = 0.06f;
        [Range(0f, 0.05f), SerializeField] private float cameraRotationBlurClamp = 0.012f;

        public LayerMask ClickableLayers => clickableLayers;
        public float ClickMaximumDistance => clickMaximumDistance;
        public float CurrencyCountUnitsPerSecond => currencyCountUnitsPerSecond;
        public float CurrencyCountMaximumDuration => currencyCountMaximumDuration;
        public float StartingGems => startingGems;
        public string GemSaveKey => string.IsNullOrWhiteSpace(gemSaveKey)
            ? DefaultGemSaveKey
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
        public float CameraMaximumRotationSpeed => Mathf.Max(1f, cameraMaximumRotationSpeed);
        public float MouseSensitivityMinimum => Mathf.Max(.01f, mouseSensitivityMinimum);
        public float MouseSensitivityMaximum => Mathf.Max(MouseSensitivityMinimum, mouseSensitivityMaximum);
        public float MouseSensitivityDefault => Mathf.Clamp(mouseSensitivityDefault, MouseSensitivityMinimum, MouseSensitivityMaximum);
        public float ShiftLockMaximumRotationSpeed => Mathf.Max(1f, shiftLockMaximumRotationSpeed);
        public float ShiftLockRotationSmoothSeconds => Mathf.Max(.001f, shiftLockRotationSmoothSeconds);
        public float CameraRotationSmoothSeconds => Mathf.Max(0.01f, cameraRotationSmoothSeconds);
        public float CameraRotationBlurStrength => Mathf.Clamp(cameraRotationBlurStrength, 0f, 0.2f);
        public float CameraRotationBlurClamp => Mathf.Clamp(cameraRotationBlurClamp, 0f, 0.05f);

        private void OnValidate()
        {
            clickMaximumDistance = Mathf.Max(0.1f, clickMaximumDistance);
            currencyCountUnitsPerSecond = Mathf.Max(1f, currencyCountUnitsPerSecond);
            currencyCountMaximumDuration = Mathf.Max(0.05f, currencyCountMaximumDuration);
            startingGems = Mathf.Max(0f, startingGems);
            if (string.IsNullOrWhiteSpace(gemSaveKey))
            {
                gemSaveKey = DefaultGemSaveKey;
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
