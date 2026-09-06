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
        [SerializeField] private Vector3 focusPoint;
        [Min(0.1f), SerializeField] private float distance = 18f;
        [SerializeField] private float yaw = 45f;
        [Range(-89f, 89f), SerializeField] private float pitch = 38f;

        [Header("Controls")]
        [Min(0f), SerializeField] private float keyboardMoveSpeed = 10f;
        [Min(0f), SerializeField] private float rotationDegreesPerPixel = 0.2f;
        [Min(0f), SerializeField] private float keyboardRotationSpeed = 90f;
        [Min(0f), SerializeField] private float mousePanSpeed = 0.0025f;
        [Min(0f), SerializeField] private float zoomSpeed = 0.012f;
        [Min(0.1f), SerializeField] private float minimumDistance = 4f;
        [Min(0.1f), SerializeField] private float maximumDistance = 45f;

        private void Awake()
        {
            controlledCamera ??= Camera.main;
        }

        private void Update()
        {
            ReadKeyboard();
            ReadMouse();
        }

        private void LateUpdate()
        {
            if (controlledCamera == null)
            {
                controlledCamera = Camera.main;
            }

            if (controlledCamera == null)
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
            float speedMultiplier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? 2f : 1f;

            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 direction = yawRotation * new Vector3(horizontal, 0f, vertical);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }
            focusPoint += direction * (keyboardMoveSpeed * speedMultiplier * Time.deltaTime);

            float rotationInput = ReadAxis(keyboard.qKey, keyboard.eKey);
            yaw += rotationInput * keyboardRotationSpeed * Time.deltaTime;
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
                yaw += delta.x * rotationDegreesPerPixel;
                pitch = Mathf.Clamp(pitch - delta.y * rotationDegreesPerPixel, -85f, 85f);
            }

            if (mouse.middleButton.isPressed && controlledCamera != null)
            {
                float scale = distance * mousePanSpeed;
                focusPoint -= controlledCamera.transform.right * (delta.x * scale);
                Vector3 flatForward = Vector3.ProjectOnPlane(controlledCamera.transform.forward, Vector3.up).normalized;
                focusPoint -= flatForward * (delta.y * scale);
            }

            float scroll = mouse.scroll.ReadValue().y;
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minimumDistance, maximumDistance);
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private void OnValidate()
        {
            minimumDistance = Mathf.Max(0.1f, minimumDistance);
            maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
            distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            keyboardMoveSpeed = Mathf.Max(0f, keyboardMoveSpeed);
            rotationDegreesPerPixel = Mathf.Max(0f, rotationDegreesPerPixel);
            keyboardRotationSpeed = Mathf.Max(0f, keyboardRotationSpeed);
            mousePanSpeed = Mathf.Max(0f, mousePanSpeed);
            zoomSpeed = Mathf.Max(0f, zoomSpeed);
        }
    }
}
