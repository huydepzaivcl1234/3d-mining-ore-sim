# Imported FPS Shooter Aiming integration — 2026-10-14

## Scope

The imported `Assets/Kinemation/SightsAligner` runtime contains aiming helpers and two-bone IK, not firing/reload clips. Package source is unchanged. `PistolPresentation` remains the sole hand-pose owner: `PistolAimingRig` calls the imported `AnimToolkitLib.SolveTwoBoneIK`. Do not also enable `CoreAnimComponent` on this player; two LateUpdate owners would overwrite the same bones.

The existing admitted pistol reload clip and saved grip remain in use. ADS orientation also respects the sight marker's local rotation. The existing analytical arm solver remains available by disabling Use Imported Aiming IK.

## Editable settings

- `Assets/GameData/Player/Weapons/PistolWeaponData.asset`: Recoil Yaw Degrees and Recoil Roll Degrees add a small alternating lateral/roll impulse to the existing rearward hand spring and camera recoil. Existing ADS recoil multiplier applies.
- `Assets/GameData/Player/Weapons/PistolReloadProfile.asset`: Magazine Insertion Arc is a weapon-local offset in meters, fading to zero at both endpoints. Zero restores the straight path.
- Existing scene grip/ADS authoring controls remain available. No movement/input controller, saved grip, Animator controller, or SampleScene save was changed in this integration.

## Live Editor validation

Unity 6000.5.3f1, Play Mode, using the actual player and gun:

- Compilation completed without errors; no new Console errors after baseline cursor 1733. Final sampled Console reported zero errors/warnings.
- All seven reload cases passed: empty magazine, holster cancellation, component-disable cancellation, pause/resume, limited reserve, zero reserve, full magazine.
- Actual R input with diagonal movement, running, jumping, camera look and RMB passed. Player moved 6.79 m and jumped 1.52 m during the test. Maximum measured hand/grip errors: 0.00000271 m / 0.000216 m.
- Hip and ADS firing feedback passed: measured backward spring peaks 0.02436 m / 0.01541 m, camera recoil 0.893 / 0.584 degrees. Independent old-magazine fall, original-magazine hiding, fresh insertion, empty-slide hold, slide release and fire blocking during reload passed.
- Actual RMB input activated ADS and hid the crosshair.
- Inspected in-game reload captures at the fresh-magazine and closing stages, with the camera moved away from the chest for visibility.
- SHA-256 checks confirmed SampleScene, PistolHandPose, PlayerController1, InputManager and imported AnimToolkitLib were unchanged.

## Limits

This improves the existing procedural/clip hybrid; it does not add a new mocap reload or claim parity with a commercial animation framework. Finger grip is still the saved pose, not newly authored per-finger reload motion. Dropped magazines use the existing brief simulated fall, not floor collision. Tests validate this player's current pistol rig; different skeletons/weapon grips need their own calibration.
