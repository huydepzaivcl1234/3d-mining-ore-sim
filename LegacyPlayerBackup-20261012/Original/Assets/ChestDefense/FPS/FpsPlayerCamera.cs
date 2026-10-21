using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;
using MiningSimulator.Ores;

namespace ChestDefense.Fps
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class FpsPlayerCamera : MonoBehaviour
    {
        public Camera targetCamera;
        public MiningOrbitCamera orbit;
        public MiningUiPanelCoordinator panels;
        public FpsWeaponController weapons;
        public Renderer[] bodyRenderers;
        [Tooltip("Other camera drivers, e.g. CinemachineBrain, suspended while FPS owns this camera.")]
        public Behaviour[] otherCameraDrivers;
        public Vector3 eyeOffset = new Vector3(0, 1.7f, 0);
        public float standingFieldOfView = 65f, fovBlendSeconds = .15f;
        public float firstPersonNearClip = .01f;
        public float pitchMinimum = -80, pitchMaximum = 80;
        [Tooltip("Mouse degrees per pixel only when there is no orbit camera. With orbit, reuse its sensitivity settings.")]
        public float lookGain = .1f;
        public float gamepadDegreesPerSecond = 140;
        public bool invertY;
        public LayerMask collisionLayers = ~0;
        public float eyeCollisionRadius = .08f;
        ThirdPersonController movement;
        StarterAssetsInputs inputs;
        PlayerInput playerInput;
        InputAction lookAction;
        MiningCharacterHealth health;
        PlayerKnockbackRagdoll knockdown;
        bool captured, oldFacing, oldCameraControl, oldInputLook, oldShiftLock;
        bool[] rendererStates, driverStates;
        float oldFov, oldNearClip, yaw, pitch;
        Vector2 recoil;
        CursorLockMode oldCursorLock;
        bool oldCursorVisible;
        readonly RaycastHit[] collisionHits = new RaycastHit[64];

        public bool OwnsView => isActiveAndEnabled && weapons != null && weapons.isActiveAndEnabled && weapons.HasWeapon &&
            targetCamera != null && (health == null || health.Health > 0) &&
            (knockdown == null || !knockdown.IsIncapacitated) &&
            (orbit == null || !orbit.CinematicOverrideActive) && !TowerPlacement.IsPlacing &&
            (panels == null || !panels.IsMainMenuOpen);
        public bool CanLook => OwnsView && Time.timeScale > 0 && Application.isFocused &&
            !weapons.FreeCursor && (panels == null || !panels.BlocksGameplay) &&
            (orbit == null || !orbit.IsInputLocked);
        void Awake()
        {
            movement = GetComponent<ThirdPersonController>(); inputs = GetComponent<StarterAssetsInputs>();
            playerInput = GetComponent<PlayerInput>(); health = GetComponent<MiningCharacterHealth>();
            if (playerInput != null && playerInput.actions != null) lookAction = playerInput.actions.FindAction("Look");
            knockdown = GetComponent<PlayerKnockbackRagdoll>();
        }
        void OnEnable() { if (orbit != null) orbit.RegisterFirstPersonView(this); }
        void OnDisable() { Release(); if (orbit != null) orbit.UnregisterFirstPersonView(this); }
        void Update()
        {
            if (!OwnsView) { Release(); return; }
            Capture();
            if (CanLook)
            {
                // Starter Assets' Look binding already inverts Y and scales delta to .05.
                // Read the selected control before action processors, like the orbit camera.
                var control = lookAction != null ? lookAction.activeControl : null;
                bool gamepad = control != null && control.device is Gamepad;
                Vector2 look = gamepad ? ((Gamepad)control.device).rightStick.ReadValue() :
                    Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
                float gain = gamepad ? gamepadDegreesPerSecond * Time.deltaTime :
                    orbit != null ? orbit.MouseDegreesPerPixel * orbit.MouseSensitivity : lookGain;
                yaw += look.x * gain;
                pitch = Mathf.Clamp(pitch + look.y * gain * (invertY ? 1 : -1), pitchMinimum, pitchMaximum);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
            }
            if (movement != null) { movement.ExternalFacing = true; movement.ExternalCameraControl = true; }
            if (inputs != null) inputs.cursorInputForLook = CanLook;
            Cursor.lockState = CanLook ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !CanLook;
        }
        void LateUpdate()
        {
            if (!OwnsView || !captured) return;
            Vector3 eye = transform.TransformPoint(eyeOffset);
            Vector3 origin = transform.position + Vector3.up * eyeOffset.y;
            Vector3 delta = eye - origin;
            if (delta.sqrMagnitude > .00001f && eyeCollisionRadius > 0)
            {
                int n = Physics.SphereCastNonAlloc(origin, eyeCollisionRadius, delta.normalized, collisionHits,
                    delta.magnitude, collisionLayers, QueryTriggerInteraction.Ignore);
                float distance = delta.magnitude;
                for (int i = 0; i < n; i++)
                    if (!collisionHits[i].collider.transform.IsChildOf(transform) &&
                        !collisionHits[i].collider.transform.IsChildOf(targetCamera.transform))
                        distance = Mathf.Min(distance, collisionHits[i].distance);
                eye = origin + delta.normalized * distance;
            }
            float recovery = weapons.CurrentWeapon != null ? weapons.CurrentWeapon.recoilRecovery : 0;
            recoil = Vector2.Lerp(recoil, Vector2.zero, 1 - Mathf.Exp(-recovery * Time.deltaTime));
            targetCamera.transform.SetPositionAndRotation(eye, Quaternion.Euler(
                Mathf.Clamp(pitch - recoil.x, pitchMinimum, pitchMaximum), yaw + recoil.y, 0));
            float fov = weapons.IsAiming ? weapons.CurrentWeapon.aimFieldOfView : standingFieldOfView;
            float blend = fovBlendSeconds <= 0 ? 1 : 1 - Mathf.Exp(-Time.deltaTime / fovBlendSeconds);
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, fov, blend);
        }
        public void AddRecoil(float up, float side) { recoil += new Vector2(up, side); }
        void Capture()
        {
            if (captured) return;
            oldShiftLock = orbit != null && orbit.IsShiftLocked;
            if (orbit != null) orbit.SetShiftLocked(false);
            oldFov = targetCamera.fieldOfView; oldNearClip = targetCamera.nearClipPlane;
            if (firstPersonNearClip > 0) targetCamera.nearClipPlane = firstPersonNearClip;
            yaw = transform.eulerAngles.y;
            float angle = targetCamera.transform.eulerAngles.x;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0, angle), pitchMinimum, pitchMaximum);
            oldCursorLock = Cursor.lockState; oldCursorVisible = Cursor.visible;
            if (movement != null) { oldFacing = movement.ExternalFacing; oldCameraControl = movement.ExternalCameraControl; }
            if (inputs != null) oldInputLook = inputs.cursorInputForLook;
            rendererStates = new bool[bodyRenderers == null ? 0 : bodyRenderers.Length];
            for (int i = 0; i < rendererStates.Length; i++) if (bodyRenderers[i] != null)
            { rendererStates[i] = bodyRenderers[i].enabled; bodyRenderers[i].enabled = false; }
            driverStates = new bool[otherCameraDrivers == null ? 0 : otherCameraDrivers.Length];
            for (int i = 0; i < driverStates.Length; i++) if (otherCameraDrivers[i] != null && otherCameraDrivers[i] != this && otherCameraDrivers[i] != orbit)
            { driverStates[i] = otherCameraDrivers[i].enabled; otherCameraDrivers[i].enabled = false; }
            captured = true;
        }
        void Release()
        {
            if (!captured) return;
            if (targetCamera != null) { targetCamera.fieldOfView = oldFov; targetCamera.nearClipPlane = oldNearClip; }
            if (movement != null) { movement.ExternalFacing = oldFacing; movement.ExternalCameraControl = oldCameraControl; }
            if (inputs != null) { inputs.cursorInputForLook = oldInputLook; inputs.look = Vector2.zero; }
            for (int i = 0; i < rendererStates.Length; i++) if (bodyRenderers[i] != null) bodyRenderers[i].enabled = rendererStates[i];
            for (int i = 0; i < driverStates.Length; i++) if (otherCameraDrivers[i] != null && otherCameraDrivers[i] != this && otherCameraDrivers[i] != orbit)
                otherCameraDrivers[i].enabled = driverStates[i];
            Cursor.lockState = oldCursorLock; Cursor.visible = oldCursorVisible;
            if (oldShiftLock && (health == null || health.Health > 0) && (knockdown == null || !knockdown.IsIncapacitated))
                if (orbit != null) orbit.SetShiftLocked(true);
            recoil = Vector2.zero; captured = false;
        }
    }
}
