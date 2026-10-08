# Treasure chest and monster spawning

Implemented in the connected Unity project `D:\3d mining sim`.

## Designer settings

`Assets/Resources/TreasureChestData.asset` controls placement, ticks per second, gold/level, health/level, XP/tick, XP thresholds, animation timing, interception range and panel visibility. Default income is one payout every five seconds. The spawner creates one base chest at runtime unless an authored chest is already active.

The chest uses the supplied HeavyTreasureChest open/close clips, a static body collider, the supplied coin-burst prefab, and the frame extracted from Group 1.svg. Gold and XP are awarded at the fully-open point. Level upgrades preserve damage already taken while adding maximum HP. Dead chests stop production. Level and XP persist under BaseTreasureChest.v1; HP currently restores on a new session.

Monster spawning now uses Terrain/A* availability instead of the removed mining NavMesh. Existing species unlocks and daily wave schedules remain in force, so monsters do not all appear immediately when Play begins. Monsters travel toward the chest; a nearby player ahead of their route is attacked first, and leaving interception range returns the monster to the chest.

## Verification

- 22 Edit Mode tests passed: chest economy and HP, navigation, boss skills, combat settings.
- Isolated Play Mode using the map's Terrain: spawn, movement, player interception, return to chest, chest damage, gold, XP/level and coin particles all passed (7.1 seconds).
- After lethal damage, wallet money stopped changing.
- At long range the panel scale approached zero and its position moved 0.2 m below the chest.
- No Console errors in the clean validation run. Returned Editor to SampleScene; Play Mode Start Scene remains None.
- No SampleScene file was saved or included in this patch. Temporary test scene was closed; no validation component was installed in production scenes.

The Play probe is editor-only and opt-in; it uses a separate wallet and disables chest persistence. It is not a long-duration crowd/performance soak or a complete visual art review.

## Installation

Apply the root-relative ZIP to the current project. It expects the existing HeavyTreasureChest model/prefab and existing A* navigation scripts. Keep their GUIDs. Compile before pressing Play.

## Reset limitation

Chest progress is not yet connected to the existing full Reset Data button. The proposed save-deletion integration was rejected by the permission reviewer because it would delete progress. No reset was executed and no reset hook was added. Explicit approval is needed before adding that integration. Normal Rebirth preserves chest progression.
