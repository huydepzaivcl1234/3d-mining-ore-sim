# Iron sights and cannon LOD — 2026-10-12

Connected checkout: D:/3d mining sim, Unity 6000.5.3f1.

## Changes

- ADS uses rear/front sight anchors on the animated gun, not a centered muzzle.
- Glock is upright; eye relief is 0.17 m. AKM eye relief is 0.14 m.
- Idle ADS compensates animated hand sway after graph evaluation. Shot/reload motion remains intact; no hand mesh added.
- Per-weapon `alignIronSights`, `aimEyeRelief`, `aimSightOffset` are editable. Legacy aim/hip fields remain; disabling alignment returns to explicit aim pose.
- Cannon keeps its original close-up mesh. LOD1 uses 7,689 rather than 21,972 triangles (65.0% reduction). LOD threshold is editable on its prefab LODGroup; full detail is used above 50% screen-relative height, lower detail below. No distance culling of gameplay.
- LOD children inherit original BaseMesh/BarrelMesh transforms. Collider, muzzle, fire clip/controller, target logic, damage and save format are unchanged.
- Original source FBX and textures are untouched. Reduced FBX retains UVs/material assignment. Existing placed-tower saves instantiate the updated prefab.

## Evidence

- Unity compiled with zero Console errors.
- Ammo, cooldown, reload/cancel, 100x attack-speed cap, 21 action samples per clip/gun passed.
- 300 animated ADS ticks per gun passed sight-line checks.
- Isolated Play Mode regression passed both gun ADS/fire/reload, magazine movement, damage, HUD, modal locks, F8 rollback and death camera release. Uses virtual devices and isolated save, not player currency/data.
- Cannon fire samples at 0, half and end keep the reduced barrel attached to its animated parent.
- Inspected full/reduced 30-cannon captures and four Blender views.
- Blender build wrapper: `VERDICT: SUCCESS`; validator: `VALIDATION: PASS (0 fail, 0 warn, 2 objects, 7689 tris)`.
- Commands: bundled Python ran `blender-modeling/scripts/run_blender.py` with Blender 5.2, `Tools/CannonLod/build_lod.py`, `validate_model.py --spec Tools/CannonLod/spec.json`, and `render_views.py --res 384`.
- Source import contained 256 degenerate faces: removed only from the generated copy before decimation. First validation compared local dimensions against world AABBs; corrected the spec to measured world dimensions and reran successfully.

## Performance limits

30-cannon isolated render + GPU-readback medians (full / reduced / restored full):

- First run: 9.724 / 2.345 / 3.110 ms.
- Second run: 3.513 / 2.617 / 2.278 ms.

These are Editor render-only microbenchmarks, excluding live animation, AI, target scans and payout UI. Restored baseline is faster than the first baseline; GPU warm-up/scheduling makes these unsuitable as proof of a gameplay FPS gain. Triangle workload reduction is verified; the remaining live crowded-map lag is **not proven resolved**. Do not conclude geometry is its only cause. No speculative collision-loop optimization was retained: 20,000-call probes showed no improvement.

## Safety

SampleScene SHA256 stayed FA6FCF075BC78051D99A9309C854C1FF3C45C982AF4858755DAE5F283B5201A0. No authored scene saved, no reset or changes to real save/currency. Temporary test scene/start-scene override cleaned up. No standalone build or 50-monster/live-cannon combat FPS soak was performed.
