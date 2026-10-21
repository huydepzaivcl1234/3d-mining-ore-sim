# Render distance setting and horde CPU follow-up

## Runtime behavior

- Existing Settings > Graphics has one new Render distance slider (25–1000 m), using the existing uGUI row/slider style. EN/VI captions use the existing MiningLocalization route. Back & Save persists it in the existing Graphics.v1 preferences; partial older JSON retains the Performance GameData default. Reset Graphics restores the default, with no changes to world progress.
- The existing main-camera clipping component accepts the player's live setting. It clips all world-rendered geometry beyond its far plane regardless of species/item/tree classification. Terrain tree/detail draw distances are capped by the chosen distance as well, never increased beyond authored values, and restored on graphics teardown. It is render culling, not unloading assets or disabling gameplay colliders/AI. Screen-space HUD is unchanged.
- GameplayPerformanceData.monsterSteeringInterval defaults to .05 s (20 Hz spatial path/avoidance probes); 0 restores per-frame calculations. CharacterController movement, gravity, target/attack state checks and attack contact timing remain per-frame. Target switch, significant goal shift, grid revision, reached corners, path completion and Reset invalidate/bypass cached steering. Actual collision resolution remains CharacterController.Move each frame.
- Health uses its existing cached monster owners instead of repeated hierarchy/component lookups, avoids repeated missing-player-stat lookups on monsters, and does not rotate hidden health bars. Damage, regeneration, burn, death, popup types and boss countdowns are unchanged.
- Profiler markers Mining.MonsterSteering and Mining.HealthUpdate remain available for future measurements.

## Evidence, Unity Editor 6000.5.3f1, 2026-10-16

Initial 50-monster live AI fixture: median frame 11.77 ms vs 7.47 ms without monsters. Disabling AI reduced it to 8.68 ms; hiding renderers afterward only reduced it to 8.18 ms. Script load was the larger contributor in this fixture. This did not reproduce the user's exact 200→40 FPS report.

Final A/B, same running 50-monster fixture, 90 frames after each warmup, grid baked and all 50 actors actually moved:

| Spatial steering | Median frame ms | P95 ms | Mean script update ms | Mean steering ms |
|---|---:|---:|---:|---:|
| Every frame (interval=0) | 11.606 | 15.846 | 4.225 | 2.068 |
| 20 Hz (.05 s) | 9.868 | 12.699 | 2.652 | .770 |

The fixture used a temporary 80×80 m A* area when no live grid was available; it was destroyed after the test. Sampling phases are sequential, and actors keep moving, so these are directional Editor measurements, not identical-frame standalone benchmarks or a promise of 200 FPS. An earlier candidate run with an unready/missing grid was excluded rather than treated as a speedup.

Health marker previously measured ~.367 ms/frame with 50 actors; after caching it measured ~.043–.046 ms in the final fixture. No AI actors were disabled in the 20 Hz comparison.

Play Mode assertions passed: slider applies live to camera at 85 m, slider active/nonzero width, old-save defaults, actual preference save/read with original preference restored in finally, nonfinite input, graphics reset. Settings screenshot was inspected after opening animation settled. Original preferences and test save backend restored; SampleScene was not saved. Existing NUnit preference tests gained migration/sanitization assertions; the full NUnit suite was not run.

Remaining: this does not guarantee constant FPS with arbitrary enemy/effect counts. The exact 40 FPS combat scenario still needs a capture including upgrades/VFX, active actors, draw calls and CPU/GPU frame timings. Distance culling cannot remove nearby AI work; asset memory remains loaded, and lowering distance causes visible pop-in.
