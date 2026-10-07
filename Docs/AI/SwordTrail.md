# Sword trail and hit impact

Settings: `Assets/GameData/Player/PlayerStatsData.asset > Short sword trail`.

Player slash particle fields, coroutine delays, third-attack slash spawning and legacy slash callbacks have been removed. Imported attack events now use OnSwordTrailDownStart, OnSwordTrailUpStart and OnSpecialSwordTrailStart at their original timestamps; damage events, animation motions and the authored third-attack damage bonus are unchanged.

The ribbon uses `Assets/GameData/Player/Animations/Materials/SwordTrail.mat` and its authored shader with core highlight, opacity and soft-edge controls. History is 0.12 seconds (previously 0.08). After contact the ribbon continues for 0.06 seconds / AttackSpeed, never beyond returning to idle, sheathing, blocking gameplay, death or disable. It uses no particles. Lifetime, follow-through, blade coverage and tint remain editable.

Successful sword hits again instantiate the configured impact prefab and play its particles. Impact remains conditional on actual positive damage. Impact and its shared vendor dependencies are intentionally retained; this patch does not delete the entire Free Slash VFX vendor package.

Removed the three Slash VFX children from the open player's hierarchy using Undo, without saving SampleScene because it already contained unsaved designer changes. Press Ctrl+S when ready to keep the hierarchy cleanup. For importing this patch elsewhere, run Mining Simulator > Cleanup > Remove obsolete player slash objects; this only removes those named direct particle-system children and never saves automatically.

Deleted project-owned asset: Assets/GameData/Player/Animations/MiningCombat/Attack 3 Downward Slash.prefab and its meta. The local backup is in the task workspace's slash-remove-backup folder. ZIP imports do not remove files: remove that obsolete prefab if importing into an older checkout. Do not delete sword animation FBXs whose names contain slash; they are still the attack motions.

Validation: Unity C# compilation and new material shader validation passed. Isolated Play Mode checks covered all five attacks, contact and follow-through deadlines, idle/sheath/disable cleanup, correct material, migrated events, history expiry, actual damage and restored impact particles. User progression was blocked from saving in the diagnostic scene. The temporary scene is removed after testing. SampleScene was not saved or included in this ZIP.

ZIP paths are relative to the project root. The five existing FBX meta files update event names and preserve all other imported animation settings; their source FBXs must already exist.
