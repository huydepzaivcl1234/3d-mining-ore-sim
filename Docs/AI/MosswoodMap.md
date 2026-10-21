# Mosswood chest-defense map

This is an opt-in environment prefab/tool. On 2026-10-11 the user explicitly requested application to the current map, superseding the earlier prefab-only requirement. The environment is now applied to the open SampleScene, left unsaved for review. SampleScene is never included in the ZIP.

## Assets and visual decisions

- `Assets/GameData/Maps/Mosswood/MosswoodDefenseMap.prefab`: 90m square clearing with raised outer ground, four gently wandering dirt approaches, a clear 13m combat radius, four suggested tower positions, and existing Ultimate Nature Starter spruce/rock/bush/grass assets.
- Moss foliage, earth paths, cool gray rocks; retain existing chest/cannon bronze and wood materials. No map-wide particles or emissive vegetation.
- Terrain grass/dirt textures and all meshes come from the existing repository. Material copies belong to Mosswood; vendor originals remain unchanged.
- Existing trader and rune-shrine plots are kept clear. The prefab does not duplicate shops, chest, monsters, towers, navigation managers or player.
- `MosswoodLighting.asset` supplies the palette preset. Application makes a separate `AppliedLighting.asset` from the scene's actual configuration and copies only lighting fields, preserving current timings/events.
- Warm chest point light has no realtime shadows and follows the existing clock. Preview PNGs use fixed sun poses, not a simulated full day cycle.

## Opt-in setup

1. Preview the prefab before editing your own scene. Tool: `Chest Defense > Map > Build Mosswood Prefab (no scene edits)`.
2. Place the root at the chest's X/Z origin, Y at supported ground. Do not move the gameplay chest into a second prefab.
3. Use `Chest Defense > Map > Apply Mosswood to Current Scene (backup + Undo)` for integration. It saves a copy of the current in-memory scene to `Temp/MosswoodBackups`, clones TerrainData, paints soft earth/moss transitions, replaces terrain trees only within the 88m square around the chest and disables the prefab ground. Original TerrainData, terrain heights, external forest and gameplay roots remain intact. Duplicate installation is refused.
4. The installer clones the scene's actual DayNightData and copies only palette fields, preserving current timings, events and sun/volume references. No global pipeline or quality assets are changed. Terrain tree rendering distance becomes 130m (was 5000m), billboards begin at 45m, detail distance becomes 40m; these remain editable in the Terrain Inspector.
5. Ensure WorldNavigationGrid bounds include the desired field, then rebake using the existing navigation workflow. New ground meshes use the existing Ground layer. Static tree trunks/rocks retain physics when culled.
6. Check your camera orbit, spawn radius, trader/rune interactions and placed-tower saves on the integrated map before saving your scene.

## Rendering and performance

ForestMapVisibility caches decoration renderers and checks distance every 0.2 seconds, using bounds plus 6m hysteresis. Default visible distance 65m is editable. It does NOT deactivate GameObjects, NPC AI, colliders or gameplay. Unity's built-in frustum culling and vendor LODGroups still select visible meshes. Nearby objects behind the camera may still cast necessary shadows. This is not baked occlusion culling; an occlusion bake requires the final authored scene and is intentionally not performed on SampleScene.

PlayerDeathRespawn now reuses its camera CharacterController, disables it on restoration and destroys it only with the owner. The proxy lives in the owner's scene for additive-scene correctness. Animator rebinding is retained to preserve correct recovery poses. Profiler markers `ChestDefense.Respawn.RestoreControls` and `.AnimatorReset` isolate that work. This removes native proxy create/destroy churn; it is NOT evidence that the full respawn FPS issue is fixed.

## Verification and limits

Validation is run in a disposable additive scene placed far away from the authored field. It tests real WorldNavigationGrid paths on all four approaches, actual collider bounds, combat clearance, material references, absence of particles, and ten camera-reset cycles. It restores the original active scene and does not enter Play Mode or alter saves.

Day/Night PNGs are rendered from the identical camera in an isolated preview scene. They include render-only chest/cannon references, not duplicate gameplay objects. They are not screenshots of integrated SampleScene gameplay.

Still required: integrated Play Mode monster/camera test and full-frame respawn profiling with representative enemy counts. No FPS improvement percentage or complete lag fix is claimed.

### Follow-up live Editor check — 2026-10-11

`MosswoodPlayCheck.Begin()` runs an explicit, bounded, separate Play start scene. It clones the actual authored player with its rig/controller, clears external scene references, uses a reset temporary GameSaveStore, restores the start-scene override immediately on entering Play and deletes the temporary scene on exit. It does not deactivate or save SampleScene roots.

- Re-run of isolated geometry/navigation validation: PASS (8,352 collider/approach checks, four actual A* paths, distance culling far/near/restore).
- Actual player clone: five lethal ragdoll/landing/respawn cycles PASS; respawn indicator did not appear while airborne; controller and Animator restored.
- Synchronous RespawnNow call durations: 3.29, 0.61, 0.60, 0.54, 0.54 ms. These are call durations, not full-frame CPU/GPU timing or an A/B FPS comparison.
- Four actual MushroomMonster prefab instances spawned on the four approaches reached the chest; actual chest damage confirmed. This is not a 50-enemy stress test or validation of every species.
- Editor returned to SampleScene with its existing unsaved state; Play start scene is null. SampleScene disk SHA256 remains `70F13EC19762C07FDB334055AE3031E2A52C456930DF85AAEAC414F3CBA2BA1F`.
- Compilation is clean and the run produced no new runtime errors. Historic console-buffer errors are not current failures.
- Refreshed same-angle Day/Night preview captures remain isolated renders. Visual inspection shows overly flat ground/path material transitions, broad empty clearing, and insufficient warm nighttime emphasis near the chest. The current map is a functional layout, not yet a polished cinematic environment.

Still pending: the user's complete gameplay scene under representative enemy/tower load, orbit-camera collision around every decoration cluster, full-frame respawn profiling, all-species navigation, and final lighting/material refinement. The fixed-camera fixture does not validate the complete orbit-camera system.

ZIP is a project-relative patch, depending on Ultimate Nature Starter and existing game scripts already in this repo. It is not a standalone redistributed vendor asset pack. SampleScene is excluded.

### Applied-map refinement — 2026-10-11

- Terrain uses three owned TerrainLayers (moss, shaded moss, earth), soft/noisy path borders and large-scale grass tone variation rather than two hard-edged meshes. Standalone preview terrain is separate from the gameplay Terrain.
- Applied scene has one Mosswood prefab, no duplicate chest/player/shops. It removed 373 original Terrain tree instances inside the map; 5,318 outside instances remain. The original TerrainData asset was not edited.
- Safety copy including the user's unsaved changes: `Temp/MosswoodBackups/SampleScene_20261011_221952.unity`. Integration supports Undo; review before saving the scene. The backup is not shipped.
- Identical-angle captures `AppliedDay.png` / `AppliedNight.png` render the actual installed geometry with temporary day/night light and ambient values. Temporary capture camera, lights and RenderSettings are restored afterward. These are Edit Mode renders, not full gameplay/post-processing or FPS evidence.
- Warm chest guide intensity is 9 at night, with no shadow-casting point light and no map-wide particles. Exposed through ForestMapLighting.
- Inspiration: https://fionazhu.com/3d-environment-swamp-cabin/ (forest path/clearing composition), https://portwindyroad.artstation.com/projects/v11QxD (readable main paths and side areas). No assets were downloaded from these references.
- Current geometry validation again passes 8,352 collider/approach checks, 13m combat clearance, four actual A* routes and culling restoration. This validates the prefab routes, not every possible path on the full authored scene.
- The installed SampleScene also passed four A* approach checks from 30m to 4m (all returned three corners). Every terrain height sample matched the original asset. Only one Mosswood instance exists and the Play Mode start-scene override is null.
- Repeated isolated Play check after refinement: five grounded ragdoll/respawn cycles and four mushrooms damaging the chest PASS. Synchronous reset durations were 2.98, 0.53, 0.57, 0.58 and 0.56ms; not full-scene frame-rate results.

The previous follow-up section records historical results. Full-scene stress profiling and every-species/camera collision coverage remain unverified; no complete respawn FPS fix is claimed.
