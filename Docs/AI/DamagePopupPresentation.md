# Damage popup presentation — 2026-10-16

Damage numbers now expand quickly from a smaller size, settle, rise to an apex, fall along a curved arc, drift sideways, rotate slightly and fade smoothly. Independent spawn jitter, travel variation and tilt separate repeated hits. They are detached world text, not combined damage totals.

Tune the existing `Assets/GameData/Player/Weapons/BlueGunUpgradeData.asset` > Damage Popup section. It now uses the existing Fredoka SemiBold font, gold fill and dark outline. Existing duration/rise/fall/apex/punch/fade controls are retained; added initial/end scale, spawn spread, motion variation, tilt, spin and an optional font material preset.

Each pooled popup owns/reuses a font material and explicitly refreshes its glyph mesh. Atlas/material setup is reapplied on reuse, with probes/shadows disabled for the number. Actual rendered test numbers no longer appeared as magenta rectangles.

MiningCharacterHealth.AnyDamaged now supplies the actual damage removed. GunSpecialUpgrades observes it for ordinary monsters and God Boss, producing exactly one popup per damage event, including DOT, splash, nail, other sources and Bullet Fly. Removed duplicate manual popup calls from individual procs. The Bullet Fly self-proc guard still runs after displaying its hit number. Lethal hits also display their actual damage.

The visible budget is retained for rendering cost, but a new hit replaces the oldest popup when full rather than dropping the new number. Popups/materials are pooled and reused. Disabling a popup removes its active entry; pause uses scaled gameplay time. No scene edits required.

Validation in connected Editor: compilation passed. A separate PopupChecksFixture save and runtime data clone verified 10 damage events -> 10 separate numbers, random starting positions, valid atlases, pop expansion, rise, fade, arc apex 0.95m/end -0.45m, expiry, newest-number retention at capacity, disable cleanup, no duplicate gun-hit number, additional-source/DOT stacking and pause. Rendered early/late frames were inspected. The existing Blue Box test also checks that firing rate, homing damage and new-day ammo refill remain functional.
