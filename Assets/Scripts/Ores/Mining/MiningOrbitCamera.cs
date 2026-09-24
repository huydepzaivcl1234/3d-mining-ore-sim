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
        [Tooltip("Static world layers that stop the camera body. The focus point is not a collider.")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        [Tooltip("Radius used to keep the camera body away from walls.")]
        [Min(0.01f), SerializeField] private float collisionRadius = 0.35f;
        [Tooltip("Small clearance kept between the camera and a blocking surface.")]
        [Min(0f), SerializeField] private float collisionPadding = 0.08f;

        private const int CollisionHitCapacity = 32;
        private readonly RaycastHit[] collisionHits = new RaycastHit[CollisionHitCapacity];
        private readonly Collider[] overlapHits = new Collider[CollisionHitCapacity];
        private Vector3 focusPoint;
        private float distance;
        private float yaw;
        private float pitch;
        private bool inputLocked;
        private bool cinematicOverride;
        private Vector3 resolvedCameraPosition;
        private bool hasResolvedCameraPosition;
        private SphereCollider collisionEye;
        private bool ownsRuntimeCollider;
        private Tween shakeTween;
        private float shakeEnvelope;
        private float shakeStrength;
        private float shakeFrequency;
        private float shakeSeed;

        private void Awake()
        {
            controlledCamera ??= Camera.main;
            EnsureCameraBodyCollider();
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
            if (gameData == null || inputLocked || cinematicOverride)
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

            if (controlledCamera == null || gameData == null || cinematicOverride)
            {
                return;
            }

            EnsureCameraBodyCollider();

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
            cinematicOverride = false;
            hasResolvedCameraPosition = false;
        }

        private void OnDestroy()
        {
            if (ownsRuntimeCollider && collisionEye != null)
            {
                // The sphere now lives ON the camera. Never destroy the camera GameObject.
                Destroy(collisionEye);
                collisionEye = null;
            }
        }

        public Camera ControlledCamera => controlledCamera;
        public Vector3 FocusPoint => focusPoint;
        public Vector3 DefaultFocusPoint => gameData != null
            ? gameData.CameraFocusPoint
            : Vector3.zero;
        public bool CinematicOverrideActive => cinematicOverride;

        public void BeginCinematicOverride()
        {
            cinematicOverride = true;
            hasResolvedCameraPosition = false;
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
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            position = targetFocus - rotation * Vector3.forward * distance;
        }

        public void EndCinematicOverride(Vector3 targetFocus)
        {
            focusPoint = targetFocus;
            cinematicOverride = false;
            hasResolvedCameraPosition = false;
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

            // Ores, NPCs, drops, and falling Lucky Blocks must not make the camera pulse in and
            // out while they move. Static level geometry has no Rigidbody and remains blocking.
            return candidate.attachedRigidbody == null &&
                   candidate.GetComponentInParent<Ore>() == null;
        }

        private void OnValidate()
        {
            collisionRadius = Mathf.Max(0.01f, collisionRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            if (ownsRuntimeCollider && collisionEye != null)
            {
                collisionEye.radius = collisionRadius;
            }
        }
    }
}
