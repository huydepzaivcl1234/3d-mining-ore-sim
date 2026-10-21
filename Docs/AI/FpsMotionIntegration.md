# Independent FPS motion integration — 2026-10-14

## Scope and evidence

Implemented a first independent procedural presentation layer, not a clone of the complete paid FPS Animation Framework. The supplied IL2CPP DLL was classified and inventoried previously (REA evidence `ev_0d7190a5459fec2fd8fcc23a6929d53211b8851410a744cbe190ddc68918efcd`); no FPS method bodies or exact demo tuning have been recovered. No demo models, animations or proprietary code were imported.

Publisher documentation supports pose sampling, procedural target modification, then final IK:

- https://kinemation.gitbook.io/scriptable-animation-system/workflow/profiles-and-layers
- https://kinemation.gitbook.io/scriptable-animation-system/fundamentals/animator-layers/sway-layer
- https://kinemation.gitbook.io/scriptable-animation-system/fundamentals/animator-layers/ads-layer

Our implementation is a design choice informed by those documents. It reads actual camera angular velocity rather than modifying the user's input. Spring mathematics and defaults were authored independently.

## Implemented

- `FpsWeaponMotionProfile`: configurable look sway, movement sway, step bob, ADS reduction and response time. Uses existing animator velocity units (walk 2, run 6), not player world speeds.
- `FpsWeaponMotion`: bounded, analytic critically damped target response. Resets on large frame gaps; handles camera angle wrapping; no transform accumulation, input polling or per-frame collections.
- `PistolWeaponData.motionProfile`: assigned the new `Weapons/PistolMotionProfile.asset`. Null retains legacy presentation/bob.
- `PistolPresentation`: applies common motion to the weapon/hand target before existing two-arm IK and accepted-shot feedback. Editing hip/ADS poses in Scene disables procedural sway for precise authoring. Holster, disable and released cursor reset motion.
- The existing recoil/slide/muzzle feedback remains separate and unchanged.

Use Q to draw, RMB to aim, Esc to release the cursor. Edit `Assets/GameData/Player/Weapons/PistolMotionProfile.asset` to tune motion; set ADS Motion Multiplier to 0 for no procedural sway once ADS settles. Position settings are meters; rotation settings are degrees. Existing scene gizmos and saved hand-pose workflow remain available.

## Validation and limitations

Unity 6000.5.3f1 compiled the new code. Seven solver checks passed: 30/60/120 Hz constant-target spring agreement (worst error 1.33e-7), bounded motion, ADS scaling, zero-dt pause, idle settling, large-frame reset and angle wrapping. These are mathematical checks, not a claim of visual parity or full runtime frame-rate equivalence.

Play Mode exercised the actual existing controller via temporary input, then removed the test mouse and restored input values: walk, diagonal run, camera turn, RMB ADS, three shots, jump and cursor release passed. Recorded grip error 7.06e-7 m, peak motion .00834 m / 1.01 degrees, jump vertical range 1.57 m; released-cursor motion returned to zero. Hip/ADS screenshots were captured and visually inspected in workspace AgentPreviews.

First recoil regression counted hand-grip error during the draw blend, where the support hand is still joining the grip; that test failed (.137 m). A rerun after PoseWeight=1 passed: three accepted shots and flashes, .02492 m kickback, .03498 m slide travel, 3.40e-7 m grip error, slide closed, kickback settled and flash instance reused. This does not claim the draw transition maintains a closed grip throughout.

Console inspection identified one tooling timeout during AssetDatabase.Refresh (5-second Pipeline dispatcher deadline), not a compile or gameplay exception. Compilation subsequently completed; no new runtime errors were captured during Play Mode. Scene remained dirty in Edit Mode.

Reload hand release/rejoin, weapon switching action clips, and exact demo parity are NOT implemented by this step. No suitable reload clip was added or extracted. Existing timed ammo reload is unchanged.

## Data safety

Saved only the new motion profile and targeted PistolWeaponData asset. Did not save SampleScene or alter PlayerController1, InputManager, the Animator/controller, or PistolHandPose asset. File SHA-256 remained unchanged:

- PistolHandPose.asset: `7C9C6A87A6B9B8AD299EE918560B19FFC36B4CBAA18E3A811BC7A9C631BC57DC`
- PlayerController1.cs: `259F80683A12431519514C1B9CB714B23557E5F8E95186D68D711F15DEDA3DF0`
- InputManager.cs: `ADB7A8B604F50421F9E8B8938FC2E9396ADFA3B876F82059B4353F4241BC3B52`
- SampleScene.unity: `0EC4AADAF17F1D537577B74F89FA991005FDE3E9409B8C79916A084B7F5833C9`

Scoped pre-edit copies are in the calling workspace `AgentPreviews/FPSMotionBaseline-20261013`. Do not broadly reset the dirty project to roll this change back.
