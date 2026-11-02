using UnityEngine;
using UnityEngine.InputSystem;

namespace GameData.Player.Manager
{
    [DefaultExecutionOrder(-100)]
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private PlayerInput PlayerInput;
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool LookIsMouse => _lookAction?.activeControl == null || _lookAction.activeControl.device is Mouse;
        public bool Run { get; private set; }
        public bool Jump { get; private set; }
        public event System.Action JumpPressed;
        [System.NonSerialized] private InputActionMap _currentMap;
        [System.NonSerialized] private InputAction _moveAction, _lookAction, _runAction, _jumpAction;

        private void Start()
        {
            bool topDown=GetComponent<GameData.Player.Script.TopDownSurvivor>()!=null;
            Cursor.lockState=topDown?CursorLockMode.None:CursorLockMode.Locked;
            Cursor.visible=topDown;
        }
        private void Update()
        {
            if(!PlayerInput)PlayerInput=GetComponent<PlayerInput>();
            var map=PlayerInput&&PlayerInput.inputIsActive?PlayerInput.currentActionMap:null;
            // PlayerInput owns the map lifetime; cached maps become stale after a script reload.
            if(map!=_currentMap){
                _currentMap=map;
                _moveAction=map?.FindAction("Move");_lookAction=map?.FindAction("Look");
                _runAction=map?.FindAction("Run");_jumpAction=map?.FindAction("Jump");
            }
            if(map==null||!map.enabled){Move=Look=Vector2.zero;Run=Jump=false;return;}
            Move=_moveAction?.ReadValue<Vector2>()??Vector2.zero;
            Look=_lookAction?.ReadValue<Vector2>()??Vector2.zero;
            Run=_runAction?.IsPressed()??false;Jump=_jumpAction?.IsPressed()??false;
            if(_jumpAction?.WasPressedThisFrame()==true)JumpPressed?.Invoke();
        }
        private void OnEnable(){_currentMap=null;_moveAction=_lookAction=_runAction=_jumpAction=null;}
        private void OnDisable(){OnEnable();Move=Look=Vector2.zero;Run=Jump=false;}
    }
}

