# Top-down survivor conversion — 2026-10-28

Project: `D:/3d mining sim`, scene `Assets/Scenes/SampleScene.unity`, Unity 6000.5.3f1.

## Gameplay

- Fixed orthographic camera pitched 85 degrees down from the horizon (5 degrees from vertical), follows the player without mouse-look. Camera size 9.5 and height 20 are editable on `TopDownSurvivor`.
- WASD/gamepad movement remains camera-relative while the actor turns. Existing run, stamina, jumping, health, enemy waves, loot, damage popups, XP and HUD remain.
- Nearby living enemies in range are ranked by distance and checked for obstruction. Energy bolts fire automatically, perform swept collision checks and dispatch through existing damage/kill/card effects. No click, ADS, ammunition or reload is required.
- Removed the pistol/rifle models, FPS duplicate geometry, gun socket, FPS camera anchor, IK components, weapon progression/drop controls, shooting component, crosshair/ammo HUD and weapon animation layers from the gameplay scene. One complete Ch15 skinned mesh and one Animator remain.
- Original source/reference assets and legacy types needed by other project code were preserved for compatibility and rollback. The old FPS pipeline no longer runs in this scene.

## Cards and existing progress

The existing card choice UI/font/layout is retained. Damage increases automatic attack damage; the previous magazine bonus becomes extra energy projectiles; projectile-speed bonuses affect bolt travel. Existing effect cards and their save IDs remain compatible. Base maximum projectiles per volley is 8. Combat pauses during card choices and clears active bolts on death.

Balance is in `Assets/GameData/Survivor/SurvivorCombat.asset`. New runtime code is limited to `TopDownSurvivor`, `SurvivorAutoCombat` and `SurvivorCombatData`. Existing movement, input, stats, hit context, effect-card integration and stat labels were adjusted. The new locomotion controller contains the existing base locomotion layer only.

## Verification

`Validation.json` records an input-driven 420-frame Play Mode test with isolated save storage and temporary high-health dummy enemies:

- Closest valid target selected: true; projectile damage observed: true.
- 11 automatic volleys and 36 hits during the measured run.
- W advanced world Z by 2.88229 m, X drift below 0.00001 m.
- D advanced world X by 2.88228583 m, Z drift below 0.00002 m.
- Large injected mouse deltas did not change camera orientation; maximum angle error was 0.0000538826 degrees.
- Card selection stopped combat and paused time: true.
- Damage card: 55.3000031 -> 55.65.
- Extra-projectile card: 3 -> 4.
- Projectile-speed card: 22 -> 22.22.
- Death stopped new volleys and cleared projectiles: true.
- Gameplay hierarchy: zero PistolShooting, zero FullBodyFirstPerson, one skinned mesh, one Animator.

Regular live monsters were also observed receiving automatic damage in the actual scene. `Gameplay.png` and `Cards.png` are real Game-view captures, opened and reviewed before handoff. `ValidationFrames` contains controlled test captures, not regular enemy-wave footage. Temporary test objects, damage isolation and save override were removed; Editor was returned to Edit Mode and the scene saved.

Final runtime observation had zero Console errors/warnings after clearing test-tool messages. Full recompilation still reports 126 existing project deprecation warnings; no compilation errors. During validation, a JSON Vector3-report formatting error was corrected and a tool request timed out while entering Play Mode; both are tool/harness issues recorded here, not hidden gameplay passes. The final 420-frame run completed successfully after those corrections.

## Review files

- `Gameplay.png`: regular top-down gameplay and original HUD.
- `Cards.png`: existing choice panel with automatic-attack upgrades.
- `FirstLook.png`: initial regular-wave verification.
- `Validation.json`: final numeric test evidence.
- `Before/SampleScene.unity` and `Before/Assets/...`: rollback copies of the scene and modified scripts.
- `IsolatedProfile.json`: test-only save, not the live user profile.

Reference direction: [Megabonk on Steam](https://store.steampowered.com/app/3405340/Megabonk/) describes survival against hordes, loot and weapon upgrades. The 85-degree camera is the user's requested design; this conversion does not copy Megabonk assets or claim a complete recreation.

Standalone build, multiplayer, mobile input, performance stress limits and long-run balance were not tested.
