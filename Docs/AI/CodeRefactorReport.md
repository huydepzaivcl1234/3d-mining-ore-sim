# Code refactor — 2026-10-04

## Scope

Connected project: `D:/3d mining sim`, Unity 6000.5.3f1.

Structural inventory covers all 190 existing first-party scripts under `Assets/Scripts`. This is not a claim of exhaustive gameplay testing or a line-by-line semantic review of all 190 files. Third-party assets/packages were left untouched.

This delivery changes 9 existing scripts and adds 18 C# files, including partial source files and regression tests. It is a focused core refactor, not a rewrite of every feature.

## What changed

- `AnimatedPanel`: one shared base for the upgrade, quest and settings opening animation. Derived panels retain their original Inspector fields, localized text, wallet subscriptions and existing close/save callbacks. Closing restores the designer's original scale. Animation uses unscaled time.
- `DamageOverTime`: burn timing is now an independent object. Strongest DPS wins; weaker hits may refresh duration; refreshing does not postpone the next tick; catch-up stops on death/clear.
- `ExperienceProgression`: player XP arithmetic is separate from movement, potion multipliers and persistence. Existing level-overflow rules, flat-growth optimization and maximum-level handling remain intact. Save version/keys/JSON field names remain unchanged.
- `CombatHitQuery`: shared capsule/height/range/angle filtering for single hits and sweeps. Sweep deduplication reuses a set. The overlap query remains unbounded rather than dropping targets in crowded combat.
- Positive player-stats component references are cached by combat/health. References can still be resolved later when components are added dynamically.
- Miner target changes share one reset routine instead of copying the same navigation reset three times.
- Large classes are organized into partial files by responsibility. These remain the SAME Unity component, not extra components to attach or new runtime services. Serialized fields and component GUIDs stay in the original source files.
- Stale weapon regression-test setup was corrected: explicit EditMode lifecycle handling, current monster hit-query method, and reflection arguments for sweep damage.

## Source layout

| Component | Original main file | Main file after split | Related files |
| --- | ---: | ---: | --- |
| MiningNpc | 1901 lines | approximately 700 lines | Targeting, Navigation, Presentation, Gizmos |
| PlayerCombatInput | 822 lines | approximately 414 lines | Targeting, Footwork, Hits, Feedback, Gizmos |
| MiningAudioManager | 1155 lines | 635 lines | Music, Ambience, Sources, Feedback |

Moving code into partial files improves navigation; it does NOT remove the moved behavior or reduce runtime work by itself. The actual deduplication is in the panel base, hit filtering and miner target-reset routine.

## Compatibility and validation

- Original serialized field declarations preserved for every modified existing script; see `SerializationAudit.csv`.
- 66 block-bodied audio methods checked as verbatim copies after organization (line endings ignored).
- Existing Animation Event names, combo state names, input bindings, save keys, authored Animator/particle assets and localization text retained.
- Unity compilation completed; Console error query returned 0 errors.
- 54 NUnit assertion cases passed in a temporary additive EditMode scene through Unity MCP; 0 failed. Fixtures: RefactorRegressionTests, FreeFlowCombatTests, WeaponAttackTests, MinerNavigationClockTests, MonsterRewardProgressionTests. These were direct fixture invocations, not a Unity Test Runner XML report.
- Covered: XP overflow/invalid input, burn refresh/catch-up/clear, panel scale restoration, three-strike authored Animator compatibility, lunge envelope/range, hit selection/deduplication, monster hit volume, sword reset, stable miner approach/clearance, clock restore, monster level probabilities and player stat growth.
- No full Play Mode session, performance profiling, player build or exhaustive shop/trader/event UI playthrough was run. Audio method equality is not an audible mix test.
- `SampleScene.unity` was neither edited nor saved. Its existing dirty state was retained. Disk SHA256 before and after: `E5C6D9EF0E532392F840D0228E8A5215EB69A7E7925030208703012FFADA56AF`.
- No Reset Data, save deletion, economy balance change, commit, branch creation or GitHub publication.

## Remaining work / boundaries

The remaining large shop, trader, camera, ore spawner and UI-data classes were inventoried but not redesigned in this delivery. An exhaustive architecture migration would need separate behavioral coverage for those systems; this package does not claim that work is complete. Long serialized data definitions were not shortened by deleting designer options.

## Files and rollback

`CodeInventory.csv` lists all baseline scripts and the limited structural metrics inspected. Raw backup of the baseline scripts is in the workspace `refactor-backup/Scripts`; it contains existing user changes as they were before this refactor.

The ZIP contains only the changed/new scripts with their Unity `.meta` files and this report/inventory. Extract into the project root; no scene is included. All new partial source files must be imported together with their main files.

To roll back this delivery, restore the 9 changed originals from that backup and remove only the 18 new C# files plus their matching `.meta` files listed in the ZIP. Do not revert the whole Git working tree: it contains unrelated user work.
