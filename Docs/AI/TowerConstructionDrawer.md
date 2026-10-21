# Tower construction drawer

## Player flow

- Open **Build / Xây dựng** above the bottom-right menu, or press **T** (rebindable in Keybind settings).
- Hover a tower card (or focus it with keyboard navigation) to reveal its stats. Click selects it for Buy/Place; leaving the card hides its information without clearing the purchase selection. Drawer bounds stay stable to prevent hover flicker.
- **Buy** purchases one tower into construction stock and keeps the drawer open. Repeat purchases update the owned count immediately, independently of bag capacity.
- **Place ×N** starts placement without spending money; it is disabled when stock is empty.
- The drawer closes immediately during placement. The ghost retains its grid, footprint and attack range; the placement hint shows remaining inventory.
- **R** rotates 90 degrees. **Esc / right click** ends placement; unplaced stock stays in construction stock. Valid placement consumes one tower, allowing consecutive placements.
- The item/equipment shop remains separate. Construction stock is a version-5 inventory-save field, not inventory slots. Legacy slot towers migrate once with their individual receipts in the same save transaction. Placed towers retain original paid price for resale. Reset Data clears stock too.
- The daily monster forecast is anchored at top-center and enters/exits vertically.

## Scope

The catalog list shows up to three cards at once and scrolls vertically for additional types in Resources/TowerCatalog. Each placed tower now has its own level and an Upgrade button in its world-space stats panel. Upgrades never mutate the shared TowerData asset or other towers.

Upgrade cost, maximum level, damage/health/attack-speed/range growth and defense increments are configurable per TowerData. Initial defaults: max level 10, first upgrade 75 gold, cost multiplier 1.5, damage +20%, health +25%, AS/range +5% of base per level, armor/MR +2. These are editable starting values, not a finalized balance. Added HP capacity is granted on upgrade; existing missing HP is preserved. Resale remains 35% of original purchase price.

Placed save document v2 includes level; the existing storage key is retained and v1 documents migrate without losing entries (missing level uses the definition's base level). Restore applies upgraded maximum HP before saved current HP. Unsupported future versions are preserved and saving remains blocked as before.

## Verification

- `TowerConstructionTests`: 5/5 Edit Mode tests passed (owned stock, purchase/cancel receipt, insufficient funds, full inventory, rebindable action).
- `TowerConsumableTests`: 16/16 regression tests passed.
- `TowerConstructionPlayValidation`: isolated Play Mode fixture passed drawer open, purchase once, cancel retention, owned-stock placement, repeated placement, 90-degree placed rotation, camera restoration and rebound-key opening.
- Cursor regression: reproduced stale shift-lock cursor ownership before the fix. Camera input-lock transitions now release that ownership synchronously. Placement temporarily disables Starter Assets' focus-lock policy, owns an unlocked visible cursor, and restores the previous policy on exit. The fixture now includes the real orbit/Starter input components, focus regain, queued mouse movement and a queued left-click; ghost tracking and exactly-one placement passed after the fix.
- Actual Play Mode capture: `Temp/TowerConstructionDrawer.png`.
- Final Editor check: no compilation failure or current Console errors; Edit Mode restored to `SampleScene`, Play Mode start-scene override is null.

Validation uses temporary isolated save data and restores scene-root active states. It does not spend the player's real currency or explicitly save/overwrite the dirty SampleScene. The isolated fixture is not a full combat soak test.

## Construction-stock update

The updated isolated Play Mode fixture passed repeated purchases without closing the drawer or filling bag slots, owned-count refresh, mouse placement consuming exactly one tower, cancellation retaining stock, and two save reloads without duplication. The existing cursor/focus, rotation and rebind checks remain in the fixture.

The current `Tower` Edit Mode regression selection passed 29/29 tests, including construction-stock purchases and v3/v4 legacy receipt migration.

## Upgrade / hover validation status

The `Tower` Edit Mode selection completed with 34/34 passed, including five new cases for independent upgrades, transaction rejection/max level, restored health, v1/v2 codec migration and five-card scroll/hover layout. Initial Test Runner main-thread timeouts resolved before continuing.

The extended isolated Play Mode fixture passed hover without resizing, actual Upgrade button, wallet deduction, serialized level, destruction/reinstantiation through TowerWorldSave.Restore retaining upgraded level/current HP without duplication, repeated purchases, stock reload, cancel retention, queued mouse placement, cursor/focus, rotation, camera restoration and rebound shortcut. Its temporary store disables legacy PlayerPrefs fallback; an initial fixture failure from importing legacy placements was diagnosed and corrected without modifying real save data.

Visual captures inspected: `Temp/TowerConstructionDrawer.png`, `Temp/TowerUpgradePanel.png` (LV 2, upgraded stats and next upgrade cost). Final Editor state: Edit Mode, SampleScene, no start-scene override, no current Console errors/warnings or compilation failure. No scene-save command was issued. No player build or full combat soak was run.
