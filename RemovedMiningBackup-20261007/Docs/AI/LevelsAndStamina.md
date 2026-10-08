# Levels, monster rarity and stamina

Import the ZIP into the actual Unity project root (the folder containing Assets and ProjectSettings). Stop Play Mode, then run **Mining Simulator > Setup > Player Stats GameData And Panel** and save your scene. This opt-in setup adds the stamina component and editable `Player Stamina Bar` under the HUD Canvas, without replacing SampleScene or repositioning existing UI.

Player settings: `Assets/GameData/Player/PlayerStatsData.asset`. Level adds Health Per Level and Damage Per Level to the level-1 base values. Alive players gain the newly added max HP; dead players stay dead. Existing XP overflow is preserved. There is no configured gameplay level cap (integer storage is bounded by int.MaxValue).

Monster settings: the MonsterRewardData assigned to each monster prefab, normally `Assets/GameData/Monsters/MushroomRewards.asset`. Higher Level Chance is the chance of continuing to another level: with .35, level 1 is 65%, level 2 is 22.75%, level 3 is 7.9625%, etc. There is no maximum-level slider. Minimum Level shifts this distribution. Stat Growth Per Level defaults to .75: base * (1 + .75 * (level - 1)). HP and damage each roll within Random Stat Multiplier. Gold Growth Per Level similarly scales the base gold; XP and item drop chances retain their authored values. Health labels show level and current/max HP.

Stamina settings are also in PlayerStatsData: Max Stamina, Sprint Stamina Per Second, Stamina Regen Amount, Stamina Regen Interval, and Stamina Resume Threshold. Sprint consumes stamina while movement input is held; normal movement remains available when exhausted. Recovery ticks only outside sprint. Resume threshold prevents flickering between sprint and exhaustion while holding the key. Respawn refills stamina. No combat animation/controller is replaced.

Assign your own Exhausted Breathing AudioClip and volume/interval in PlayerStatsData. With no clip, a synthesized filtered-noise inhale/exhale fallback plays. Existing MiningAudioManager routes it through the game's SFX mute/volume; when absent a local AudioSource is used. The stamina HUD's Fill and Value children are scene-editable. Stats includes current/max stamina.

Validation: source compilation covers StarterAssets, runtime and Editor assemblies. NUnit regression tests were added for rarity/growth; they require Unity Test Runner to execute. Play Mode behavior, appearance and audio must still be checked in Unity. SampleScene is deliberately excluded from delivery.

## Low stamina, persistent progression, death SFX

PlayerStatsData exposes Low Stamina Fraction (default .2), Low Stamina Text Color (red), and Low Stamina Blink Seconds (.7). Below/equal to that fraction, the stamina label pulses its alpha, staying red. When recovered, its original scene-authored color/alpha is restored; disabling the HUD also restores it. Blink uses unscaled time for presentation only.

Player level, current XP, and next-level XP requirement are stored together as versioned JSON at PlayerPrefs key `MiningSimulator.PlayerProgress.v1`. Only dirty progress writes, at most once a second, with flush on pause/quit/disable. HP/damage growth is derived from saved level and current GameData, avoiding stale duplicated stats and double-applying growth. Authored movement/combat/stamina configuration stays in GameData; transient current health/stamina are not saved. Death, respawn, rebirth, and scene changes do not reset player progression. The existing Reset Data / ResetAllProgress workflow clears this key and resets loaded players to GameData starting values. The existing New Game button also calls ResetAllProgress, as it did before. Invalid/unsupported saves are preserved and not overwritten; Reset Data explicitly clears them.

Death Sfx and Death Sfx Volume are in PlayerStatsData. The existing PlayerDeathRespawn component plays it once on entering death, through MiningAudioManager and its mute/volume settings. Without an assigned clip, a short synthesized descending-tone fallback plays; replace it with your own death recording. No scene setup is needed for this update if the previous stamina HUD and death/respawn setup are already installed.

Manual Play Mode checks: drain below threshold and recover to verify warning color restoration; gain XP, exit/relaunch, verify level/XP/HP/damage; die/respawn and rebirth without losing level; press Reset Data, exit/relaunch, verify starting level; verify death clip plays once and respects SFX mute. These interactive checks have not been executed by the agent.
