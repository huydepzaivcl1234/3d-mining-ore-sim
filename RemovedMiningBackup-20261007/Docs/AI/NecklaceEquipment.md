# Necklace equipment

## Placement and tuning

The existing `Assets/GameData/Items/Icons/Necklace_Pedestal.prefab` now contains three world-space offers. Drag this prefab into your scene where you want the shop; this change intentionally does not place or save anything in SampleScene. An existing scene instance inherits the new prefab children unless it overrides them.

Edit `Assets/GameData/Items/Equipment/Necklace_Red.asset`, `Necklace_Green.asset`, and `Necklace_Orange.asset`:

| Item | Editable starting bonus | Editable starting gem price |
| --- | --- | --- |
| Red | 10% actual-hit life steal | 10 |
| Green | +25% healing, -20% regeneration interval | 15 |
| Orange | 2 burn damage every 1 second, refreshed to 4 seconds | 20 |

These are initial defaults, not fixed gameplay rules. Prices and bonuses are item data; rotation, bob height/frequency, purchase distance and fade duration are editable on each `MiningNecklaceDisplay` child. The three original necklace models and pedestal mesh/materials are unchanged. Purchase panels are editable children of the prefab and visible in Prefab Mode. In Play they fade in within range (default 4m), billboard toward the current camera and disable purchasing when dead, out of range, short of gems, inventory full or already owned. Camera lock/cursor behavior is unchanged; unlock the cursor with the existing camera control when clicking the panels.

## Inventory and effect ownership

- Left-click a necklace in the bag to equip it. The first of the five existing equipment positions is the necklace socket. `MiningInventoryPanel.necklaceSocket` can be assigned explicitly; its fallback resolves the currently authored first position.
- Left-click the equipped necklace to return it to an empty bag slot. Drag-and-drop supports equipping, swapping necklaces and returning one to the bag. A full bag cannot remove equipment by clicking, so no item is lost. Dragging onto a bag necklace can still swap.
- Only one necklace is equipped at a time. The remaining four future equipment positions remain reserved and reject inventory drops; potions and gifts cannot enter the necklace socket.
- Equipment transfers between bag and socket, rather than copying the item. It is not consumed and has stack size one. Bulk-use menus remain for consumables only.
- Purchase adds the necklace to the existing bag, not directly to the character. The existing wallet pays gems; a failed add refunds the price. Rebuying an owned or equipped necklace is rejected.
- PlayerStatsData no longer owns burn, life steal or healing-effectiveness fields. Base damage, base regeneration, movement, stamina, audio and other existing player settings remain intact. Card healing/interval bonuses and existing upgrades are preserved.
- Life steal uses actual HP removed by direct weapon contact, excluding overkill. Orange burn uses the existing strongest-DPS, duration-refresh behavior; switching equipment stops applying new burns but does not erase a burn already inflicted on an enemy.

## Persistence and validation

The existing inventory save key is retained. Version 2 adds `necklaceId` alongside the existing bag slots; version 1 saves load without equipped gear. Necklaces are registered in the existing database with zero source-drop weight and trader buy/sell disabled by default, so they are sold by the pedestal, not newly mixed into random ore drops.

Validated through the connected Unity 6000.5.3f1 Editor:

- Script import/compilation succeeded.
- Seven isolated Edit Mode tests passed: equip/swap/remove ownership; consumable rejection; full-bag removal; additive save DTO round trip; gem purchase/duplicate rejection; legacy/new inventory load; green bonus removal/restoration.
- Tests invoked in an additive temporary scene without saving SampleScene. Test persistence uses unique temporary keys, not user save keys.
- Pedestal prefab contains three valid display components, world-space canvases, GraphicRaycasters, price buttons and item references. Existing SampleScene has an EventSystem.
- Offscreen prefab render inspected: three colored necklaces and readable bonus/price panels. A preview-camera teardown diagnostic was produced while rendering; it is unrelated to runtime scripts or compilation, and the temporary camera/texture were disposed.
- SampleScene disk SHA256 unchanged: `E5C6D9EF0E532392F840D0228E8A5215EB69A7E7925030208703012FFADA56AF`.

Full interactive Play Mode combat/purchase testing was not performed to avoid altering the user's ongoing scene and saves. Check actual camera distance, cursor interaction and bobbing in Play after placing the prefab.

Clean-code rules were added to `Docs/AI/ProjectRules.md`: cohesive classes, composition before artificial inheritance, clear ownership boundaries, serialized/save compatibility, editable values and scoped Editor verification.
