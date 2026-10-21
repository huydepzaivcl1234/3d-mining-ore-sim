using UnityEngine;
using UnityEngine.InputSystem;

namespace ChestDefense.Fps
{
    // Opt-in trial only. Existing melee player and all authored animations stay intact.
    [DefaultExecutionOrder(250), DisallowMultipleComponent]
    public sealed class FpsTrialToggle : MonoBehaviour
    {
        public FpsWeaponController weapons;
        public FpsPlayerCamera cameraRig;
        public InputAction toggle = new InputAction("ToggleFpsTrial", InputActionType.Button, "<Keyboard>/f8");
        public bool Active => weapons != null && weapons.enabled;
        void OnEnable() => toggle.Enable();
        void OnDisable() { toggle.Disable(); SetActive(false); }
        void OnDestroy() => toggle.Dispose();
        void Update()
        {
            if (Time.timeScale > 0 && Application.isFocused && toggle.WasPressedThisFrame()) SetActive(!Active);
        }
        public void SetActive(bool active)
        {
            if (weapons == null || cameraRig == null) return;
            if (!active) { cameraRig.enabled = false; weapons.enabled = false; }
            else { cameraRig.enabled = true; weapons.enabled = true; }
        }
    }
}
