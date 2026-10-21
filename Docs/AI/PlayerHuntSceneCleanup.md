# Player-hunt scene cleanup — 2026-10-14

User approved removing chest/legacy mining upgrades, retaining source assets and monsters, and letting monsters hunt the player without a chest.

## Scene changes

The live SampleScene in Edit Mode now has only PlayerArmature and Map roots. Retained lighting, terrain, scenery, borders, day/night, monster spawner, localization, EventSystem, player HUD, inventory, loot, quests, audio and settings are grouped under Map. Player controller, input, Animator, pistol and saved hand pose were not changed.

Removed scene instances: Chest System, BaseTreasureChest, GoldenSquareSlab upgrade station, Wandering Trader System, Wander, Fish_Market_lvl2, Necklace_Pedestal, RuneStoneShrine, Upgrade System, Rebirth System, Lucky Block System, Toxic Slime Portal Visual, old Camera System/Mining Interaction System, retired money/gem/rebirth HUD, Main Menu, Computer Info Panel, Shop Panel, Wandering Trader Panel, rebirth panels and shop/upgrade/rebirth UI components/buttons. Assets and source code for retired systems remain available; save data was not reset.

Scene operations are Undo-recorded. Original SampleScene file was NOT overwritten because the scene already contained unsaved user changes and prior constraints prohibited overwriting it. The cleaned live scene remains dirty; review it and use Ctrl+S to persist the current scene if desired.

## Recovery copies

- Before cleanup, including previously unsaved changes: `C:/Users/huyancut/Documents/ChatGPT/mining simulator 3d/AgentPreviews/BeforePlayerHuntCleanup-20261014-132937.unity`
- Cleaned scene copy: `C:/Users/huyancut/Documents/ChatGPT/mining simulator 3d/AgentPreviews/AfterPlayerHuntCleanup.unity`

## Runtime change

MonsterSpawnZone has Require Treasure Chest enabled by default for compatibility. It is disabled on this scene's spawner. Hunt-mode spawning uses the spawner position and editable 12–25 m ring, retaining terrain/obstacle/spacing validation, quotas, species gates and night scheduling. It does not recreate a chest. MushroomMonster targets the living player without the defense interception rule. Mushnight uses normal combat instead of stealing from a nonexistent chest. Skullclaw's night-only lifecycle remains unchanged.

## Validation

- Unity compilation succeeded.
- Edit Mode: 2 roots, 0 missing script components, 0 TreasureChest/MiningUpgradeSystem/MiningRebirthSystem components.
- Normal runtime spawning succeeded with no chest/cannon; terrain navigation grid baked successfully.
- Seven species instantiated for bounded night-mode targeting checks: Mushroom, Golem, Bat, Mushranon, Forest Golem, Mushnight, Skullclaw. All selected the player and reported Following A*. Test clones were despawned without kill rewards.
- Actual R reload with diagonal run, jump, look and ADS passed: 6.79 m movement, 1.52 m jump. Three-shot feedback test passed with reused muzzle flash and slide/recoil settling.
- SHA-256 confirmed original SampleScene, PlayerController1, InputManager and PistolHandPose unchanged on disk.
- Two earlier external test-helper errors attempted to read a Skullclaw clone after it despawned during daytime. The helper was corrected with null-safe reporting and a night-mode test; the final seven-species check passed. These were not production gameplay exceptions.

Runtime weapon FX create temporary roots during Play Mode; the two-root layout refers to the authored Edit Mode hierarchy. No broad asset deletion, settings change or save reset was performed.
