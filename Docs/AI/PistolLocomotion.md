# Pistol locomotion and airborne running stamina

## Current binding - user's authored Animator restored

The user subsequently authored their own Pistol Presentation Idle state (9-child directional Blend Tree) and Pistol Jump inside `Assets/GameData/Player/Animations/PlayerController.controller`, and explicitly requested that version. PlayerArmature's empty controller slot was reconnected to this exact original asset. The generated PlayerController_Pistol is no longer assigned. Their clips, blend positions, full-body layer without mask, base states and return transition were preserved. Only pistol Idle -> Jump hasExitTime was disabled to let the existing Jump trigger enter immediately. Stamina clip references now use their forward/backward pistol runs; shared side clips use the existing velocity classification.

10 live scenarios passed again; observed active layer clips include piston walk, Pistol Run 1, Pistol Strafe Left/Right, Pistol Run Backward and Pistol Jump. Run+jump lost about 9.5 stamina in the air; walk+jump lost 0. Scene stays dirty for Ctrl+S. Before/after scene copies: AgentPreviews/BeforeUserPistolAnimator.unity and AfterUserPistolAnimator.unity. Controller backup: PistolLocomotion/UserAnimatorBeforeRebind.controller. Sections below record the earlier implementation and tests, not the current controller binding.

## Integration

- User-authorized animation import from `Pistol_Handgun Locomotion Pack.zip`: 20 animation FBXs under `Assets/GameData/Player/Animations/PistolLocomotion`. Vanguard reference model excluded. Each clip validated as Humanoid; root rotation/XZ/Y baked into pose and old source events removed, so existing physics owns movement and jumping.
- Canonical game-dev CLI unavailable; explicit user exception accepted for missing license/receipt. Binary staging and SHA256 copy checks recorded in `PistolLocomotion/ImportReceipt.json`, with original archive digest and per-FBX hashes. No claim of independent license verification.
- `PlayerController_Pistol.controller` is a copy of the user's current `PlayerController.controller`. Original unarmed blend tree, parameters and Jump/Fall/Landing transitions preserved. New 1D PistolLocomotion selector wraps base locomotion and jump. Q draw/holster feeds selector through existing presentation blend.
- Pistol directional tree uses original X_velocity/Y_velocity and thresholds 2/6. Includes idle, forward/backward walk/run and left/right strafe. Strafe directions measured from source root motion before restoring in-place settings. Shared strafe clips play at .8 walk and 1.3 run speed. Arc clips and kneeling clips imported but not bound to new actions; no crouch input/controller introduced.
- Pistol Presentation layer now uses an arms/fingers mask instead of overwriting the entire body with Idle. Existing upper-body idle clip, gun offsets, saved hand pose, input and movement controller retained.
- AnimationRunMana reads current running clips plus existing locomotion Animator parameters. Run-range motion continues consuming stamina through Jump/Fall, including pistol strafe. Walk/jump and stationary idle do not incur the running cost. Disabled/dead motor is excluded; recovery/cost still come from PlayerCoreStatsData.
- During reload, common weapon pose is projected into both arms' reach, maintaining hand-magazine contact while locomotion leans the shoulders. No independent wrist stretch or modified saved pose.

## Live validation

Unity 6000.5.3f1 at D:/3d mining sim; isolated GameSaveStore fixtures, real player save untouched.

- 10-scenario test passed: Q draw/holster, forward/left walk, forward/right/backward run, armed run+jump, walk+jump and unarmed run+jump. Actual InputManager values drove the existing physics and Animator; Q/R presentation paths exercised with temporary Input System devices. Frames advanced and selected clip names were recorded.
- Armed run+jump consumed approximately 9.6 stamina while airborne; unarmed approximately 9.4. Walk+jump consumed 0 while airborne. Ground walk also consumed 0.
- 3-shot regression passed: recoil returns, slide closes, pooled muzzle flash emits, hand grip retained.
- R reload with diagonal run, camera look, RMB and jump passed after reach correction: moved 7.64m, jumped 1.47m, wrist-target error .000034m, magazine grasp error .000855m, magazine 7 / reserve 18 from fixture 5 / 20.
- Game View captured and visually inspected at `Assets/Docs/Validation/PistolLocomotion_Hip.png`.
- Final replay of all 10 scenarios passed with 1438 advancing frames. One Pipeline main-thread timeout occurred while preparing the re-entered Play session; subsequent commands recovered and gameplay tests completed. Console retained this tooling timeout; no compilation failure or gameplay exception was observed. Missing scripts: 0.

Scene modifications remain dirty for user's Ctrl+S; no overwrite of SampleScene. Before/after copies in calling workspace AgentPreviews/BeforePistolLocomotion.unity and AfterPistolLocomotion.unity. Animator tuning is in the generated controller; movement/stamina tuning remains in Player Core Data.
