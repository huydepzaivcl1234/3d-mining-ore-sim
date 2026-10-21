# Pistol reload integration — 2026-10-14

## Follow-up: magazine ejection, empty lock-back and hip recoil

The follow-up supersedes the original continuous-magazine motion: an old magazine leaves the well and falls independently; the original magazine renderers are hidden while the support hand moves to a pickup point, then reused as the fresh magazine. One cached detached visual is reused for the old magazine, with deterministic gravity motion and a short lifetime (no floor collision/debris persistence). No per-reload Instantiate/Destroy.

New profile controls: ejectTime, newMagazineTime, insertTime, slideReleaseTime (normalized action time), freshMagazineOffset, pickupHandOffset and droppedMagazineLifetime. Weapon pose/tilt still comes from the admitted target clip. Magazine position is now phase-driven; old MagazineOffset position curves do not set its travel. Rotation still uses sampled clip motion blended back to the seated rotation. Fingers keep the saved grasp pose; this is not a claim of a fully authored replacement for the commercial demo.

The slide stays rearward at zero ammo, including before pressing R. Empty reload releases it after insertion; partial reload skips this lock-back sequence. Camera recoil is a reversible additive presentation offset, not an edit to the movement/input controller. New weapon data fields cameraRecoilDegrees and cameraReturnSeconds tune the response in hip and ADS (aimRecoilMultiplier applies). Hip kick now moves inward after the resting IK reach solve, so reach clamping no longer erases most of the stroke.

Live test initially reproduced the hip issue: requested spring stroke 24.9 mm, actual weapon movement only 4.1 mm. After the targeted IK correction, actual weapon movement was 24.8 mm hip and 14.1 mm ADS; measured camera rotation peaked at 0.897 and 0.584 degrees respectively. Ejection/falling, hidden well, fresh magazine grip, empty slide hold/close, blocked repeated R/fire and ammo transfer passed. All seven reload edge cases passed again. These are live temporary helper checks, not NUnit test results. The user's current magazine size of seven was retained.

SampleScene had already changed on disk since the earlier turn; current-turn baseline hash was 46BCC1E91A188DD727C3878083012ABED8CEA71EB16DBF5F5DD0208F6454C938. Do not interpret the earlier report's scene hash as the baseline for this follow-up. No scene save was requested by the implementation.

## Implementation

R plays the existing admitted `Assets/ChestDefense/FPS/Generated/Pistol_Reload.anim` (2.041667 seconds), adapted to the current M1911 and PlayerArmature. No proprietary demo clips or code were imported. Existing source-pack credit remains in `Assets/ChestDefense/FPS/Models/Credits.txt` (J-Toastie, CC BY 3.0).

The baked generic target clip drives weapon and magazine offsets. Existing player IK moves the right hand with the weapon and releases the left shared grip to follow the magazine. The left finger grasp centroid is aligned to the magazine mesh center. The calibrated idle finger pose is retained; this is not newly articulated finger animation.

Empty reload also racks the slide; partial reload does not. Firing and repeated reload requests are blocked during reload. Ammo transfers once at completion. Holster, presentation disable, or dead health cancel before completion without spending reserve ammo. Time-scale pause freezes progress. ADS is suppressed during reload and can resume while RMB remains held.

## Editable assets

- `Assets/GameData/Player/Weapons/PistolWeaponData.asset`: reload duration and reload profile reference.
- `Assets/GameData/Player/Weapons/PistolReloadProfile.asset`: weapon raise/tilt, left grip offset/rotation, magazine and slide paths, sound/volume.
- `Assets/GameData/Player/Weapons/PistolReloadTargets.anim`: editable generic animation curves.
- `Assets/GameData/Player/Weapons/PistolReloadAuthoring.prefab`: generic control transforms and authoring Animator for editing the target clip in the Animation window. This is separate from the player's Animator.

The menu `Chest Defense/Player/Bake Pistol Reload Targets` regenerates the target clip from the source. Back up manual curve edits before rebaking: regeneration replaces the target curves and rebuilds the authoring prefab. It does not modify the saved idle hand pose.

## Live validation

Executed temporary test helpers outside Assets through the running Unity Editor; these are live behavioral checks, not a claim of an NUnit suite.

- Actual R input after firing: animation ran, repeated R and firing were rejected, ammo transferred once, mechanism restored.
- Seven-case sequential suite passed: empty reload, holster cancel, presentation disable, pause, limited reserve, zero reserve rejection, full magazine rejection.
- Empty reload: maximum magazine travel approximately 0.215 m, slide travel 0.035 m. Maximum measured grip/grasp alignment error below 0.001 m.
- Actual R while running diagonally, looking, holding RMB and jumping passed; measured movement 8.475 m and jump rise 1.474 m in the test interval.
- Post-reload firing regression passed: three shots/three muzzle flashes, slide restored, recoil settled, feedback object reused.
- Final Console interval after cursor 206028 contained no new error entries; compilation was successful. Earlier interrupted Play/recompile logged unrelated TowerBuildHud binding and CoinRainEventSystem rainParticles errors; those systems were not changed here.

Screenshots and test helpers are in the calling workspace's `AgentPreviews` folder, including `PistolReloadStage1.png`, `PistolReloadStage2.png`, and `PistolReloadStage3.png`.

## Preservation and limitations

The file hashes of PlayerController1.cs, InputManager.cs, PistolHandPose.asset and SampleScene.unity were verified unchanged. No scene save or player Animator replacement was performed. Unity was returned to Edit Mode.

Death cancellation is implemented but an actual death/respawn scenario was not tested in this pass. Numeric grip alignment is not a full mesh-intersection test. Reload sound uses PlayOneShot and may finish briefly after cancellation. Cancellation restores the mechanism without a separately authored cancel animation. Exact visual parity or superiority to the commercial demo is not claimed.
