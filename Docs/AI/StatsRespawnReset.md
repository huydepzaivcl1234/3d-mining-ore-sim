# Stats, respawn and explicit gameplay reset

- Restored bottom-left Stats button wired to existing MiningPlayerStatsPanel.
- PlayerRespawn uses the existing Respawn Now UI and Player Respawn Point. Death disables the current motor, releases the cursor, cancels reload; clicking restores health/stamina/ammo and motor state. Existing input, Animator, hand poses and movement tuning are retained.
- Settings Reset Data retains its double-confirmation, but calls WorldProgressReset instead of requiring the removed Rebirth scene component. It clears saved progression, card and blue gun upgrades, item effects/inventory, quests/achievements, currency, placed towers, monsters/corpses, collectible cards/boxes and active enemy/tower projectiles. Resets the clock and rebuilds encounter schedules. Controls/audio/graphics preferences and authored data assets are not reset.
- PlayerCoreStatsData.damagePerLevel defaults to 1. Flat level bonus = max(0, level - 1) * damagePerLevel. Effective gun damage = (weapon damage + level bonus) * gun-card multiplier; range falloff applies afterward. Stats and shooting read the same calculation.

## Validation

Live Unity 6000.5.3f1, D:/3d mining sim. Compilation passed, Console errors zero, missing scripts zero. Isolated GameSaveStore fixtures used throughout Play Mode; real player save not reset.

Final StatsRespawnChecks: real Stats button opens panel; lethal damage opens Respawn with motor disabled and cursor released; real Respawn click refills and relocates player; real Settings double-confirm reset returns level/day to 1, HP to 50, currency to 0, removes prior monsters and gun bonuses; Settings cursor remains usable. Temporary damagePerLevel=2 clone produced damage 1 at level 1 and 5 at level 3. All checks passed; data clone removed and testStore released after Play exit.

Scene changes left dirty for user's Ctrl+S. Backup copies: AgentPreviews/BeforeStatsRespawn.unity and AgentPreviews/AfterStatsRespawn.unity in the calling workspace. SampleScene was not overwritten.
