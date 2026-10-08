# Armor, magic resistance and player-relative monster levels

## Damage pipeline

MiningCharacterHealth remains the health owner. CombatDamageType distinguishes Physical, Magic and True damage. Old single-argument damage methods remain available and mean Physical. Direct player attacks use PlayerStatsData.attackDamageType; monster attacks against the player use that species' MonsterRewardData.attackDamageType. Burn ticks use Magic. The explicit Editor Test Death command uses True damage.

Received damage = incoming damage * resistanceScale / (resistanceScale + relevant resistance). Default scale 100 follows the LoL positive-armor/MR curve. Armor applies to Physical; Magic Resistance applies to Magic; True bypasses both. 100 rating reduces damage by 50%, 300 by 75%, and 900 by 90%. A weak 1-damage hit against 900 armor deals 0.1 damage instead of being blocked. No arbitrary minimum damage is added. Negative/non-finite inputs remain rejected or treated as zero; this project does not add negative resistance or penetration mechanics. Life steal, damage popups and boss damage thresholds use actual HP removed after mitigation and overkill clamping. resistanceScale is editable again; leave it at 100 to keep the LoL-style curve.

## Authoring

- PlayerStatsData: armor, magicResistance, armorPerLevel, magicResistancePerLevel, resistanceScale and attackDamageType. Player defense = base + growth * (current level - 1) + equipped item bonuses. Both growth values default to 1 and are editable. Current/restored level drives defenses directly, so no separate defense save state is needed. Stats displays ratings and percentage reduction using existing localized keys.
- Equipment Bonuses: armor and magicResistance. All three equipped necklaces contribute; removal immediately removes bonuses. Existing item effects and equipment save format are preserved. This does not unlock the reserved armor equipment sockets or add purchasable armor items.
- MonsterRewardData for each species, or its optional boss reward override: armor, magicResistance, armorPerLevel, magicResistancePerLevel, resistanceScale and attackDamageType. Spawn initializes the receiving health component from these values. Base rating grows linearly by the authored per-level increment.
- Base ratings and monster growth default to zero. Player growth defaults to +1 armor and +1 MR per level after level 1. Miner damage/health remains unchanged; the defense pipeline covers player and monsters.

## Spawn level

The active spawn path uses RollSpawnLevel(sample, playerLevel, dayNumber), for both regular and boss variants. Default relative settings are 1-4 levels below the player, or 1-20 above; levels never fall below 1. Player level 30 therefore yields 26-29 or 31-50. At player level 1, lower rolls collapse to level 1.

Higher Level Chance and its existing daily probability increase determine the upper branch: current species settings start at 35%, add 0.01 percentage points per completed day, and cap at 50%. Both branches are uniform within their authored offset ranges. Already-spawned enemies retain their levels. Designer-facing limits remain editable; no prefab or scene was overwritten. Legacy RollLevel/GetSpawnLevel APIs remain for compatibility; the old behavior is opt-in by disabling Use Player Relative Levels.

## Verification

The original relative-level update was checked against 10,000 samples and all three species, normal and boss. Percentage-defense revision includes regression tests for physical/magic curves, weak damage against high resistance, editable scale, true damage, capped overkill and player level growth. The full NUnit runner and Stats visual layout have not been reviewed for this revision; use Unity MCP isolated checks and Console compilation to validate without altering user save data.

Unity MCP confirmed this revision compiled with zero Console errors. Isolated preview-scene checks passed: 100 physical damage against 100 armor removes 50 HP; 100 magic damage against 300 MR removes 25 HP; 1 physical damage against 900 armor removes 0.1 HP; 1 magic damage against 9900 MR removes 0.01 HP. Checks also covered true damage/overkill, invalid input, editable scale, Stats reduction fraction, player growth at level 6 and restoring level 1. No Play Mode session or full NUnit run was performed for this revision, and no user save or scene was changed.

SampleScene SHA256 was unchanged: 03001E88ABA4D7E07B250E2BEE1C47F597187F1A356862CCCB8DC69009A157F1.
