# Bounded combat projectile pooling

## Changes
- Cannon and ranged-monster shots reuse scene-owned instances, rather than instantiate and destroy on every shot.
- Cannon GameData: `projectilePoolCapacity` (default 64); ranged attack Inspector: capacity (default 32). Zero destroys returned shots. The scene pool retains at most 256 inactive shots across buckets; overflow is destroyed. Active shots are not capped or dropped.
- Reset clears owner, target, damage, lifetime, launch flags, hit buffers, trails and particles. Initialization happens before activation. Scene unload destroys the pool; projectiles are not saved.
- Cannon target scans and player-collision refreshes start at staggered phases.
- Dead monsters stop health/AI processing, collision and health-bar display. Registry removal is deferred until LateUpdate; authored death animation still plays, then the existing death delay destroys the body. Whole monsters are deliberately not pooled until their species-specific reset contracts are safe.
- Placed towers and their existing world-save behavior remain unchanged.

## Validation
Invoke `MiningSimulator.Ores.Editor.ProjectilePoolChecks.Run()` through the connected Editor. It creates and closes an unsaved additive scene, does not enter Play, and does not touch player saves.
Checks: 32 simultaneous shots reuse 640 times without additional creation; inactive rent; double return; world pose; owner and homing-target reset; separate typed buckets; bucket overflow; scene-wide 256-instance retention bound.

These are lifecycle checks, not an FPS benchmark. Still verify in Play with 1/16/32 cannons and up to 50 monsters: stable shot/hit rewards, corpse removal after death delay, scene reload, tower destruction during shot flight, and CPU/GC measurements in Profiler. Compare identical scene, camera, quality and enemy counts after pool warm-up. Do not claim an FPS percentage from creation counts alone.

SampleScene is neither saved nor included in the delivery archive.
