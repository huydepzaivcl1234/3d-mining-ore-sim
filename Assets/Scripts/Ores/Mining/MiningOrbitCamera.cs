using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MiningSimulator.Ores
{
    /// <summary>Provides 360-degree orbit, keyboard movement, and mouse-wheel zoom.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private MiningGameData gameData;

        [Header("Collision")]
        [Tooltip("Static world layers that stop camera movement and block the camera boom.")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        [Tooltip("Radius used to keep the camera body away from walls.")]
        [Min(0.01f), SerializeField] private float collisionRadius = 0.35f;
        [Tooltip("Small clearance kept between the camera and a blocking surface.")]
        [Min(0f), SerializeField] private float collisionPadding = 0.08f;
        [Tooltip("Height of the horizontal probe that prevents keyboard movement through walls.")]
        [Min(0f), SerializeField] private float focusCollisionHeight = 1f;

        private const int CollisionHitCapacity = 32;
        private readonly RaycastHit[] collisionHits = new RaycastHit[CollisionHitCapacity];
        private Vector3 focusPoint;
        private float distance;
        private float yaw;
        private float pitch;
        private bool inputLocked;
        private Tween shakeTween;
        private float shakeEnvelope;
        private float shakeStrength;
        private float shakeFrequency;
        private float shakeSeed;

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
            if (gameData == null || inputLocked)
            {
                return;
            }

            ReadKeyboard();
            ReadMouse();
        }

        /// <summary>Called by MiningUiPanelCoordinator while a modal (Shop, Upgrade,
        /// Rebirth...) is open, so orbit/pan/zoom can't fight with clicking/scrolling the UI.</summary>
        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
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

            Vector3 position = ResolveCameraPosition(desiredPosition);
            controlledCamera.transform.SetPositionAndRotation(position, rotation);
        }

        public void PlayRewardShake(float strength, float duration, float frequency)
        {
            if (strength <= 0f || duration <= 0f)
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
            if (shakeTween.isAlive)
            {
                shakeTween.Stop();
            }
            shakeEnvelope = 0f;
        }

        private static float SampleShake(float time, float seed)
        {
            return Mathf.PerlinNoise(time, seed) * 2f - 1f;
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
            MoveFocusPoint(direction *
                           (gameData.CameraMoveSpeed * speedMultiplier * Time.deltaTime));

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

            // If the pointer is over a UI element (a panel, a scrollable list, a button...),
            // none of this input should reach the 3D camera — otherwise scrolling a UI list
            // also zooms the camera underneath it, and dragging a UI element can spin the view.
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
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

            float scroll = mouse.scroll.ReadValue().y;
            distance = Mathf.Clamp(distance - scroll * gameData.CameraZoomSpeed,
                gameData.CameraMinimumDistance, gameData.CameraMaximumDistance);
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private Vector3 ResolveCameraPosition(Vector3 desiredPosition)
        {
            // The gameplay focus commonly sits exactly on the ground plane. Lifting only the
            // cast origin prevents an initial ground overlap from hiding a floor collision when
            // the player rotates the camera below the surface.
            Vector3 castOrigin = focusPoint + Vector3.up * (collisionRadius + collisionPadding);
            Vector3 offset = desiredPosition - castOrigin;
            float castDistance = offset.magnitude;
            if (!TryGetClosestObstruction(castOrigin, offset, castDistance, out RaycastHit hit))
            {
                return desiredPosition;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - collisionPadding);
            return castOrigin + offset.normalized * safeDistance;
        }

        private void MoveFocusPoint(Vector3 movement)
        {
            movement.y = 0f;
            float moveDistance = movement.magnitude;
            if (moveDistance <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 direction = movement / moveDistance;
            Vector3 probeOrigin = focusPoint + Vector3.up * focusCollisionHeight;
            if (!TryGetClosestObstruction(probeOrigin, direction, moveDistance,
                    out RaycastHit hit))
            {
                focusPoint += movement;
                return;
            }

            float forwardDistance = Mathf.Max(0f, hit.distance - collisionPadding);
            focusPoint += direction * forwardDistance;

            // Keep diagonal movement responsive by sliding along the wall instead of stopping
            // completely. A second cast prevents the slide from cutting through a nearby corner.
            Vector3 remaining = movement - direction * forwardDistance;
            Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
            slide.y = 0f;
            float slideDistance = slide.magnitude;
            if (slideDistance <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 slideDirection = slide / slideDistance;
            probeOrigin = focusPoint + Vector3.up * focusCollisionHeight;
            if (TryGetClosestObstruction(probeOrigin, slideDirection, slideDistance,
                    out RaycastHit slideHit))
            {
                slideDistance = Mathf.Max(0f, slideHit.distance - collisionPadding);
            }

            focusPoint += slideDirection * slideDistance;
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
            int hitCount = Physics.SphereCastNonAlloc(origin, collisionRadius, direction,
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

            // Ores, NPCs, drops, and falling Lucky Blocks must not make the camera pulse in and
            // out while they move. Static level geometry has no Rigidbody and remains blocking.
            return candidate.attachedRigidbody == null &&
                   candidate.GetComponentInParent<Ore>() == null;
        }

        private void OnValidate()
        {
            collisionRadius = Mathf.Max(0.01f, collisionRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            focusCollisionHeight = Mathf.Max(collisionRadius, focusCollisionHeight);
        }
    }
}
