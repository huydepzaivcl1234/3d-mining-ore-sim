# Forest Golem uses regular Golem baseline

Supersedes the original Forest-only baseline in ForestGolem.md. ForestGolemRewards
now copies all regular Golem combat, health, regeneration, navigation, scaling,
defenses, rewards, drops, lifetime and boss settings. Attack speed is 0.75 and
death delay 2 seconds, matching the current Golem GameData. Its boss includes the
regular Golem slow skill in addition to Forest's composed ability. Self-referencing
boss loot is remapped to ForestGolemRewards so it retains its species ownership.

Forest's model, textures, rig, matching Idle/Walk/Hammer/Hit/Damage/Down clips,
ChargeUp state, identity, spawn entry and health-bar placement remain intact. The
regular clips are generic with different rig paths, so they are not assigned to
the Forest rig. State names, speed and clip durations already match. Forest's
second contact phase remains 0.54 (regular Golem 0.52) to align its actual hand
impact; the rest of the baseline combat settings match.

The CharacterController capsule, skin, step height and minimum movement distance
match Golem. Fallback health/defense/regen fields match too; Forest UI references
remain its own. No SampleScene or save-data writes.

All current Forest-specific ability settings are retained: extra wave radius
2 m, 0.65-second wave, 3 successful player hits to ChargeUp, burn 0.1 per tick,
0.1-second interval, 2-second duration. These are the user's current authored
values, not reset to older feature defaults. Material/color and release timing
are retained as well.

Validation: 99 baseline fields matched, six loot entries matched, capsule matched,
three existing Forest asset checks passed. Isolated Play verification exercises
the slam/wave deduplication, delayed travelling wave, charge after three real hits,
remote burn, repeat charge, death and disable. Wave-boundary test uses a cloned
diagnostic loadout with the original 1.2 m radius; production data stays at 2 m.
