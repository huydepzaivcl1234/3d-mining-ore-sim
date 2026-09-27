# Player stats GameData and panel

Run `Mining Simulator > Setup > Player Stats GameData And Panel` outside Play Mode, then save the scene.

The first run captures the Player's existing movement, health, regeneration, combat and respawn values into `Assets/GameData/Player/PlayerStatsData.asset`. Later runs retain the existing data and authored UI layout. No scene file is included in the delivery ZIP.

Edit numbers in that asset. Player component inspectors hide the migrated legacy stat fields, while input bindings, Animator, health-bar references and collision/camera settings remain editable on their original components. Monsters without MiningPlayerStats retain their own health settings. Current HP is runtime state, never written to the data asset.

The setup creates `Player Stats Button` beside the Upgrade button and `Player Stats Panel` inside the same HUD Canvas. Edit their RectTransforms, colors, font and layout in the scene. `Player Stats UI` owns the presenter and references; do not place the presenter on the panel that it hides. Open/Close uses the existing panel coordinator. Text follows Lean Localization English/Vietnamese.

The panel displays only: Player level, XP progress bar, damage, attack animation speed multiplier, configured movement speed (walk speed, not instantaneous velocity), and attack range. Other tunable stats remain in GameData but are not displayed. Run the same setup menu once to add the Level and Experience Progress objects to an older panel; subsequent runs preserve their layout. New panels are compact; existing panel size, title and Close button placement are preserved.

Player progression is independent of NPC progression. Configure starting values and XP requirement growth in PlayerStatsData. Monster kills now call MiningPlayerStats.AddExperience: excess XP carries into subsequent levels. Runtime progression can also call SetProgress(level, experience, requiredExperience). Level-ups do not automatically increase combat/movement numbers, and Player XP is not yet persisted between sessions.

Attack speed is the animation playback multiplier, not a separate cooldown. Animation contact and attack gizmos read the same data as damage detection. Changing max HP preserves current HP, clamps it to the new maximum and updates MicroBar's range without restoring dead players.

Validation: runtime and Editor C# sources compile with Roslyn against the project's Unity assembly references; ZIP paths and content are verified. This is not a Unity Editor compile or Play Mode test. Unity Editor import, setup execution and Play Mode still need validation; no desktop-control permission was requested for this change.
