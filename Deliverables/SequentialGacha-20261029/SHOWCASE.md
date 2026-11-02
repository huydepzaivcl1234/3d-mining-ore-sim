# Sequential rarity / weighted rewards — 2026-10-29

Implemented in the live project `D:/3d mining sim`. Final Play Mode verification used Unity CLI 1.0.0-beta.14; no mouse/keyboard automation was needed for this final pass.

## How to edit

Select `Assets/GameData/Survivor/SurvivorUpgradeData.asset` (already selected in Unity). The Inspector has separate RED CARD (stats) and BLUE CARD (abilities) sections. Each has five tables, ordered Legendary → Epic → Rare → Uncommon → Common. Expand a table to edit entries, Weight and stat amount multiplier. Definitions contain the existing names, icons and base upgrade amounts.

Other migrated consumers:
- `Assets/GameData/Items/MiningItemDatabase.asset`: item drops after their existing source-drop gate.
- `Assets/GameData/Shop/MiningShopData.asset`: wheel rewards; prices and spin timing preserved.
- Gift-box item assets: Gift Rarity Chances and rewards grouped by rarity. Item rarity comes from the referenced item; currency uses Currency Rarity.
- `Assets/GameData/Cards/CardDrops.asset`: blue ability card reference and abilityCardPercent (currently 20%). This controls blue versus red among successful card drops, not the rarity roll or monster drop chance.

## Exact rules

Each populated tier gets its own independent conditional check: Legendary 1%, then Epic 5%, then Rare 3%, then Uncommon 17%. Stop at the first successful tier. Common is 100% fallback. These four values are editable per reward pool.

With every tier populated, Epic's total probability is 99% × 5% = 4.95%. These are conditional gates, not a flat percentage distribution.

After selecting a tier, select one eligible entry by weight / total eligible weight in that tier. Five entries with weight 1 each are 20% each within that tier. Weights 1 and 3 give 25% and 75%. Adding entries changes these within-tier shares but does not change configured rarity gates. Invalid/zero weights are excluded.

Empty or fully exhausted tiers are skipped. Cards at level 99 are excluded, and a panel does not repeat the same card ID. Keep eligible Common rewards for guaranteed fallback: if Common is empty and all remaining higher gates miss, the roll returns no reward, with no gift consumption or shop charge. No forced higher-rarity result is substituted.

The 12 existing stat upgrades populate all five tables; amount multipliers default to 1/2/3/4/5 and are editable. The six existing ability effects populate Common only because the supplied game document had no higher-rarity ability definitions. Higher tables remain editable; new gameplay effects still require their implementation. Existing upgrade IDs/progress, icons, effects and user combat tuning were preserved. Legacy FPS reward boxes remain disabled.

## Verification

- `RulesValidation.json`: passed. 400,000 before/after rolls; adding 100 Common entries preserved every seeded rarity outcome. Sequential stopping, equal/unequal weights, single-entry tables, invalid weights, huge weights, empty tables, item/gift/shop consumers and card cap/duplicate filters passed.
- `PlayValidation.json`: all Boolean checks passed. Three blue choice buttons each applied an ability level; red offered only stats; spawned blue card used its blue face material; direct world-card collection opened abilities; Skip resumed time. 1,008 game frames advanced.
- `Blue-Ability-Offers.png`: personally inspected earlier in the session; preserved existing layout/fonts and showed ability choices. The user will inspect the Inspector themselves.
- Final CLI compile status: not compiling, compilationFailed=false. Current Unity Console reported 0 errors / 0 warnings during the final pass. CLI history also retains earlier failed evaluation/compiler/input assertions; these are historical and are not a claim that the whole project has been audited.
- Test saves used a separate temporary profile. After Play Mode stopped, testStore was cleared, timeScale restored to 1, audio unpaused and assets saved. Unity was left in Edit Mode with SurvivorUpgradeData selected.
- Standalone build and unrelated gameplay systems were not tested.

`Before/` folders contain backups. Implementation/migration helper scripts are archived for review; do not rerun the initial patch script against the already-migrated project.
