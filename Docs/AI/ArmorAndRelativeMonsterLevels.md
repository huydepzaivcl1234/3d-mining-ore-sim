# Armor, magic resistance and player-relative monster levels

## Damage pipeline

MiningCharacterHealth remains the health owner. CombatDamageType distinguishes Physical, Magic and True damage. Old single-argument damage methods remain available and mean Physical. Direct player attacks use PlayerStatsData.attackDamageType; monster attacks against the player use that species' MonsterRewardData.attackDamageType. Burn ticks use Magic. The explicit Editor Test Death command uses True damage.

Received damage = max(0, incoming damage - relevant resistance). Armor applies to Physical; Magic Resistance applies to Magic; True bypasses both. Thus 150 physical damage against 100 armor removes 50 HP, while 100 or less removes no HP. Negative/non-finite inputs are rejected or treated as zero. Life steal, damage popups and boss damage thresholds use actual HP removed after mitigation and overkill clamping. Legacy resistanceScale fields/signatures remain hidden for serialized/API compatibility but no longer affect damage.

## Authoring

- PlayerStatsData: armor, magicResistance, armorPerLevel, magicResistancePerLevel and attackDamageType. Player defense = base + growth * (current level - 1) + equipped item bonuses. Both growth values default to 1 and are editable. Current/restored level drives defenses directly, so no separate defense save state is needed. Stats displays ratings and flat subtraction, not percentages, using existing localized keys.
- Equipment Bonuses: armor and magicResistance. All three equipped necklaces contribute; removal immediately removes bonuses. Existing item effects and equipment save format are preserved. This does not unlock the reserved armor equipment sockets or add purchasable armor items.
- MonsterRewardData for each species, or its optional boss reward override: armor, magicResistance, armorPerLevel, magicResistancePerLevel and attackDamageType. Spawn initializes the receiving health component from these values. Base rating grows linearly by the authored per-level increment.
- Base ratings and monster growth default to zero. Player growth defaults to +1 armor and +1 MR per level after level 1. Miner damage/health remains unchanged; the defense pipeline covers player and monsters.

## Spawn level

The active spawn path uses RollSpawnLevel(sample, playerLevel, dayNumber), for both regular and boss variants. Default relative settings are 1-4 levels below the player, or 1-20 above; levels never fall below 1. Player level 30 therefore yields 26-29 or 31-50. At player level 1, lower rolls collapse to level 1.

Higher Level Chance and its existing daily probability increase determine the upper branch: current species settings start at 35%, add 0.01 percentage points per completed day, and cap at 50%. Both branches are uniform within their authored offset ranges. Already-spawned enemies retain their levels. Designer-facing limits remain editable; no prefab or scene was overwritten. Legacy RollLevel/GetSpawnLevel APIs remain for compatibility; the old behavior is opt-in by disabling Use Player Relative Levels.

## Verification

The original relative-level update was checked against 10,000 samples and all three species, normal and boss. Flat-defense revision has focused regression tests for physical/magic subtraction, zero-damage boundaries, true damage, capped overkill and player level growth. The full NUnit runner and Stats visual layout have not been reviewed for this revision; use Unity MCP isolated checks and Console compilation to validate without altering user save data.

SampleScene SHA256 was unchanged: 03001E88ABA4D7E07B250E2BEE1C47F597187F1A356862CCCB8DC69009A157F1.
