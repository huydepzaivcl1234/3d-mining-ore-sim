# Bullet Fly level scaling and bounded horde performance

## Designer controls

- `Assets/GameData/Player/Weapons/BlueUpgrades/BulletFly.asset`: existing per-bullet base damage is preserved. New `damagePerPlayerLevel` defaults to 1. Each bullet = base + player level × coefficient, still true damage. Snapshot level when volley starts, before the existing per-target survival nerf. New volleys use the latest player level. Ownership, save keys and proc rules unchanged.
- `Assets/GameData/Resources/GameplayPerformanceData.asset`: renderDistance defaults to 300 m (0 restores camera original). Applies automatically to tagged MainCamera cameras on scene load, including inactive ones. It clips the world camera; it does not deactivate enemies, colliders or HUD. Shadows/terrain are not replaced with a new renderer system. Assets remain loaded in memory; the distance cutoff can visibly pop geometry.
- Same asset: spreadSpawnsAcrossFrames, maximumSpawnAttemptsPerFrame (2), spawnBudgetMilliseconds (2). Shared across normal zone SpawnOne requests; queued waves retain their members and retry next frame when the budget is exhausted. A single Instantiate cannot be preempted and can exceed the time budget. No enemy cap or daily count increase, no whole-monster pool/reset rewrite, no damage/AI throttling.
- Lean EN/VI Bullet Fly descriptions show the new coefficient.

## Measured validation, Unity Editor 6000.5.3f1, 2026-10-16

120 Bat rendering fixture, AI disabled to isolate render work; same camera, 90 frames per phase after warmup:

| Camera distance | Median frame ms | P95 frame ms | Mean Editor draw calls |
|---|---:|---:|---:|
| 1000 m | 11.971 | 12.981 | 6110.7 |
| 300 m | 9.945 | 10.754 | 5114.5 |

Draw-call counts include Editor surfaces, not standalone-build telemetry. This is not a moving/combat horde benchmark or a claim of Mega Bonk-scale performance.

Baseline unbudgeted instantiation of 120 Bat: 132.235 ms total, max single Instantiate 4.009 ms. Shared-budget fixture completed all 120 in 63 frames, at most 2 per frame, max spawn batch 3.594 ms (total 185.357 ms including per-frame overhead). Lower spike at the cost of spreading arrival across frames, not less total work. The fixture exercises the same budget used by SpawnOne but bypasses terrain placement checks; normal gameplay wave counts are unchanged.

Play Mode assertions passed: auto camera attachment, live distance edit and 0 fallback, base+level formula at levels 1/10, invalid index, actual true damage through very high defenses, level snapshot within a volley, next-level volley with existing 5% nerf, spawn conservation and frame attempt limit. Isolated save stores detached after Stop. No SampleScene save.

Further CPU/physics/animation horde optimization requires representative combat profiling; this change does not promise constant FPS for arbitrary counts.
