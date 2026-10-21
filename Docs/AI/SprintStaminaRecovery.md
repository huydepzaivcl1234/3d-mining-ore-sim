# Sprint stamina exhaustion

- `AnimationRunMana` owns the exhaustion latch. Reaching zero blocks sprinting until stamina reaches `PlayerCoreStatsData.sprintRecoveryThreshold` times maximum stamina (default 0.25 = 25%).
- `PlayerController1` gates both physical speed and animation blend targets through `CanRun`, without changing input bindings, jump, or camera behavior. Players without this optional stamina component keep their existing behavior.
- Exhausted players can walk, and fading running animation cannot keep resetting regeneration delay. Holding Run resumes sprinting at the threshold. Once unlocked, going below the threshold does not lock sprinting again until zero.
- Refill (including existing respawn/reset callers) clears exhaustion. Zero delta time does not advance recovery. A zero threshold still requires positive stamina.
- Configure in `Assets/GameData/Player/PlayerCoreStatsData.asset` > Stamina > Sprint Recovery Threshold. Cost, regeneration and regeneration delay retain their existing authored values.

Validation: connected Unity 6000.5.3f1 compiled successfully; 10 isolated stamina checks passed in Edit Mode. Four Play Mode integration checks passed using the real player's controller/input/core data and an isolated physics scene: full stamina runs, depleted stamina walks, below threshold walks, exact threshold runs; checked physical speed (3/7 m/s) and Animator velocity targets. Temporary state and test physics objects were restored/removed. SampleScene was not saved. Existing Console warnings remain unrelated to this change.
