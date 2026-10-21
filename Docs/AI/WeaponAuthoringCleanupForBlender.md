# Blender authoring transition (2026-10-17)

Removed and backed up outside Assets:
- Assets/GameData/Player/Scirpt/Editor/WeaponViewAuthoringWindow.cs (+ meta)
- Assets/GameData/Player/Scirpt/WeaponViewLibrary.cs (+ meta)
- Assets/GameData/Player/Weapons/Rifle/RifleViews.asset (+ meta)

RifleViews.asset had no serialized references in project scenes, prefabs, controllers or other .asset files. Its referenced PistolHandPose was retained. PistolPresentationEditor no longer opens or checks that old window. The dedicated PreviewAuthoringPose/EndAuthoringPose methods and SetViewDistance setter, which had no other consumers, were removed. Current runtime grip, ADS, recoil, reload, saved poses, Scene gizmos and the F8 weapon debug panel remain intact.

Recovery archive: agent workspace Backups/WeaponAuthoring-BeforeBlender-20261017.zip. Restore its relative paths with original metadata if needed. For another checkout applying the changed-file ZIP, also remove the three paths above and their metadata; absence of a file in a ZIP does not delete the old copy automatically.

User Blender sources and step-by-step instructions are in the workspace BlenderWeaponAuthoring folder. Unity runtime camera: vertical FOV 60 degrees, aspect 16:9, near clip 0.05 m. Actual runtime eye location was approximately (0.014, 1.625, 0.123) in player-local coordinates, unlike its Edit Mode transform.

Unity Editor compilation passed after removal. Play Mode switched pistol -> rifle -> pistol successfully with authored pose references intact. No scene save or gameplay data reset was performed. Migration from procedural arm/reload motion to Blender clips is deferred until the user authors those replacement clips; deleting the active runtime pose system now would remove existing presentation behaviour.

The final Console had no errors but contained two monster-animation warning types: Animator.GotoState state missing and Invalid Layer Index -1, from MushroomMonster.Play/BeginAttack. They are outside the removed view-authoring code; they were inspected and not suppressed or attributed to this cleanup.
