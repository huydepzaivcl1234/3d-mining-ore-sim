# Encounter-driven day/night — 2026-10-16

DayNightSystem advances to the opposite period after all active encounters remain clear for three scaled gameplay seconds. DayNumber increases only when Night changes to Day. DayNightData exposes Cleared Period Delay Seconds (default 3).

The old duration still controls scheduled-wave timing and sky progression, but reaching its end no longer changes the period. A living enemy, pending wave, eligible pending night thief/Skullclaw, or unfinished God encounter blocks the change. A NightOnly daily event can pass through its empty daytime and spawn its budget at night. An empty period is allowed to finish without requiring a kill. No enabled spawner means no automatic transition.

The countdown resets when enemies return and pauses with scaled gameplay time. The normal lighting transition remains configured by Transition Duration Seconds. The countdown restarts after loading rather than persisting a nearly completed transition. Existing clock save keys and format are retained.

MonsterSpawnZone registers enabled zones; the clock checks matching zones and the existing live monster registry without per-frame scene searches. The existing explicit SetPeriod API remains available to authored events/debugging.

Files: DayNightData.cs, DayNightSystem.cs, MonsterSpawnZone.cs. No scene or prefab edits are required. New data fields use their serialized defaults.

Validation: connected Editor compilation passed. Isolated Play Mode fixture verified living enemies block beyond the old duration, pending waves block, reinforcements cancel the countdown, pending night encounters block, day-to-night takes 3.008 seconds, and night-to-day takes 3.005 seconds with exactly one day increment. Tests used a separate GameSaveStore and runtime-only data clones.
