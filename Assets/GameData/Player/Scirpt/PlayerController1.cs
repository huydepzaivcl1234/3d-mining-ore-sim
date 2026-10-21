using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using GameData.Player.Manager;

namespace GameData.Player.Script
{
    public class PlayerController1 : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, HideInInspector, Min(0f)] private float WalkSpeed = 2f;
        [SerializeField, HideInInspector, Min(0f)] private float RunSpeed = 6f;
        [SerializeField] private float AnimBlendSpeed = 8.9f;

        [SerializeField] private Transform CameraRoot;

        [SerializeField] private Transform Camera;

        [SerializeField] private float UpperLimit = -40f;

        [SerializeField] private float BottomLimit = 70f;

        [SerializeField] private float MouseSensitivity = 21.9f;
        [SerializeField, Range(.1f,4f)] private float mouseSensitivityMultiplier = .5f;
        public const string SensitivityPreferenceKey = "Player.Input.MouseSensitivityMultiplier.v1";
        public float MouseSensitivityMultiplier => mouseSensitivityMultiplier;
        public float EffectiveMouseSensitivity => MouseSensitivity * mouseSensitivityMultiplier;
        public void SetMouseSensitivityMultiplier(float value, bool persist = true)
        {
            if(float.IsNaN(value)||float.IsInfinity(value))return;
            mouseSensitivityMultiplier=Mathf.Clamp(value,.1f,4f);
            if(persist)PlayerPrefs.SetFloat(SensitivityPreferenceKey,mouseSensitivityMultiplier);
        }
        private void Awake()
        {
            SetMouseSensitivityMultiplier(PlayerPrefs.GetFloat(SensitivityPreferenceKey,mouseSensitivityMultiplier),false);
        }
        private void OnApplicationQuit()=>PlayerPrefs.Save();

        [Header("Jump")]
        [SerializeField, HideInInspector, Min(0f)]
        private float JumpFactor = 260f;
        [SerializeField, Min(1f), Tooltip("Extra gravity only while falling. 1 = normal gravity.")]
        private float FallGravityMultiplier = 1.4f;

        [SerializeField] private float Dis2Ground = 0.8f;

        [SerializeField] private LayerMask GroundCheck;

        [SerializeField] private float AirResistance = 0.8f;

        [SerializeField, Min(0f), Tooltip("Horizontal acceleration while jumping or falling. AirResistance scales this control strength.")]
        private float AirControlAcceleration = 20f;
        [SerializeField, Min(0f), Tooltip("Horizontal braking in the air when movement is released. 0 preserves all momentum.")]
        private float AirBraking = 1.5f;

        private Rigidbody _playerRigidbody;
        private InputManager _inputManager;

        private Animator _animator;
        private MiningSimulator.Ores.MiningPlayerStats _playerStats;
        private AnimationRunMana _runMana;
        private PlayerCoreStatsData CoreData => _playerStats != null ? _playerStats.CoreData : null;

        private bool _grounded;

        private bool _jumpHeldLastStep;
        private bool _jumpPending;
        private bool _jumpRequested;
        private int _takeOffGroundChecks;

        private bool _hasAnimator;

        private int _xVelHash;

        private int _yVelHash;

        private int _jumpHash;

        private int _groundHash;

        private int _fallingHash;

        private int _zVelHash;

        private float _xRotation;

        // The existing blend tree was authored at these values, independently of world speed.
        private const float WalkAnimationSpeed = 2f;
        private const float RunAnimationSpeed = 6f;

        private Vector2 _currentVelocity;


        private void Start()
        {
            _hasAnimator = TryGetComponent<Animator>(out _animator);
            _playerRigidbody = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();
            _playerStats = GetComponent<MiningSimulator.Ores.MiningPlayerStats>();
            _runMana = GetComponent<AnimationRunMana>();

            _xVelHash = Animator.StringToHash("X_velocity");
            _yVelHash = Animator.StringToHash("Y_velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");
            _zVelHash = Animator.StringToHash("Z_velocity");
            _inputManager.JumpPressed += OnJumpPressed;
            SampleGround();
        }

        private void OnJumpPressed()
        {
            if (isActiveAndEnabled && Time.timeScale > 0f) _jumpRequested = true;
        }

        private void Update() { HandleJump(); }

        private void OnDisable() { _jumpRequested = false; _jumpHeldLastStep = false; }
        private Vector3 knockbackVelocity;
        private float knockbackRemaining, knockbackDuration;
        public void ApplyKnockback(Vector3 velocity, float seconds)
        {
            knockbackVelocity = new Vector3(velocity.x, 0, velocity.z);
            knockbackDuration = knockbackRemaining = Mathf.Max(.01f, seconds);
            if (_playerRigidbody) _playerRigidbody.AddForce(velocity, ForceMode.VelocityChange);
        }
        public void ResetMotion()
        {
            knockbackVelocity = Vector3.zero; knockbackRemaining = 0;
            _currentVelocity = Vector2.zero;
            _jumpRequested = _jumpPending = _jumpHeldLastStep = false;
            _takeOffGroundChecks = 0;
            if (_animator) { _animator.ResetTrigger(_jumpHash); _animator.SetFloat(_xVelHash, 0f); _animator.SetFloat(_yVelHash, 0f); }
            SampleGround();
        }
        private void OnDestroy() { if (_inputManager != null) _inputManager.JumpPressed -= OnJumpPressed; }

        private void FixedUpdate()
        {
            SampleGround();
            Move();
            if (!_grounded && _playerRigidbody.useGravity && _playerRigidbody.linearVelocity.y < 0f)
                _playerRigidbody.AddForce(Physics.gravity * (Mathf.Max(1f, FallGravityMultiplier) - 1f), ForceMode.Acceleration);
        }

        private void LateUpdate()
        {
            CamMovements();
        }

        private void OnAnimatorMove()
        {
            // In-place jump/fall root motion otherwise overwrites Rigidbody horizontal velocity.
            // Keep the existing grounded animation behaviour; physics owns the whole jump.
            if (_hasAnimator && _grounded && !_jumpPending && knockbackRemaining <= 0 && _playerRigidbody.linearVelocity.y <= 0.1f)
                _animator.ApplyBuiltinRootMotion();
        }

        private void Move()
        {
            if (!_hasAnimator) return;

            var coreData = CoreData;
            float walkSpeed = coreData != null ? coreData.walkSpeed : WalkSpeed;
            float runSpeed = coreData != null ? coreData.runSpeed : RunSpeed;
            bool running = _inputManager.Run && (_runMana == null || _runMana.CanRun);
            float targetSpeed = Mathf.Max(0f, running ? runSpeed : walkSpeed);
            float animationSpeed = running ? RunAnimationSpeed : WalkAnimationSpeed;
            Vector2 movementInput = Vector2.ClampMagnitude(_inputManager.Move, 1f);

            // Keep reading movement in the air, not only while grounded.
            _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, movementInput.x * animationSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
            _currentVelocity.y = Mathf.Lerp(_currentVelocity.y, movementInput.y * animationSpeed, AnimBlendSpeed * Time.fixedDeltaTime);
            // Physics responds to input immediately; only the visual blend is smoothed.
            Vector2 desiredVelocity = movementInput * targetSpeed;
            if (knockbackRemaining > 0)
            {
                var impulse = transform.InverseTransformDirection(knockbackVelocity * (knockbackRemaining / knockbackDuration));
                desiredVelocity += new Vector2(impulse.x, impulse.z);
                knockbackRemaining = Mathf.Max(0, knockbackRemaining - Time.fixedDeltaTime);
            }

            if (_grounded)
            {
                // Input and blend-tree velocities are local to the player. Compare
                // them with local Rigidbody velocity before converting the correction.
                var localVelocity = transform.InverseTransformDirection(_playerRigidbody.linearVelocity);
                var xVelDifference = desiredVelocity.x - localVelocity.x;
                var yVelDifference = desiredVelocity.y - localVelocity.z;

                _playerRigidbody.AddForce(transform.TransformDirection(new Vector3(xVelDifference, 0f, yVelDifference)), ForceMode.VelocityChange);

            } 
            else
            {
                // Steer horizontal velocity without changing the vertical jump/fall trajectory.
                var localVelocity = transform.InverseTransformDirection(_playerRigidbody.linearVelocity);
                bool steering = movementInput.sqrMagnitude > 0.0001f;
                var airTarget = steering || knockbackRemaining > 0 ? desiredVelocity : Vector2.zero;
                var correction = new Vector3(airTarget.x - localVelocity.x, 0f, airTarget.y - localVelocity.z);
                float acceleration = steering ? Mathf.Max(0f, AirControlAcceleration) * Mathf.Max(0f, AirResistance) : Mathf.Max(0f, AirBraking);
                float maxCorrection = acceleration * Time.fixedDeltaTime;
                correction = Vector3.ClampMagnitude(correction, maxCorrection);
                _playerRigidbody.AddForce(transform.TransformDirection(correction), ForceMode.VelocityChange);
            }

            _animator.SetFloat(_xVelHash, _currentVelocity.x);
            _animator.SetFloat(_yVelHash, _currentVelocity.y);


        }

        private void CamMovements()
        {
            if (!_hasAnimator) return;

            var Mouse_x = Cursor.lockState == CursorLockMode.Locked ? _inputManager.Look.x : 0f;
            var Mouse_y = Cursor.lockState == CursorLockMode.Locked ? _inputManager.Look.y : 0f;
            Camera.position = CameraRoot.position;

            // Preserve the old 60 Hz tuning, without amplifying mouse delta on slow frames.
            float lookScale = _inputManager.LookIsMouse ? 1f / 60f : Time.deltaTime;
            _xRotation -= Mouse_y * EffectiveMouseSensitivity * lookScale;
            _xRotation = Mathf.Clamp(_xRotation, UpperLimit, BottomLimit);

            Camera.localRotation = Quaternion.Euler(_xRotation, 0, 0);
            _playerRigidbody.MoveRotation(_playerRigidbody.rotation * Quaternion.Euler(0, Mouse_x * EffectiveMouseSensitivity * lookScale, 0));
        }

        private void HandleJump()
        {
            if (!_hasAnimator) return;
            bool jumpPressed = _jumpRequested || (_inputManager.Jump && !_jumpHeldLastStep);
            _jumpRequested = false;
            _jumpHeldLastStep = _inputManager.Jump;
            if (!jumpPressed || !_grounded || _jumpPending || Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked) return;

            _jumpPending = true;
            _animator.ResetTrigger(_jumpHash);
            _animator.SetTrigger(_jumpHash);
            // Take off now. The legacy clip event becomes an idempotent no-op.
            JumpAddForce();
        }
        public void JumpAddForce()
        {
            if (!_jumpPending || _playerRigidbody == null) return;
            _jumpPending = false;
            float jumpStrength = CoreData != null ? CoreData.jumpStrength : JumpFactor;
            Vector3 velocity = _playerRigidbody.linearVelocity;
            velocity.y = Mathf.Max(0f, jumpStrength) / Mathf.Max(.001f, _playerRigidbody.mass);
            _playerRigidbody.linearVelocity = velocity;
            // Avoid re-grounding on the take-off frame.
            _takeOffGroundChecks = 2;
            _grounded = false;
            SetAnimationGrounding();
        }

        private void SampleGround()
        {
            if(!_hasAnimator) return;


            RaycastHit hitInfo;
            _animator.SetFloat(_zVelHash, _playerRigidbody.linearVelocity.y);
            bool takingOff = _takeOffGroundChecks > 0;
            if (takingOff) _takeOffGroundChecks--;
            if (!takingOff && _playerRigidbody.linearVelocity.y <= 0.1f && Physics.Raycast(_playerRigidbody.worldCenterOfMass,Vector3.down,out hitInfo, Dis2Ground + 0.1f, GroundCheck, QueryTriggerInteraction.Ignore))
            {
                _grounded = true;
                SetAnimationGrounding();
                return;

            }

            _grounded = false;
            _animator.SetFloat(_zVelHash, _playerRigidbody.linearVelocity.y);
            SetAnimationGrounding();
            return;
        }

        private void SetAnimationGrounding()
        {
            _animator.SetBool(_groundHash, _grounded);
            _animator.SetBool(_fallingHash, !_grounded);
        }


    }
}
