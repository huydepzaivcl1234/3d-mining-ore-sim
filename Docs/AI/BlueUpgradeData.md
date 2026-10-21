# Blue Box upgrade data

Each of the eight Blue Box upgrades has its own typed ScriptableObject asset in
`Assets/GameData/Player/Weapons/BlueUpgrades/`. The existing
`BlueGunUpgradeData.asset` is now only a box configuration, damage-popup settings
and eight explicit references. Runtime ownership, choice IDs and save v1 are unchanged.
No scene changes are required. Existing balance values were copied via Unity APIs
and compared against a pre-reload JSON snapshot before saving the catalogue.

## Designer workflow

Select an asset in BlueUpgrades and edit its Inspector; changes to assets persist
even after leaving Play Mode. Do not change runtime components to edit balance.
Each type is also available under Create > Player > Blue Upgrades.
If creating a new catalogue, assign all eight assets before using it.

For the bomb ring select `GiantBomb.asset`:

- Giant Bomb Hits: damage events required to trigger.
- Giant Bomb Base Damage / Damage Type: fixed damage added to current gun damage.
- Giant Bomb Radius: maximum radius, metres.
- Giant Bomb Wave Speed: expansion speed, metres per second.
- Giant Bomb Vertical Range: vertical damage tolerance.
- Giant Bomb Width: ring thickness and swept collision margin.
- Giant Bomb Color: ring colour.
- Giant Bomb Visual Height: rendering-only offset above the emission point.
- Giant Bomb Segments: smoothness (8-128), applied to newly emitted rings.

Ring rendering remains flat, not terrain-conforming. Radius, speed, width and colour
are read live; segment buffer is reused and resized only when starting a wave.
Lean Localization continues to format descriptions from the matching data assets.

## Validation (2026-10-16)

- Live Unity Editor compiled without C# errors.
- Eight separate assets persisted after reimport; old balance and box/popup values preserved.
- Isolated-save Play Mode regression: random three unique unowned choices, legacy
  unlocks, bilingual descriptions, Hunter kill reward/cap/save, actual-damage healing,
  10th/20th-hit waves, nearby damage/range and recursion guard passed.
- Cloned-data runtime test: changed radius/speed, width, colour, visual height and
  segment count affected emitted ring geometry; live colour/width changes and
  radius termination passed. Authored assets were not changed by the test.
- Test save detached and Editor returned to Edit Mode; no scene save performed.
