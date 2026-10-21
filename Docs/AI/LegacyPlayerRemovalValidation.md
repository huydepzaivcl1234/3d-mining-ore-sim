# Legacy player controls retired — 2026-10-12

Connected checkout: `D:/3d mining sim`, Unity 6000.5.3f1.

## Scope and recovery

Retired 57 C# files (old melee/combo, FPS trial, Starter Assets movement/input, old orbit camera, ragdoll/death/respawn, and their obsolete validation scripts). Four old FPS generated data/prefab assets were also retired. Original gun models, animation clips, player mesh/avatar, and the user's controller/input assets are preserved.

Nothing was permanently erased. Original files, metadata and four edited prefabs are backed up under `LegacyPlayerBackup-20261012/Original`; removed files are under `Retired`. Exact targets are in `LegacyPlayerRemovalPlan.json`.

## Dependencies

- Stats and health still exist for progression, UI, monsters and towers; no old movement/animation overrides remain in player stats or stamina.
- Player kill credit/retaliation recognizes `GameData.Player.Script.PlayerController1`, not the removed melee component. Tower credit remains intact.
- Interaction no longer depends on the FPS trial. Keybinding UI uses the current PlayerInput action map and a separate custom-controller preference key, without loading legacy overrides into the new input map.
- Old FPS/camera-lock binding rows are unavailable. Sensitivity UI is disabled until the new camera owner is explicitly integrated.
- Boss slow/knockback hooks to the retired motor/ragdoll have been removed. They are not silently reimplemented on the user's new controller.
- Removed camera coupling from tower/rune/UI systems. Tower placement retains its own top-down presentation, not a player movement controller.

## Verified in Unity Editor

- Full asset refresh and successful compilation; Console contains zero errors.
- Loaded assemblies contain none of the retired controller/combat component types.
- All 23 scene root objects remain; the open scene has zero missing MonoBehaviour scripts.
- PlayerArmature keeps Animator, Rigidbody, CapsuleCollider, PlayerInput, PlayerController1 and InputManager; 73 transforms remain.
- Animator controller remains `Assets/GameData/Player/Animations/PlayerController.controller`; avatar remains the original Armature.fbx avatar.
- Four edited prefabs have zero missing MonoBehaviour scripts.
- SampleScene file hash remains `C37D8F72C7933006308F6A385502AB6F7A08AA259978EBB9D24706E521B7496F`.

## Scene persistence and limits

SampleScene was already dirty on arrival. Only targeted component removal was performed with Undo in the open scene. No scene was saved or overwritten. The user must save their current scene to persist these removals before reopening; the old disk scene still contains retired component references.

No Play Mode run, save reset, currency spend, Animator rewrite or modification to PlayerController1/InputManager was performed. This verifies removal and compilation, not the correctness of the user's replacement controller. InputManager was edited externally during the work and those changes were left intact.
