# Auto-upgrade priority and necklace hints

- Automation buys at most one upgrade per interval, scanning from the first displayed card downwards each time. Unaffordable or maxed cards are skipped. Card-only upgrades remain excluded.
- The carousel supplies its actual card order. Without a carousel, MiningUpgradeData.AutoUpgradeOrder remains the fallback.
- Automated purchases retain upgrade/wallet events and save behavior. MiningAudioManager skips only their purchase SFX. Manual purchases and UI button sounds remain unchanged.
- Empty necklace sockets use Resources/NecklaceSlotPlaceholder.png (the supplied image). MiningInventoryPanel exposes the sprite and tint; default alpha is 0.25. Equipping replaces the hint with the normal full-opacity item icon; unequipping restores it. Hints do not receive raycasts.
- No scene, inventory layout, equipment addresses, localization, or save keys were changed.

## Verification

Unity MCP confirmed script compilation with no Console errors. Isolated preview-scene checks passed for repeated top priority, skipping unaffordable/maxed cards, automatic/manual purchase context, card-only exclusion, all three empty hints, occupied-slot replacement, and restoring hints after unequipping. Tests did not modify player balances or save keys.

SampleScene SHA256 before and after: 03001E88ABA4D7E07B250E2BEE1C47F597187F1A356862CCCB8DC69009A157F1.
