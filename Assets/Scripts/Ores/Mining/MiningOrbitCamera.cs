using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MiningSimulator.Ores
{
    /// <summary>Provides 360-degree orbit, keyboard pan, mouse pan, and zoom.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private MiningGameData gameData;

        private Vector3 focusPoint;
        private float distance;
        private float yaw;
        private float pitch;

        private void Awake()
        {
            controlledCamera ??= Camera.main;
            if (gameData != null)
            {
                focusPoint = gameData.CameraFocusPoint;
                distance = gameData.CameraDistance;
                yaw = gameData.CameraYaw;
                pitch = gameData.CameraPitch;
            }
        }

        private void Update()
        {
            if (gameData == null)
            {
                return;
            }

            ReadKeyboard();
            ReadMouse();
        }

        private void LateUpdate()
        {
            if (controlledCamera == null)
            {
                controlledCamera = Camera.main;
            }

            if (controlledCamera == null || gameData == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = focusPoint - rotation * Vector3.forward * distance;
            controlledCamera.transform.SetPositionAndRotation(position, rotation);
        }

        private void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            float horizontal = ReadAxis(keyboard.aKey, keyboard.dKey);
            float vertical = ReadAxis(keyboard.sKey, keyboard.wKey);
            float speedMultiplier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? gameData.CameraFastMoveMultiplier
                : 1f;

            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 direction = yawRotation * new Vector3(horizontal, 0f, vertical);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }
            focusPoint += direction * (gameData.CameraMoveSpeed * speedMultiplier * Time.deltaTime);

            float rotationInput = ReadAxis(keyboard.qKey, keyboard.eKey);
            yaw += rotationInput * gameData.CameraKeyboardRotationSpeed * Time.deltaTime;
        }

        private void ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            if (mouse.rightButton.isPressed)
            {
                yaw += delta.x * gameData.CameraRotationDegreesPerPixel;
                pitch = Mathf.Clamp(pitch - delta.y * gameData.CameraRotationDegreesPerPixel,
                    gameData.CameraMinimumPitch, gameData.CameraMaximumPitch);
            }

            if (mouse.middleButton.isPressed && controlledCamera != null)
            {
                float scale = distance * gameData.CameraMousePanSpeed;
                focusPoint -= controlledCamera.transform.right * (delta.x * scale);
                Vector3 flatForward = Vector3.ProjectOnPlane(controlledCamera.transform.forward, Vector3.up).normalized;
                focusPoint -= flatForward * (delta.y * scale);
            }

            float scroll = mouse.scroll.ReadValue().y;
            distance = Mathf.Clamp(distance - scroll * gameData.CameraZoomSpeed,
                gameData.CameraMinimumDistance, gameData.CameraMaximumDistance);
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }
    }
}
