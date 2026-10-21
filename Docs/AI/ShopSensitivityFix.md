# Shop startup and mouse settings regression — 2026-10-14

Confirmed root cause: the currency restoration helper incorrectly assigned the modal Shop Panel to MiningUiPanelCoordinator.shopPanel, a legacy field representing always-visible base HUD. Coordinator enabled it on startup and faded it as HUD during modal transitions. Rebound that field to Currency HUD, retained modal ownership on MiningShopPanel.panelRoot, and clarified the field tooltip. Corrected the restoration helper too. No shop assets or unrelated UI were removed.

Settings sensitivity had been disabled in Awake and its handler only changed a text label. MiningAudioSettingsPanel now binds the existing slider to PlayerController1.SetMouseSensitivityMultiplier, refreshes from the actual controller at startup/open, and enables the slider when a controller is available. Range is 0.1–4x. New default is 0.5x: effective coefficient 10.95 instead of 21.9; original Inspector base value is preserved. Preference key is Player.Input.MouseSensitivityMultiplier.v1. Mouse delta remains frame-rate-independent and stick input remains time-scaled.

Validation in live Play Mode:

- Shop stayed closed at startup and passed three authored open/X-close button cycles.
- Slider interactable and connected to actual camera: 0.25x produced 0.09124756 degrees for the test input, 1x produced 0.364990234 degrees (4x ratio).
- Saving and reloading preference through controller initialization passed. Temporary test preference values were restored afterwards.
- Initial test's Quaternion.Angle rounded the small low-sensitivity angle to zero; Euler-angle sampling corrected that measurement. No gameplay change was needed for that test issue.
- Compilation completed without errors; no new errors during validation.
- Gameplay save was isolated for tests, retained through Play exit and then released.

SampleScene remains edited but unsaved; no overwrite. Recovery copy: C:/Users/huyancut/Documents/ChatGPT/mining simulator 3d/AgentPreviews/AfterShopSensitivityFix.unity. Review and Ctrl+S to persist the scene wiring.
