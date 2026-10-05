# Armor, magic resistance and player-relative monster levels

## Damage pipeline

MiningCharacterHealth remains the health owner. CombatDamageType distinguishes Physical, Magic and True damage. Old single-argument damage methods remain available and mean Physical. Direct player attacks use PlayerStatsData.attackDamageType; monster attacks against the player use that species' MonsterRewardData.attackDamageType. Burn ticks use Magic. The explicit Editor Test Death command uses True damage.

Received damage = incoming damage * ResistanceScale / (ResistanceScale + relevant resistance). Armor applies to Physical; Magic Resistance applies to Magic; True bypasses both. Default scale is 100, so 100 rating reduces damage by 50%, 300 by 75%. Negative/non-finite inputs are rejected or treated as zero. Life steal, damage popups and boss damage thresholds use actual HP removed after mitigation and overkill clamping.

## Authoring

- PlayerStatsData: armor, magicResistance, resistanceScale and attackDamageType. Stats displays both ratings and their reduction percentages using localized keys.
- Equipment Bonuses: armor and magicResistance. All three equipped necklaces contribute; removal immediately removes bonuses. Existing item effects and equipment save format are preserved. This does not unlock the reserved armor equipment sockets or add purchasable armor items.
- MonsterRewardData for each species, or its optional boss reward override: armor, magicResistance, armorPerLevel, magicResistancePerLevel, resistanceScale and attackDamageType. Spawn initializes the receiving health component from these values. Base rating grows linearly by the authored per-level increment.
- New ratings default to zero: existing combat remains numerically unchanged until ratings are authored. Miner damage/health remains unchanged; the defense pipeline covers player and monsters.

## Spawn level

The active spawn path uses RollSpawnLevel(sample, playerLevel, dayNumber), for both regular and boss variants. Default relative settings are 1-4 levels below the player, or 1-20 above; levels never fall below 1. Player level 30 therefore yields 26-29 or 31-50. At player level 1, lower rolls collapse to level 1.

Higher Level Chance and its existing daily probability increase determine the upper branch: current species settings start at 35%, add 0.01 percentage points per completed day, and cap at 50%. Both branches are uniform within their authored offset ranges. Already-spawned enemies retain their levels. Designer-facing limits remain editable; no prefab or scene was overwritten. Legacy RollLevel/GetSpawnLevel APIs remain for compatibility; the old behavior is opt-in by disabling Use Player Relative Levels.

## Verification

Unity MCP confirmed compilation with no Console errors. Checks covered separate resistance types, true damage, overkill, dead/invulnerable actors, 10,000 relative-level samples including player level 1, daily probability (35% to 50%), and integer boundaries. Play Mode checks confirmed player baseline plus equipment aggregation, immediate removal on unequip, a real burn tick through MR, and spawn initialization/mitigation on Mushroom, Golem and Bat normal and boss prefabs using isolated temporary actors. User data was not reset or edited by these checks. Stats body text contains both localized defense rows; full in-game layout review was not performed because the main menu was active. Regression tests are included under the existing UNITY_INCLUDE_TESTS guard.

SampleScene SHA256 was unchanged: 03001E88ABA4D7E07B250E2BEE1C47F597187F1A356862CCCB8DC69009A157F1.
