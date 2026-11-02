# Survivor stats and health — 2026-10-28

Project: D:/3d mining sim. Scene: Assets/Scenes/SampleScene.unity.

## Changes

- Renamed MiningPlayerStats, MiningCharacterHealth, MiningPlayerStatsPanel and MiningPlayerStatsData to PlayerStats, CharacterHealth, PlayerStatsPanel and PlayerStatsData. Updated source references and full serialized class identifiers; script .meta files were moved with their scripts so GUIDs remain intact. Added MovedFrom metadata. Existing save keys and serialized card bonus names remain unchanged for compatibility.
- PlayerCoreStatsData is the player's source for health, optional health per level, armor/MR with optional growth, resistance scale, regeneration and attack-speed multiplier, alongside existing progression/stamina/movement fields. Current balancing values preserved: 50 base HP, zero base armor/MR/regen, existing walk/run and XP settings.
- Fixed missing defense/equipment/regeneration effects when the legacy stats data is null. Removed CharacterHealth's dependency on retired mining upgrades. Non-player health retains its own fallback/monster settings.
- Source-aware CharacterHealth Inspector hides ignored player fallback values such as Max Health=100, shows effective totals and links to the correct Core Data asset. Edit Mode Health reports max HP rather than uninitialized runtime zero; stored HUD preview now reads 50/50.
- Reused existing font/layout for Stats. Shows actual auto-attack damage, attacks/sec, walk/run speed, attack range, stamina, health, armor/MR with mitigation, HP/sec and healing per tick. Footer shows actual projectile count/speed. No jump-force or obsolete gun labels in the survivor panel.
- Combat attack data still lives in SurvivorCombat.asset: base damage, interval, range and projectile settings. Panel reads the actual combat component rather than a duplicate stats formula.

## Validation

- Validation.json: runtime checks passed for core health source; physical (40 damage -> 20 with 100 armor), magic and true damage; healing clamp; regeneration (+10 HP in test interval); health level growth; damage/projectile/speed card bonuses; attack cadence multiplier; no healing resurrection; full respawn; zero Missing Scripts. Test advanced 45 frames.
- EquipmentValidation.json: temporary equipment correctly added armor and MR, healing effectiveness and regen interval reduction while legacy player data was null; original equipment slots restored immediately.
- Test modified a cloned Core Data object and isolated profile JSON. Original Core Data values confirmed unchanged after stopping. testStore reset after Play Mode.
- Stats.png: real 1920x1080 Game view of the revised panel, visually inspected for readable rows and no overflow.
- Compiles successfully; current Console has no runtime errors. Existing project deprecation/unused-field compile warnings remain.
- During implementation a local variable naming collision was corrected; a verification-only item initially lacked Equipment use type and was corrected before the successful equipment test. Pipeline timeout diagnostics came from a modal external-scene-reload dialog, which was resolved through Unity UI.
- Standalone build, all enemy archetypes and a complete automated test suite were not run this turn.

## Editing

Select PlayerArmature -> Player Stats -> Core Data, or press Edit Player Core Stats in Character Health. Edit base combat attack settings in Assets/GameData/Survivor/SurvivorCombat.asset.

Before/ stores originals; ChangedFiles.json records initial migration file paths. Internal shared namespaces still use MiningSimulator.Ores for compatibility; no wholesale namespace/save migration was attempted.
