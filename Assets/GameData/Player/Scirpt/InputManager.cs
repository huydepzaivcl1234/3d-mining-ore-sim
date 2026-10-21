using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace GameData.Player.Manager
{
    public class InputManager : MonoBehaviour
    {


        [SerializeField] private PlayerInput PlayerInput;

        public Vector2 Move { get; private set; }

        public Vector2 Look { get; private set; }
        // Pointer delta is already displacement this frame; stick input is a rate.
        public bool LookIsMouse => _lookAction?.activeControl == null || _lookAction.activeControl.device is Mouse;

        public bool Run { get; private set; }

        public bool Jump { get; private set; }
        public event System.Action JumpPressed;

        private InputActionMap _currentMap;

        private InputAction _moveAction;

        private InputAction _lookAction;

        private InputAction _runActiion;

        private InputAction _jumpAction;

        private void Awake()
        {

            HideCusor();
            _currentMap = PlayerInput.currentActionMap;
            _moveAction = _currentMap.FindAction("Move");
            _lookAction = _currentMap.FindAction("Look");
            _runActiion = _currentMap.FindAction("Run");
            _jumpAction = _currentMap.FindAction("Jump");

            _moveAction.performed += OnMove;
            _lookAction.performed += OnLook;
            _runActiion.performed += OnRun;
            _jumpAction.performed += OnJump;

            _moveAction.canceled += OnMove;
            _lookAction.canceled += OnLook;
            _runActiion.canceled += OnRun;
            _jumpAction.canceled += OnJump;
        }

        private void HideCusor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            Look = context.ReadValue<Vector2>();
        }

        private void OnRun(InputAction.CallbackContext context)
        {
            Run = context.ReadValueAsButton();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            
               Jump = context.ReadValueAsButton();
               if (context.performed && Jump) JumpPressed?.Invoke();
        }

        private void OnEnable()
        {
            _currentMap.Enable();
        }

        private void OnDisable()
        {
            _currentMap.Disable();
        }
    }
}
