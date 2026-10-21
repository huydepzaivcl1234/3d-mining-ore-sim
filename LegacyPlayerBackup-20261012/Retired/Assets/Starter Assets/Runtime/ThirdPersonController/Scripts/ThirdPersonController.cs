using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        // External gameplay systems may gate sprint without modifying held input.
        public bool SprintAllowed { get; set; } = true;
        // Combat owns facing while locked; movement remains camera-relative.
        public event System.Action Jumped;
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        public bool ExternalFacing { get; set; }
        // Combat supplies intent; this motor remains the sole owner of collision-safe movement.
        public float CombatMoveMultiplier { get; set; } = 1f;
        public float LocomotionMoveMultiplier { get; set; } = 1f;
        private float slowUntil;
        private float slowMultiplier = 1f;
        public float MovementSlowMultiplier => Time.time < slowUntil ? slowMultiplier : 1f;
        // Refresh the strongest active slow; never mutate shared player stat data.
        public void ApplyMovementSlow(float percent, float seconds)
        {
            if (seconds <= 0f || percent <= 0f) return;
            if (Time.time >= slowUntil) slowMultiplier = 1f;
            slowMultiplier = Mathf.Min(slowMultiplier, 1f - Mathf.Clamp01(percent * .01f));
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }
        public Vector3 CombatStepVelocity { get; set; }
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private Vector3 _headDeflectionVelocity;
        private float _headDeflectionUntil;

        // Gameplay callers supply the impulse; this motor remains the sole owner of Move/gravity.
        public void DeflectFromMonsterHead(Vector3 direction, float sidewaysSpeed, float downwardSpeed, float seconds)
        {
            direction.y = 0f;
            _headDeflectionVelocity = direction.normalized * Mathf.Max(0f, sidewaysSpeed);
            _headDeflectionUntil = Time.time + Mathf.Max(0f, seconds);
            _verticalVelocity = -Mathf.Max(0f, downwardSpeed);
            Grounded = false;
        }
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        // Set by the project's active camera owner without coupling this package assembly to it.
        public bool ExternalCameraControl { get; set; }
        public event System.Action<Vector3, bool, float> MovementAnimationRequested;
        public float ExternalRotationSmoothTime { get; set; }

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private float _lastFootstepTime = -10f;
        private float _lastLandingTime = -10f;
        private AudioSource _footstepSource;
        public event System.Action<AudioSource> MovementAudioSourceCreated;
        public AudioSource MovementAudioSource => _footstepSource;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null)
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        // Respawn must not reuse pre-death jump velocity or the running blend.
        public void ResetMotionAfterRespawn()
        {
            slowUntil = 0f;
            slowMultiplier = 1f;
            CombatMoveMultiplier = 1f;
            LocomotionMoveMultiplier = 1f;
            CombatStepVelocity = Vector3.zero;
            _speed = _animationBlend = _rotationVelocity = 0f;
            _verticalVelocity = -2f;
            _headDeflectionUntil = 0f;
            _headDeflectionVelocity = Vector3.zero;
            _targetRotation = transform.eulerAngles.y;
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        public bool UseUnscaledMovementTime { get; set; }
        private float MovementDeltaTime => UseUnscaledMovementTime ? Time.unscaledDeltaTime : Time.deltaTime;
        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            // The active mining orbit owns all camera input; keep only locomotion here.
            if (ExternalCameraControl) return;
            if (CinemachineCameraTarget == null) return;
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
            if (Time.time < _headDeflectionUntil) Grounded = false;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : MovementDeltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint && SprintAllowed ? SprintSpeed : MoveSpeed;
            targetSpeed *= MovementSlowMultiplier;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            // CharacterController.velocity uses scaled time, which stops in a rune session.
            float currentHorizontalSpeed = UseUnscaledMovementTime ? _speed :
                new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    MovementDeltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed * Mathf.Clamp01(CombatMoveMultiplier),
                MovementDeltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            Vector3 animationDirection = Quaternion.Euler(0f, _mainCamera.transform.eulerAngles.y, 0f) * inputDirection;
            MovementAnimationRequested?.Invoke(animationDirection, _input.sprint && SprintAllowed, MovementDeltaTime);

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                // Don't accumulate an unused camera-relative turn velocity while
                // combat owns facing; the handoff must begin from the current pose.
                if (!ExternalFacing)
                {
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                        ExternalRotationSmoothTime > 0f ? ExternalRotationSmoothTime : RotationSmoothTime,
                        Mathf.Infinity, MovementDeltaTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
                else _rotationVelocity = 0f;
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move((targetDirection.normalized * (_speed * Mathf.Clamp01(CombatMoveMultiplier) *
                             Mathf.Clamp01(LocomotionMoveMultiplier)) +
                             Vector3.ProjectOnPlane(CombatStepVelocity, Vector3.up)) * MovementDeltaTime +
                             ((_headDeflectionUntil > Time.time ? _headDeflectionVelocity : Vector3.zero) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f)) * MovementDeltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f && _verticalVelocity <= 0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    Jumped?.Invoke();

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= MovementDeltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= MovementDeltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * MovementDeltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (isActiveAndEnabled && animationEvent.animatorClipInfo.weight > 0.01f)
                PlayFootstep();
        }

        private void PlayFootstep()
        {
            if (Time.time - _lastFootstepTime < 0.25f || FootstepAudioClips == null ||
                FootstepAudioClips.Length == 0) return;
            int start = Random.Range(0, FootstepAudioClips.Length);
            AudioClip clip = null;
            for (int i = 0; i < FootstepAudioClips.Length; i++)
            {
                clip = FootstepAudioClips[(start + i) % FootstepAudioClips.Length];
                if (clip != null) break;
            }
            if (clip == null) return;
            PlayMovementSound(clip);
            _lastFootstepTime = Time.time;
        }

        private void PlayMovementSound(AudioClip clip)
        {
            if (clip == null) return;
            if (_footstepSource == null)
            {
                _footstepSource = gameObject.AddComponent<AudioSource>();
                _footstepSource.playOnAwake = false;
                _footstepSource.loop = false;
                _footstepSource.spatialBlend = 0;
                MovementAudioSourceCreated?.Invoke(_footstepSource);
            }
            _footstepSource.PlayOneShot(clip, FootstepAudioVolume);
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (isActiveAndEnabled && animationEvent.animatorClipInfo.weight > 0.01f &&
                Time.time - _lastLandingTime >= 0.1f)
            {
                PlayMovementSound(LandingAudioClip);
                _lastLandingTime = Time.time;
            }
        }
    }
}
