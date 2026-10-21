# Glock/rifle pose and crowded-chest rendering — 2026-10-12

Connected Editor: Unity 6000.5.3f1, `D:/3d mining sim`. Authored SampleScene is not saved or packaged. Player saves are not reset.

## Changes

- Glock: hip raised 0.035 m; ADS raised 0.025 m and horizontally adjusted against the actual gun-only capture, not merely the muzzle axis. Current asset offsets remain editable under View in `Assets/ChestDefense/FPS/Generated/Pistol.asset`.
- Rifle: hip and ADS moved forward 0.10 m. Tune independently in `Generated/Rifle.asset`. Existing shot cooldown, reload, damage, input sensitivity and ammo are untouched.
- Cannon shared URP/Lit material now permits GPU instancing. This does not guarantee instancing on every URP/SRP batching path; no draw-call reduction is claimed without a frame capture.
- A closed chest no longer submits its imported `Treasure_*` interior meshes. These six meshes contain 198,445 vertices. All enabled interior content returns at opening, including animation frame zero; exterior trim, collision, coin burst and damaged-model rendering are preserved. Repair reapplies closed visibility after restoring the intact renderers. `TreasureChestData.hideClosedContents` provides a designer opt-out for a replacement model with externally visible treasure.
- Fully closed tower stats panels return before camera, collider, transform and text refresh work. Show/close fade behavior is retained.

## Evidence and limits

Suspected full-model lid sampling was measured rather than changed: 200 reflected `SampleLid` calls took 0.281 ms in an Editor microbenchmark. That result does not support it as the primary slowdown. Idle foundation alignment did not dirty the transform in the inspected prefab; no speculative movement rewrite was applied.

`MiningSimulator.Ores.Editor.ChestRenderChecks.Run()` creates an additive, disposable render fixture with one actual chest and 30 actual cannon prefabs, a fixed camera/light, 960x540 offscreen target, and synchronous GPU readback. Ten samples follow three warm-ups for each case. The only measured baseline/candidate difference is closed interior visibility; GPU instancing is enabled in both. The check never enters gameplay or writes a save/scene.

Two local Editor runs:

| Metric | Interior visible | Interior hidden |
| --- | --- | --- |
| Enabled chest mesh vertices | 253,679 | 55,234 |
| Render + synchronous readback median, run 1 | 15.734 ms | 2.341 ms |
| Same metric, run 2 | 16.559 ms | 2.025 ms |

A third reversal run measured 13.314 ms before hiding, 2.655 ms hidden, then 5.733 ms after restoring the original visibility. The restored baseline remains slower than the hidden case, but its different absolute timing exposes warm-up/scheduling variability. Do not translate these timings into a claimed gameplay FPS increase.

These are bounded offscreen render measurements, **not gameplay FPS**, GPU timestamps, or proof that every slowdown is eliminated. The fixture excludes combat AI, projectile updates, chest payout VFX, animated tower firing and HUD work. Driver/readback overhead and Editor scheduling are included. Opening the chest deliberately restores the high-detail interior; sustained combat with many active towers still needs a comparable Development Player CPU/GPU profile if the reported hitch remains.

Regression checks: open/close restores the exact enabled mesh vertex budget, repair does not reveal closed loot, and designer opt-out restores original content. Existing projectile-pool check passed 640 reuses of 32 shots with bounded retention. Both gun clip pose/rate checks pass. Isolated FPS Play check verifies real virtual mouse/key input, direction/sensitivity, damage, ammo, wall occlusion, hip/ADS, automatic rifle direction, animated reloads, HUD and rollback; it uses a temporary scene and separate test save.

## User retest

Test near the chest with the same placed towers, graphics settings and camera angle before/after opening the lid. Check Glock hip/ADS and rifle firing/reload. Check broken/paid/emergency-repaired chest states. Avoid comparing Editor Gizmos/Scene view rendering against a standalone build. Do not disable gameplay AI merely because a monster is out of view.
