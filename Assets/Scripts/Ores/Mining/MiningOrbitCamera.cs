using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiningSimulator.Ores
{
    /// <summary>Provides 360-degree orbit, keyboard movement, and mouse-wheel zoom.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class MiningOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private MiningGameData gameData;

        [Header("Third Person (scene references)")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 followOffset = new(0f, 1.5f, 0f);
        [Min(0.5f), SerializeField] private float followDistance = 4.5f;
        [Range(-70f, 80f), SerializeField] private float followPitch = 18f;
        [SerializeField] private bool rotateOnlyWhileRightMouseHeld = true;
        [Tooltip("Smoothly return behind the player after manual orbit. Attacking/aiming cannot steer the camera.")]
        [SerializeField] private bool followPlayerHeading = true;
        [Min(0.01f), SerializeField] private float headingSmoothTime = 0.3f;
        [Min(0f), SerializeField] private float manualOrbitResumeDelay = 1f;
        [Header("Shift lock (camera leads, character follows)")]
        [SerializeField] private InputAction toggleShiftLock = new InputAction("Shift Lock", InputActionType.Button, "<Keyboard>/leftShift");
        [Min(.01f), SerializeField] private float shiftFacingSmoothSeconds = .15f;
        [Min(0f), SerializeField] private float shiftFacingMaximumSpeed = 540f;
        [SerializeField] private bool suppressHeadingDuringCombat = true;
        private bool shiftLocked;
        public InputAction ShiftLockAction => toggleShiftLock;
        private bool ownsCursor;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private float shiftFacingVelocity;
        private MiningCharacterHealth playerHealth;
        private const string MouseSensitivityKey = "MiningSimulator.MouseSensitivity.v1";
        private float mouseSensitivity = 1f;
        public float MouseSensitivity => mouseSensitivity;
        public float SensitivityMinimum => gameData != null ? gameData.MouseSensitivityMinimum : .25f;
        public float SensitivityMaximum => gameData != null ? gameData.MouseSensitivityMaximum : 4f;
        private float EffectiveMaximumRotationSpeed => (IsShiftLocked ? gameData.ShiftLockMaximumRotationSpeed : gameData.CameraMaximumRotationSpeed) * mouseSensitivity;
        public void SetMouseSensitivity(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            mouseSensitivity = Mathf.Clamp(value, SensitivityMinimum, SensitivityMaximum);
            PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
        }
        public bool IsShiftLocked => shiftLocked && !inputLocked && !cinematicOverride &&
            !FollowingKnockdown && isActiveAndEnabled && Time.timeScale > 0f && (playerHealth == null || playerHealth.Health > 0f);
        private PlayerKnockbackRagdoll playerKnockdown;
        private bool FollowingKnockdown => playerKnockdown != null && playerKnockdown.IsIncapacitated;

        [Header("Combat framing (follows the player's Standing/Combat mode)")]
        [SerializeField] private bool adaptiveCombatFraming = true;
        [Tooltip("Disable reward camera shake for players sensitive to camera motion.")]
        [SerializeField] private bool reduceMotion = true;
        [Min(0.5f), SerializeField] private float explorationDistance = 4f;
        [Min(0.5f), SerializeField] private float combatDistance = 7f;
        [Range(1f, 179f), SerializeField] private float explorationFov = 60f;
        [Range(1f, 179f), SerializeField] private float combatFov = 70f;
        [Range(0f, 30f), SerializeField] private float combatPitchOffset = 6f;
        [Min(0.01f), SerializeField] private float framingSmoothTime = 0.35f;
        [Header("Collision")]
        [Tooltip("Static world layers that stop the camera body. The focus point is not a collider.")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        [Tooltip("Radius used to keep the camera body away from walls.")]
        [Min(0.01f), SerializeField] private float collisionRadius = 0.35f;
        [Tooltip("Small clearance kept between the camera and a blocking surface.")]
        [Min(0f), SerializeField] private float collisionPadding = 0.08f;
        [Tooltip("Time to ease back to the normal orbit distance after leaving a wall. Moving inward stays collision-safe.")]
        [Min(0.01f), SerializeField] private float collisionReturnSmoothTime = 0.25f;

        private const int CollisionHitCapacity = 32;
        private readonly RaycastHit[] collisionHits = new RaycastHit[CollisionHitCapacity];
        private readonly Collider[] overlapHits = new Collider[CollisionHitCapacity];
        private Vector3 focusPoint;
        private float distance;
        private float yaw;
        private float headingVelocity;
        private float lastManualOrbitTime = float.NegativeInfinity;
        private float pitch;
        private float zoomOffset;
        private float distanceVelocity;
        private float fovVelocity;
        private float pitchOffsetVelocity;
        private float currentPitchOffset;
        private float unobstructedDistance;
        private float obstructionReturnVelocity;
        private PlayerCombatInput combatInput;
        private bool inputLocked;
        private bool cinematicOverride;
        private Vector3 resolvedCameraPosition;
        private bool hasResolvedCameraPosition;
        private SphereCollider collisionEye;
        private PlayerInput playerInput;
        private StarterAssets.ThirdPersonController playerMovement;
        private bool ownsRuntimeCollider;
        private Tween shakeTween;
        private float shakeEnvelope;
        private float shakeStrength;
        private float shakeFrequency;
        private float shakeSeed;
        private Volume rotationBlurVolume;
        private VolumeProfile rotationBlurProfile;
        private MotionBlur rotationBlur;
        private UniversalAdditionalCameraData postCamera;
        private bool enabledPostProcessing;

        private void Awake()
        {
            mouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey,
                gameData != null ? gameData.MouseSensitivityDefault : 1f), SensitivityMinimum, SensitivityMaximum);
            controlledCamera ??= Camera.main;
            EnsureCameraBodyCollider();
            if (gameData != null)
            {
                focusPoint = gameData.CameraFocusPoint;
                distance = gameData.CameraDistance;
                yaw = gameData.CameraYaw;
                pitch = gameData.CameraPitch;
            }
            if (followTarget != null)
            {
                focusPoint = followTarget.position + followOffset;
                yaw = followTarget.eulerAngles.y;
                pitch = followPitch;
                distance = followDistance;
                playerInput = followTarget.GetComponentInChildren<PlayerInput>(true);
                combatInput = followTarget.GetComponent<PlayerCombatInput>();
                playerHealth = followTarget.GetComponent<MiningCharacterHealth>();
                playerKnockdown = followTarget.GetComponent<PlayerKnockbackRagdoll>();
                playerMovement = followTarget.GetComponent<StarterAssets.ThirdPersonController>();
                if (playerMovement != null) playerMovement.ExternalCameraControl = true;
            }
        }

        private void Update()
        {
            if (shiftLocked && (FollowingKnockdown || playerHealth != null && playerHealth.Health <= 0f)) SetShiftLocked(false);
            if (followTarget != null && gameData != null && !inputLocked && !cinematicOverride && Time.timeScale > 0f &&
                !FollowingKnockdown && toggleShiftLock != null && toggleShiftLock.WasPressedThisFrame()) SetShiftLocked(!shiftLocked);
            UpdateShiftCursor();
            if (IsShiftLocked && playerMovement != null) playerMovement.ExternalFacing = true;
            if (gameData == null || inputLocked || cinematicOverride)
            {
                return;
            }

            ReadKeyboard();
            ReadMouse();
        }

        private void OnEnable()
        {
            toggleShiftLock?.Enable();
            if (playerMovement != null) playerMovement.ExternalCameraControl = true;
            EnsureRotationBlur();
        }

        /// <summary>Called by MiningUiPanelCoordinator while a modal (Shop, Upgrade,
        /// Rebirth...) is open, so orbit/pan/zoom can't fight with clicking/scrolling the UI.</summary>
        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (followTarget == null) return;
            if (playerInput == null) playerInput = followTarget.GetComponentInChildren<PlayerInput>(true);
            if (playerInput == null) return;
            if (locked)
            {
                playerInput.DeactivateInput();
                // Starter Assets stores movement outside PlayerInput. Clear the values so
                // a menu opened while running cannot leave the character moving.
                Component inputs = playerInput.GetComponent("StarterAssetsInputs");
                if (inputs != null)
                {
                    System.Type type = inputs.GetType();
                    type.GetField("move")?.SetValue(inputs, Vector2.zero);
                    type.GetField("look")?.SetValue(inputs, Vector2.zero);
                    type.GetField("jump")?.SetValue(inputs, false);
                    type.GetField("sprint")?.SetValue(inputs, false);
                }
            }
            else playerInput.ActivateInput();
        }

        private void LateUpdate()
        {
            if (controlledCamera == null)
            {
                controlledCamera = Camera.main;
            }

            if (controlledCamera == null || gameData == null || cinematicOverride)
            {
                if (rotationBlur != null) rotationBlur.intensity.value = 0f;
                return;
            }

            EnsureCameraBodyCollider();

            if (followTarget != null)
                focusPoint = FollowingKnockdown ? playerKnockdown.CameraFocusPosition : followTarget.position + followOffset;

            UpdateFollowHeading();
            UpdateCombatFraming();

            Quaternion rotation = ResolveComfortRotation(Quaternion.Euler(EffectivePitch, yaw, 0f));
            Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * distance;
            if (shakeEnvelope > 0f)
            {
                float sampleTime = Time.unscaledTime * shakeFrequency;
                Vector3 localShake = new(
                    SampleShake(sampleTime, shakeSeed),
                    SampleShake(sampleTime, shakeSeed + 17.31f),
                    0f);
                desiredPosition += rotation * (localShake * (shakeStrength * shakeEnvelope));
            }

            Vector3 position = followTarget != null
                ? ResolveFollowCameraPosition(desiredPosition, RuneStation.PlayerDeltaTime)
                : ResolveCameraPosition(desiredPosition);
            controlledCamera.transform.SetPositionAndRotation(position, rotation);
            if (IsShiftLocked && followTarget != null && (combatInput == null || !combatInput.IsTrackingLunge))
            {
                float facing = Mathf.SmoothDampAngle(followTarget.eulerAngles.y, rotation.eulerAngles.y,
                    ref shiftFacingVelocity, shiftFacingSmoothSeconds, shiftFacingMaximumSpeed, RuneStation.PlayerDeltaTime);
                followTarget.rotation = Quaternion.Euler(0f, facing, 0f);
            }
        }

        public void SetShiftLocked(bool locked)
        {
            shiftLocked = locked && followTarget != null && !FollowingKnockdown;
            headingVelocity = 0f;
            shiftFacingVelocity = 0f;
            lastManualOrbitTime = Time.unscaledTime;
            if (playerMovement != null) playerMovement.ExternalFacing = IsShiftLocked;
            UpdateShiftCursor();
        }

        private void UpdateShiftCursor()
        {
            if (IsShiftLocked && Application.isFocused)
            {
                if (!ownsCursor) { previousCursorLock = Cursor.lockState; previousCursorVisible = Cursor.visible; ownsCursor = true; }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (ownsCursor)
            {
                Cursor.lockState = previousCursorLock;
                Cursor.visible = previousCursorVisible;
                ownsCursor = false;
            }
        }

        public void PlayRewardShake(float strength, float duration, float frequency)
        {
            if (reduceMotion || strength <= 0f || duration <= 0f)
            {
                return;
            }

            if (shakeTween.isAlive)
            {
                shakeTween.Stop();
            }

            shakeStrength = strength;
            shakeFrequency = Mathf.Max(0.1f, frequency);
            shakeSeed = Random.Range(0f, 1000f);
            shakeEnvelope = 1f;
            shakeTween = Tween.Custom(this, 1f, 0f, duration,
                    static (cameraRig, envelope) => cameraRig.shakeEnvelope = envelope,
                    Ease.OutQuad)
                .OnComplete(this, static cameraRig => cameraRig.shakeEnvelope = 0f);
        }

        private void OnDisable()
        {
            SetShiftLocked(false);
            toggleShiftLock?.Disable();
            ReleaseRotationBlur();
            if (playerMovement != null) playerMovement.ExternalCameraControl = false;
            if (shakeTween.isAlive)
            {
                shakeTween.Stop();
            }
            shakeEnvelope = 0f;
            cinematicOverride = false;
            hasResolvedCameraPosition = false;
            unobstructedDistance = 0f;
        }

        private void OnDestroy()
        {
            toggleShiftLock?.Dispose();
            ReleaseRotationBlur();
            if (ownsRuntimeCollider && collisionEye != null)
            {
                // The sphere now lives ON the camera. Never destroy the camera GameObject.
                Destroy(collisionEye);
                collisionEye = null;
            }
        }

        public Camera ControlledCamera => controlledCamera;
        private float EffectivePitch => Mathf.Clamp(pitch + currentPitchOffset, -85f, 85f);
        public Transform FollowTarget => followTarget;
        public Vector3 FocusPoint => focusPoint;
        public Vector3 DefaultFocusPoint => gameData != null
            ? gameData.CameraFocusPoint
            : Vector3.zero;
        public bool CinematicOverrideActive => cinematicOverride;

        public void BeginCinematicOverride()
        {
            cinematicOverride = true;
            hasResolvedCameraPosition = false;
            unobstructedDistance = 0f;
        }

        public void SetCinematicPose(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            if (!cinematicOverride)
            {
                return;
            }

            controlledCamera ??= Camera.main;
            if (controlledCamera == null)
            {
                return;
            }

            controlledCamera.transform.SetPositionAndRotation(position, rotation);
            controlledCamera.fieldOfView = fieldOfView;
        }

        public void GetGameplayPose(Vector3 targetFocus, out Vector3 position,
            out Quaternion rotation)
        {
            rotation = Quaternion.Euler(EffectivePitch, yaw, 0f);
            position = targetFocus - rotation * Vector3.forward * distance;
        }

        private void UpdateCombatFraming()
        {
            if (!adaptiveCombatFraming || followTarget == null) return;
            // Nearby monsters must not change the camera while the player is Standing.
            // The combat input is the single owner of the Standing/Combat transition.
            bool combat = combatInput != null && combatInput.IsCombatMode;
            float smooth = Mathf.Max(0.01f, framingSmoothTime);
            float baseDistance = combat ? combatDistance : explorationDistance;
            float targetDistance = Mathf.Clamp(baseDistance + zoomOffset,
                gameData.CameraMinimumDistance, gameData.CameraMaximumDistance);
            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity,
                smooth, Mathf.Infinity, RuneStation.PlayerDeltaTime);
            currentPitchOffset = Mathf.SmoothDamp(currentPitchOffset,
                combat ? combatPitchOffset : 0f, ref pitchOffsetVelocity,
                smooth, Mathf.Infinity, RuneStation.PlayerDeltaTime);
            controlledCamera.fieldOfView = Mathf.SmoothDamp(controlledCamera.fieldOfView,
                combat ? combatFov : explorationFov, ref fovVelocity,
                smooth, Mathf.Infinity, RuneStation.PlayerDeltaTime);
        }

        private void UpdateFollowHeading()
        {
            if (FollowingKnockdown) { headingVelocity = 0f; return; }
            // Never feed combat-facing changes back into the camera yaw.
            if (IsShiftLocked || suppressHeadingDuringCombat && combatInput != null && combatInput.IsCombatMode)
            { headingVelocity = 0f; return; }
            if (!followPlayerHeading || followTarget == null || inputLocked ||
                Time.unscaledTime - lastManualOrbitTime < manualOrbitResumeDelay ||
                (playerMovement != null && playerMovement.ExternalFacing)) return;
            // The character's ordinary movement sets its heading. Soft aim owns ExternalFacing
            // during attacks, so acquiring a monster never hijacks the view.
            yaw = Mathf.SmoothDampAngle(yaw, followTarget.eulerAngles.y, ref headingVelocity,
                headingSmoothTime, Mathf.Infinity, RuneStation.PlayerDeltaTime);
        }

        private Quaternion ResolveComfortRotation(Quaternion desired)
        {
            Quaternion previous = controlledCamera.transform.rotation;
            float dt = RuneStation.PlayerDeltaTime;
            if (dt <= 0f || inputLocked)
            {
                if (rotationBlur != null) rotationBlur.intensity.value = 0f;
                return previous;
            }
            // Limit real rendered angular speed as well as mouse input. Follow/recentre
            // rotations use the same comfort path; position uses this exact rotation.
            Quaternion smooth = Quaternion.Slerp(previous, desired,
                1f - Mathf.Exp(-dt / (IsShiftLocked ? gameData.ShiftLockRotationSmoothSeconds : gameData.CameraRotationSmoothSeconds)));
            Quaternion result = Quaternion.RotateTowards(previous, smooth,
                EffectiveMaximumRotationSpeed * dt);
            EnsureRotationBlur();
            if (rotationBlur != null)
            {
                float angularSpeed = Quaternion.Angle(previous, result) / dt;
                float strength = gameData.CameraRotationBlurStrength *
                    Mathf.InverseLerp(40f, EffectiveMaximumRotationSpeed, angularSpeed);
                rotationBlur.intensity.value = Mathf.Lerp(rotationBlur.intensity.value,
                    strength, 1f - Mathf.Exp(-dt / 0.08f));
                rotationBlur.clamp.value = gameData.CameraRotationBlurClamp;
            }
            return result;
        }

        private void EnsureRotationBlur()
        {
            if (!Application.isPlaying || controlledCamera == null || gameData == null ||
                rotationBlurVolume != null || UniversalRenderPipeline.asset == null) return;
            postCamera = controlledCamera.GetComponent<UniversalAdditionalCameraData>();
            if (postCamera == null || postCamera.volumeLayerMask.value == 0) return;
            int layer = 0;
            while ((postCamera.volumeLayerMask.value & (1 << layer)) == 0 && layer < 31) layer++;
            var host = new GameObject("Camera Rotation Blur (runtime)");
            host.hideFlags = HideFlags.DontSave;
            host.layer = layer;
            host.transform.SetParent(controlledCamera.transform, false);
            rotationBlurProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            rotationBlurProfile.hideFlags = HideFlags.DontSave;
            rotationBlur = rotationBlurProfile.Add<MotionBlur>(true);
            rotationBlur.mode.value = MotionBlurMode.CameraOnly;
            rotationBlur.quality.value = MotionBlurQuality.Low;
            rotationBlur.intensity.value = 0f;
            rotationBlur.clamp.value = gameData.CameraRotationBlurClamp;
            rotationBlurVolume = host.AddComponent<Volume>();
            rotationBlurVolume.isGlobal = true;
            rotationBlurVolume.priority = 100f;
            rotationBlurVolume.sharedProfile = rotationBlurProfile;
            enabledPostProcessing = !postCamera.renderPostProcessing;
            if (enabledPostProcessing) postCamera.renderPostProcessing = true;
        }

        private void ReleaseRotationBlur()
        {
            if (rotationBlurVolume != null) Destroy(rotationBlurVolume.gameObject);
            if (rotationBlurProfile != null)
            {
                // Runtime-owned components must be released with their profile.
                foreach (var component in rotationBlurProfile.components)
                    if (component != null) Destroy(component);
                Destroy(rotationBlurProfile);
            }
            if (enabledPostProcessing && postCamera != null) postCamera.renderPostProcessing = false;
            enabledPostProcessing = false;
            rotationBlur = null;
            rotationBlurVolume = null;
            rotationBlurProfile = null;
        }

        public void EndCinematicOverride(Vector3 targetFocus)
        {
            focusPoint = targetFocus;
            cinematicOverride = false;
            hasResolvedCameraPosition = false;
            unobstructedDistance = 0f;
        }

        private static float SampleShake(float time, float seed)
        {
            return Mathf.PerlinNoise(time, seed) * 2f - 1f;
        }

        private void ReadKeyboard()
        {
            // WASD belongs to the character when following it.
            if (followTarget != null) return;
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
            MoveFocusPoint(direction *
                           (gameData.CameraMoveSpeed * speedMultiplier * RuneStation.PlayerDeltaTime));

            float rotationInput = ReadAxis(keyboard.qKey, keyboard.eKey);
            yaw += rotationInput * gameData.CameraKeyboardRotationSpeed * RuneStation.PlayerDeltaTime;
        }

        private void ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            // If the pointer is over a UI element (a panel, a scrollable list, a button...),
            // none of this input should reach the 3D camera — otherwise scrolling a UI list
            // also zooms the camera underneath it, and dragging a UI element can spin the view.
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            if (IsShiftLocked || !rotateOnlyWhileRightMouseHeld || mouse.rightButton.isPressed)
            {
                if (mouse.rightButton.isPressed || delta.sqrMagnitude > 0f)
                {
                    lastManualOrbitTime = Time.unscaledTime;
                    headingVelocity = 0f;
                }
                Vector2 degrees = Vector2.ClampMagnitude(delta * gameData.CameraRotationDegreesPerPixel * mouseSensitivity,
                    EffectiveMaximumRotationSpeed * RuneStation.PlayerDeltaTime);
                yaw += degrees.x;
                pitch = Mathf.Clamp(pitch - degrees.y,
                    gameData.CameraMinimumPitch, gameData.CameraMaximumPitch);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (adaptiveCombatFraming && followTarget != null)
                zoomOffset = Mathf.Clamp(zoomOffset - scroll * gameData.CameraZoomSpeed,
                    gameData.CameraMinimumDistance - combatDistance,
                    gameData.CameraMaximumDistance - explorationDistance);
            else
                distance = Mathf.Clamp(distance - scroll * gameData.CameraZoomSpeed,
                    gameData.CameraMinimumDistance, gameData.CameraMaximumDistance);
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private Vector3 ResolveCameraPosition(Vector3 desiredPosition)
        {
            // The focus may pass through a wall. Only the physical camera position has
            // collision: sweep its body from its last position to its desired orbit pose.
            if (!hasResolvedCameraPosition)
            {
                resolvedCameraPosition = ResolveEyeOverlap(controlledCamera.transform.position);
                hasResolvedCameraPosition = true;
            }
            resolvedCameraPosition = ResolveEyeOverlap(ResolveEyeMovement(desiredPosition));
            return resolvedCameraPosition;
        }

        private Vector3 ResolveFollowCameraPosition(Vector3 desiredPosition, float deltaTime)
        {
            // Solve the intended orbit ray, not the previous camera's wall-slide path.
            // Sliding first changes its height; a subsequent focus cast pulls it back,
            // feeding a different slide direction into the next frame.
            Vector3 offset = desiredPosition - focusPoint;
            float length = offset.magnitude;
            if (length <= Mathf.Epsilon) return ResolveEyeOverlap(focusPoint);

            Vector3 direction = offset / length;
            float safeDistance = length;
            if (TryGetClosestObstruction(focusPoint, direction, length, out RaycastHit hit))
                safeDistance = Mathf.Max(0f, hit.distance - collisionPadding);

            if (!hasResolvedCameraPosition || safeDistance <= unobstructedDistance)
            {
                unobstructedDistance = safeDistance;
                // Discard outward momentum when the wall forces us inward.
                obstructionReturnVelocity = 0f;
            }
            else if (deltaTime > 0f)
            {
                unobstructedDistance = Mathf.SmoothDamp(unobstructedDistance, safeDistance,
                    ref obstructionReturnVelocity, collisionReturnSmoothTime,
                    Mathf.Infinity, deltaTime);
            }

            // No minimum zoom clamp: a close wall must be allowed to bring the eye
            // closer than the ordinary mouse-wheel zoom limit.
            resolvedCameraPosition = ResolveEyeOverlap(focusPoint + direction *
                Mathf.Min(unobstructedDistance, safeDistance));
            hasResolvedCameraPosition = true;
            return resolvedCameraPosition;
        }

        private Vector3 ResolveEyeMovement(Vector3 targetPosition)
        {
            if (!hasResolvedCameraPosition)
            {
                return targetPosition;
            }

            Vector3 movement = targetPosition - resolvedCameraPosition;
            float movementDistance = movement.magnitude;
            if (movementDistance <= Mathf.Epsilon ||
                !TryGetClosestObstruction(resolvedCameraPosition, movement, movementDistance,
                    out RaycastHit hit))
            {
                return targetPosition;
            }

            Vector3 direction = movement / movementDistance;
            float forwardDistance = Mathf.Max(0f, hit.distance - collisionPadding);
            Vector3 position = resolvedCameraPosition + direction * forwardDistance;
            Vector3 remaining = targetPosition - position;
            Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
            float slideDistance = slide.magnitude;
            if (slideDistance <= Mathf.Epsilon)
            {
                return position;
            }

            Vector3 slideDirection = slide / slideDistance;
            if (TryGetClosestObstruction(position, slideDirection, slideDistance,
                    out RaycastHit slideHit))
            {
                slideDistance = Mathf.Max(0f, slideHit.distance - collisionPadding);
            }

            return position + slideDirection * slideDistance;
        }

        private Vector3 ResolveEyeOverlap(Vector3 position)
        {
            EnsureCameraBodyCollider();
            if (collisionEye == null || collisionLayers.value == 0)
            {
                return position;
            }

            // Resolve a few contacts so the virtual eye can escape wall corners instead of
            // remaining wedged when a fast orbit begins with the camera already intersecting.
            for (int pass = 0; pass < 3; pass++)
            {
                int count = Physics.OverlapSphereNonAlloc(position, BodyRadius,
                    overlapHits, collisionLayers, QueryTriggerInteraction.Ignore);
                bool moved = false;
                for (int index = 0; index < count; index++)
                {
                    Collider candidate = overlapHits[index];
                    if (!IsCameraObstruction(candidate) ||
                        !Physics.ComputePenetration(collisionEye, position,
                            Quaternion.identity, candidate, candidate.transform.position,
                            candidate.transform.rotation, out Vector3 direction,
                            out float penetration))
                    {
                        continue;
                    }

                    position += direction * (penetration + collisionPadding);
                    moved = true;
                }

                if (!moved)
                {
                    break;
                }
            }

            return position;
        }

        private float BodyRadius
        {
            get
            {
                if (collisionEye == null) return collisionRadius;
                Vector3 scale = collisionEye.transform.lossyScale;
                float largestAxis = Mathf.Max(Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                return Mathf.Max(0.01f, collisionEye.radius * largestAxis);
            }
        }

        private void EnsureCameraBodyCollider()
        {
            if (controlledCamera == null || collisionEye != null) return;
            collisionEye = controlledCamera.GetComponent<SphereCollider>();
            if (collisionEye != null) return;

            // The actual camera owns the sphere. Movement is constrained by the sweeps below;
            // a trigger avoids pushing physics-driven NPCs while the orbit moves each frame.
            collisionEye = controlledCamera.gameObject.AddComponent<SphereCollider>();
            collisionEye.radius = collisionRadius;
            collisionEye.isTrigger = true;
            ownsRuntimeCollider = true;
        }

        [ContextMenu("Add Camera Body Collider In Scene")]
        private void AddCameraBodyColliderInScene()
        {
            controlledCamera ??= Camera.main;
            if (controlledCamera == null) return;
            if (controlledCamera.GetComponent<SphereCollider>() != null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SphereCollider authored = UnityEditor.Undo.AddComponent<SphereCollider>(
                    controlledCamera.gameObject);
                UnityEditor.Undo.RecordObject(authored, "Set Camera Body Collider");
                authored.radius = collisionRadius;
                authored.isTrigger = true;
                collisionEye = authored;
                ownsRuntimeCollider = false;
                return;
            }
#endif
            EnsureCameraBodyCollider();
        }

        private void MoveFocusPoint(Vector3 movement)
        {
            movement.y = 0f;
            float moveDistance = movement.magnitude;
            if (moveDistance <= Mathf.Epsilon)
            {
                return;
            }

            focusPoint += movement;
        }

        private bool TryGetClosestObstruction(Vector3 origin, Vector3 direction,
            float castDistance, out RaycastHit closestHit)
        {
            closestHit = default;
            if (castDistance <= Mathf.Epsilon || direction.sqrMagnitude <= Mathf.Epsilon ||
                collisionLayers.value == 0)
            {
                return false;
            }

            direction.Normalize();
            int hitCount = Physics.SphereCastNonAlloc(origin, BodyRadius, direction,
                collisionHits, castDistance, collisionLayers, QueryTriggerInteraction.Ignore);
            float closestDistance = float.PositiveInfinity;
            bool found = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit candidate = collisionHits[index];
                if (!IsCameraObstruction(candidate.collider) ||
                    candidate.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = candidate.distance;
                closestHit = candidate;
                found = true;
            }

            return found;
        }

        private bool IsCameraObstruction(Collider candidate)
        {
            if (candidate == null || candidate.isTrigger)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            if (controlledCamera != null &&
                (candidateTransform == controlledCamera.transform ||
                 candidateTransform.IsChildOf(controlledCamera.transform)))
            {
                return false;
            }

            if (followTarget != null && (candidateTransform == followTarget ||
                candidateTransform.IsChildOf(followTarget))) return false;

            // Gameplay objects can have static colliders (CharacterController monsters and
            // chest MeshColliders). They are not walls and must not cause zoom pulses.
            return candidate.attachedRigidbody == null &&
                   candidate.GetComponentInParent<MushroomMonster>() == null &&
                   candidate.GetComponentInParent<MiningChest>() == null &&
                   candidate.GetComponentInParent<LuckyBlock>() == null;
        }

        private void OnValidate()
        {
            collisionRadius = Mathf.Max(0.01f, collisionRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            collisionReturnSmoothTime = Mathf.Max(0.01f, collisionReturnSmoothTime);
            followDistance = Mathf.Max(0.5f, followDistance);
            explorationDistance = Mathf.Max(0.5f, explorationDistance);
            combatDistance = Mathf.Max(0.5f, combatDistance);
            framingSmoothTime = Mathf.Max(0.01f, framingSmoothTime);
            if (ownsRuntimeCollider && collisionEye != null)
            {
                collisionEye.radius = collisionRadius;
            }
        }
    }
}
