# Demo gun investigation and rifle preparation

User request: continue REA investigation of FPSAnimator_Playable demo guns and prepare rifle while preserving their pistol and authored Animator.

## Evidence boundaries

Original Downloads RAR is no longer present. Investigation reused its previously extracted, user-selected build outside Unity Assets. GameAssembly.dll identity reverified through REA inspect_managed_artifact: SHA256 ae55260625efb902188e408bf307ac7d92af1eb943647c20047ffcf2398db53e; Evidence ev_a8bc4534e9a3914ef8efecbff8f7f3796cb121b10adab02668e9f5d784b6e839. No CLI directory/CIL: native IL2CPP analysis, not a recovered C# source project. A focused native search is separate from serialized-asset inventory. No demo executable launched, no demo meshes/clips imported.

REA opened the native DLL with Ghidra, then the new focused search for AK12/Mk18/Mk23/MaskAttachHand/WeaponBone/OnReload hit the MCP client's 300-second tools/call deadline. No native search results or method semantics were recovered in this pass. Do not interpret this timeout as evidence that names/functions are absent. close_binary completed successfully afterward; preserve the separate serialized-file findings and their limitations.

## New serialized-file observations

Read-only parser AgentPreviews/inspect_demo_unity_assets.py inventories class IDs, name fields and Transform data with original object offsets/path IDs and file hashes. It validates declared file size, type IDs and object bounds. Report: AgentPreviews/DemoGunSerializedInventory.json in calling workspace. No assets exported. This parser is independent local artifact inspection, NOT output from REA's native decompiler. Type trees are stripped; script fields and animation curves are not reconstructed.

- sharedassets0.assets: 687 serialized objects; name decoding errors 0.
- resources.assets: 1384 objects; name decoding errors 0.
- level0: 631 objects; name decoding errors 0.
- Weapon-related objects/profiles include AK12, Mk18, FAL, Mk23Mod0 and their AnimatorProfile/overlay assets.
- Separate C_AK12_ReloadEmpty, W_AK12_ReloadEmpty and A_Cam_AK12_Reload objects; similar character/weapon reload pairs for FAL and Mk23, plus Mk18 character/weapon/camera reload assets.
- AK12/Mk18 have angled/vertical/no-grip variants. AimPoint, bolt, magazine and WeaponBone/IK WeaponBone targets are present.

Observation: the build carries weapon-specific poses/profiles and separate character/mechanism/camera action assets. Inference for reconstruction: a replacement gun needs its own grip, sight/pivot/muzzle/support-hand targets, mechanism paths, recoil and action configuration. Object names alone do NOT prove runtime selection, curve timing, tuning, original commercial pack provenance or licensing. Do not claim these named assets identify a purchasable source pack.

Primary-source cross-check, independent of this build's version: publisher describes pose sampling -> procedural layers -> final IK, with per-weapon profiles. https://kinemation.gitbook.io/scriptable-animation-system/workflow/profiles-and-layers . Publisher's public demo README still requires the FPS Animation Framework package: https://github.com/kinemation/scriptable-animation-system . No commercial implementation or animation curves copied.

## Existing project rifle

Assets/ChestDefense/FPS/Models/Rifle_FPS.fbx contains AKM_model, ArmModel and a separate Generic armature (UpperArm.L/Hand.L and UpperArm.R.001/Hand.R.001). Bolt/Magazine/Trigger under RootNode/Armature/Root. Idle, Shoot and Reload clips report isHumanMotion=false. This is not a drop-in Humanoid player reload/locomotion pack; do not play it directly over the user's Animator. Existing source credit/assets remain unchanged.

Prepared Assets/GameData/Player/Weapons/Rifle/RifleWeaponData.asset and RifleSetupProfile.asset; profile class WeaponSetupProfile is authoring-only and explicitly reports missing gun-only prefab, player grip and Generic-hand conversion. Data has editable prototype values (automatic, .12s cooldown, 30 magazine, 120 reserve, 2.6s reload); not enabled or balanced in gameplay. Existing source clip and mechanism references recorded; no automatic rig conversion claimed.

Validation: Unity compilation completed without errors. Assets created successfully, issue-list check returns the three expected readiness blockers. Re-read confirmed active weapon still Weapons/PistolWeaponData.asset and controller still Animations/PlayerController.controller; Editor remains out of Play Mode. This is preparation/configuration validation, not a rifle playtest.

Next implementation: gun-only prefab and calibrated markers, player rifle grip/Humanoid locomotion, explicit hand/mechanism reload retarget, then a shared equip path preserving per-weapon ammo/profile and cancelling actions on switch. Keep PlayerController1, InputManager and user's PlayerController.controller. Active pistol, hand pose, player save and scene are unchanged by this preparation.

Additional preparation completed in an isolated Preview Scene: RifleGunOnly_Prototype.prefab created from existing Rifle_FPS.fbx, separate ArmModel mesh removed from this new copy, source Animator omitted, existing material replacements assigned where available. Only AKM_model renderer remains; original magazine and bolt paths verified. Original imported source untouched. Profile now references this prefab. Remaining readiness blockers are the player's rifle grip, Generic-hand conversion and uncalibrated sight/muzzle/support-hand markers. Prefab validated structurally, not enabled/playtested in the player. Marker coordinates are deliberately unset rather than pretending the pistol pose automatically fits a rifle.
