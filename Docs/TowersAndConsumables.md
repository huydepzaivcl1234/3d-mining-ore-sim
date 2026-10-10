# Cannon tower and player consumables

## Assets and admission

- ZIP: `C:/Users/huyancut/Downloads/Reference_Cannon_Animated.zip`
- SHA256: `495915988DDB9F00EE71E152969F9B0C298C58B8944DB69D3F8684C1BC96B304`
- User confirmed ownership and explicitly approved import without license manifest/receipt.
- Imported only the two FBXs and five texture PNGs into `Assets/GameData/Tower/Cannon/Source`.
- Did not import/run ZIP scripts, reuse its GUIDs, or alter original monster/player assets.
- The canonical vendoring CLI was unavailable; this is a manually scoped import, not a canonical vendor receipt.
- Icon is a render of the actual prefab with transparent alpha, imported as a single Sprite.

## Designer controls

`Assets/GameData/Tower/Cannon/CannonData.asset` is a dedicated `CannonTowerData` definition.
Price, damage, range, attack speed (shots/second), health, MR, armor, projectile speed and level are editable.
Initial values are provisional tuning: price 100, damage 5, range 10 m, 1 shot/s, health 100, zero defenses, projectile speed 12 m/s, level 1.
Shot delay matches source animation frame 2 (0.033 s). Animation/controller and projectile are assigned.
Tower level is an authored value; no tower-level progression was requested or added.
Each future tower can derive its own data class from `TowerData`; add its asset to `Resources/TowerCatalog`.

## Buying, placing and selling

- Existing Upgrade Panel presents tower purchases instead of old upgrades. Existing upgrade save bonuses are retained, not wiped.
- Buy puts one unstackable tower into inventory, storing its actual paid price.
- Select tower: PLACE or SELL. Place confirms with left click; Escape/right click cancels without consuming the item.
- Placement rejects obstructed/steep surfaces and positions more than 12 m from the player.
- Placement consumes the selected slot, not the first matching tower, and suppresses player attacks during placement.
- PLACE smoothly enters top-down camera mode, showing a 1 m ground grid, ghost, occupied-cell perimeter, range circle and preview stats. Green is valid; red is blocked. Camera is restored on placement/cancel; Escape/right click cancels without consuming an item.
- Placement uses actor-filtered ground sampling and checks all four footprint corners. The stationary BaseMesh is aligned to the placement plane even while the firing clip plays.
- Click a nearby placed tower (or interact) for a non-modal billboard world-space stats panel. X or moving beyond 5 m closes it. SELL on that panel, or inventory sell, refunds 35% of its receipt. Changing GameData price does not change refunds.
- Bought inventory towers retain their receipts. Committed deployments now persist by unique ID and scene in `ChestDefense.PlacedTowers.v1`: tower type, position, yaw, current HP and paid price. Restore does not spend inventory or money. Sell/death remove records; Reset Data clears deployed towers. Invalid/unknown or newly blocked placements are retained rather than duplicated/refunded. Corrupt/newer saves are preserved and writes blocked. Cosmetic previews/tests are never registered.
- Cannon grounding uses a cached real BaseMesh foot vertex, not the transformed renderer AABB (whose empty corners previously caused a ~34 cm lift). The FBX has Read/Write enabled for the one-time foot scan; LateUpdate is a single point transform, not a full vertex scan.
- Towers use the shared navigation obstacle path and physical health/defenses. Player attacks do not damage their own tower or chest.
- Monsters retain player interception/retaliation and otherwise choose the nearest living tower/chest by collider distance.
- Cannon uses real swept projectiles: walls stop shots, not instantaneous damage through geometry.

## Consumables

| Item | Player effect | Duration |
|---|---|---|
| Apple | 10 base HP total, distributed over time | 5 s |
| Banana | +5 flat movement/sprint speed | 180 s |
| Carrot | +1% attack speed | 180 s |
| Grape | +5% maximum health | 180 s |
| Ice cream | 1% reduction of incoming damage | 180 s |
| Pea | Each player strike on a monster adds 0.5% of its max HP as true damage | 150 s |

Existing healing-effectiveness bonuses apply to Apple. Healing cannot revive a dead player.
Pea is added at the player strike boundary (including nonphysical strikes), not tower projectiles/burn ticks. The combined hit emits one damage event.
Grape expiration clamps HP to the new maximum and cannot kill a low-health player.

## Verification

`TowerConsumableTests`: 13 Editor assertions/cases, invoked through Editor reflection rather than Test Runner, cover exact effects, fractional multipliers, complete assets, transparent icon, receipt transactions, full inventory/invalid prices, true-damage event semantics, closest defenses on either side and dead-tower exclusion.
`MonsterRetaliationTests`: four existing regression tests.
`TowerPlayValidation`: isolated bounded Play Mode fixture disables/restores scene roots and uses an inventory database with no save key. Checks recoil, projectile damage, wall blocking, Apple healing, supported/blocked footprint, grid alignment, grounded BaseMesh, world-space stats/X/distance closing, top-down camera, ghost/grid/range/stats overlays, and cancellation restoring camera and inventory receipt. Mouse preview uses a deterministic test ray.
No original scene is saved or overwritten by validation. No player currency/progression reset is used.

## Cannon fantasy UI

The stats and compact damage-health billboard follow `Cannon_UI_Fantasy.zip` (Cannon_UI.png / Cannon_UI_Spec.json): dark plum panels, gold rounded borders, teal HP (red at or below 20%), six separate stat cells, the existing transparent cannon icon, Fredoka SemiBold titles and Nunito Bold content. Runtime values come from TowerData/MiningCharacterHealth; sell still uses the original receipt.

`Assets/Resources/TowerUiTheme.asset` holds font references. The separate `TowerDamageHealthBar` starts hidden, appears on actual damage, resets its deadline on each subsequent hit, and hides after 3 unscaled seconds. It never opens stats, locks input or changes selection. The opened stats panel retains X / walk-away closing independently of this timer. Ghost previews disable the damage component.

The isolated Play fixture tests damage reveal, critical color, repeated-hit timeout renewal and automatic hiding, in addition to the original placement/combat checks. Its camera capture is saved to `Docs/Validation/CannonFantasyUI.png` for visual review.
