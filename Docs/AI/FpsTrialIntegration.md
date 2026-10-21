# Reversible FPS trial

The supplied ChestDefense_FPS_Weapon_System.zip was reviewed and integrated into the connected D:/3d mining sim checkout. Shared orbit-camera, keybind, interaction and localization changes were merged, not replaced by ZIP versions. Existing shift-lock fixes and Build keybind remain.

## Try it
The open scene's Player is wired in memory, without saving SampleScene. Play and press **F8** to enter or leave FPS. Left mouse fires, right mouse aims, R reloads, Q cycles weapons, 1/2 select Glock/AKM. Hold Alt to release the mouse for UI. The player's existing character, melee animations, sword mounts, stats and progression remain; no soldier/NPC conversion has been done.

If scene setup was discarded, select Player then use **Chest Defense > FPS > 3. Prepare reversible F8 trial on selected player**. This has Undo and does not save the scene automatically. Trial ammo is session-only (`saveAmmo=false`). Weapon values and gun poses are editable in Assets/ChestDefense/FPS/Generated/Pistol.asset and Rifle.asset; F8's InputAction is editable on FpsTrialToggle.

## Package repairs and tests
- Fixed the missing Input System import that prevented the supplied HUD from compiling.
- Corrected the supplied FBXs' 100x import scale mismatch, detected from actual Play capture and renderer bounds. Source geometry/animations are preserved; the model importer scale is 0.01.
- Added reversible F8 trial and safe isolated Play checks.
- English/Vietnamese FPS strings added without replacing existing localization.
- Editor checks: six magazine/rate/reload/cancel/pickup cases plus generated gun references pass.
- Isolated Play with an actual player clone and temporary save: actual Glock damage, blocked shot at wall, AKM equip, reload cancellation, camera/FOV and melee restoration, death-camera release pass. It does not prove physical mouse feel, final viewmodel framing, balance, or all original menu/ragdoll paths.
- Test cleanup removes the temporary scene/start override and returns to SampleScene. No SampleScene file is in the delivery ZIP.

## Asset admission exception
The user's explicit approval permits importing this ZIP without the canonical game-dev receipt. game-dev was not available in this session; no canonical admission/receipt verification is claimed. ZIP SHA256: 1E2CCD91E769C15DBB3AB33197A7A23129D55175F02D3C5227A9280189FC149B.
Supplied provenance attributes models and animations to **J-Toastie**, **CC BY 3.0**, Glock https://poly.pizza/m/uxko5LkGia and AKM https://poly.pizza/m/U6l6wjxFhC. Retain Assets/ChestDefense/FPS/Models/Credits.txt in distribution and game credits. Supplied audio is described by the package as synthesized placeholder SFX. This records package attribution, not an independent ownership audit.

## FPS bug repair, 2026-10-12

- Console confirmed `UnassignedReferenceException` at optional `muzzleFlash?.Play`. Unity serialized missing objects require `!= null`; a missing flash no longer aborts `PlayShot` before damage. The same guard is used for optional reload audio and camera/audio owners.
- Actual Player/Look bindings contain `InvertVector2(invertX=false),ScaleVector2(x=0.05,y=0.05)`. FPS was inverting again and multiplying this scaled input by another 0.1. FPS now reads raw mouse delta, reuses the orbit's degrees-per-pixel and saved sensitivity, and reads raw gamepad stick with delta-time scaling. No shared bindings or saved sensitivity were overwritten.
- Both gun prefabs hide `ArmModel` only. Bone hierarchies and Idle/Shoot/Reload clips are retained. `FpsGunView.showHands` is false by default, with explicit `handRenderers` references. The player character itself has not been replaced.
- Glock and AKM face opposite source X directions. Glock's mount is corrected to face forward and counter its idle roll; its muzzle follows the animated `Barrel_end` bone. AKM's hip pose is offset right. Hip/ADS poses stay editable in their generated weapon data assets.
- Regression now queues virtual mouse delta and button events through actual Update/LateUpdate, rather than calling the private damage method. It tests direction/sensitivity, ammo consumption plus target damage, wall occlusion, automatic fire, input lock/unlock, reload cancellation, F8 rollback and death camera release. It uses an isolated player clone with the actual orbit configuration and temporary save. This is not a physical-mouse user playthrough of every menu in SampleScene.
- Captures: `FpsTrialPistol.png`, `FpsTrialRifle.png`, and corresponding `ADS` images. These are actual Play camera captures, not mockups.
- The temporary check scene and Play Mode Start Scene override are removed on exit. Generated gun prefabs/data are saved; SampleScene is not saved automatically.

## Shot / reload / HUD follow-up, 2026-10-12

The previous idle-only framing checks missed action-clip reference-frame defects. Source sampling established that AKM Shoot/Reload begin approximately 180 degrees away from Idle; Glock Reload puts the weapon roughly 118 m from the rig origin. Neither is a camera sensitivity issue.

- `FpsGunAnimationRepair` creates four editable `.anim` copies in `Generated`, normalizes the weapon root's action reference frame against Idle, and holds its ancestor pose stable for this gun-only trial. It preserves the magazine/slide/bolt tracks. The FBX takes are not overwritten. The supplied poses and muzzle mounts are aligned using the visible barrel axes; AKM's muzzle now follows its animated weapon root.
- The supplied skinned renderer bounds followed `UpperArm.R.001`. Its hidden reload movement moved the culling bounds behind the camera even while the weapon root/muzzle were correct. Both viewmodels now use the stable rig root and dynamic skin bounds. This applies only to the two first-person models, not map/monster culling.
- The playable graph has one explicit clock and evaluates after pose/action weights change. Shooting starts immediately rather than losing most of AKM's short 0.167-second clip to its fade-in. Reload scales its original clip to the authored reload duration.
- Ammo card is above the original player HP card, with larger weapon/ammo text. A production font-material A/B capture showed the project's standard Mobile SDF path drawing no FPS glyphs while SSD rendered the same font/atlas/geometry. `Resources/FpsHudText.mat` uses the existing TMP SSD shader and Nunito atlas without modifying shared menu font materials. Runtime visibility refresh also covers HUD activation before weapon initialization.

### Editable settings

Open `Assets/ChestDefense/FPS/Generated/Pistol.asset` or `Rifle.asset` in Inspector. Damage, range, spread, magazine/reserve capacity, reload duration, hip/ADS pose, ADS FOV, recoil and SFX remain per-weapon settings.

`Minimum Shot Interval` is the minimum seconds between accepted shots, including after player attack-speed bonuses. The effective interval is `max(Minimum Shot Interval, 1 / (Rounds Per Second * attack-speed ratio))`. Defaults are Glock **0.35 s**, AKM **0.125 s**. Zero disables the additional floor. Glock stays Semi Automatic (one new click per shot); AKM stays Automatic. Disable `Use Player Attack Speed` for a fixed gun rate.

On `FPS HUD`, `Ammo Card Offset/Size` and three font sizes are editable. `Apply Ammo Layout=false` preserves a manually authored layout. The shader material is FPS-only.

The repair menu is an explicit migration for these supplied models: it resets their default alignment/cooldown, so do not rerun it after custom pose tuning unless you want those defaults. Ordinary player setup does not overwrite existing weapon data.

### Verification scope

Pure magazine checks include 100 rapid-fire attempts and attack-speed ratios of 100, verifying the cooldown floor. The isolated Play regression uses actual virtual mouse/keyboard Input System events: damage/ammo, wall occlusion, modal lock, automatic-fire barrel direction, R reload for BOTH guns, magazine movement measured in rig coordinates (not camera motion), single reload commit, HUD text/layout, F8 restoration and death-camera release. Captures include Shoot, both Reloads and ADS. The test uses a cloned player and temporary save, cleans up its temporary scene/start override, and does not save SampleScene.

This validates the connected Editor and supplied configurations, not every physical-input device, every menu, or every user-authored future weapon/clip. Camera captures are real Play renders, not concept images.
