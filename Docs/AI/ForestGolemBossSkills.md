# Forest Golem boss skills

Configure `ForestGolemRewards.asset > Combat > Forest Golem > Boss Skills`.

- Boss-only: after three completed ground-contact slams, pulse for three seconds.
- Default tick interval: one second. Ticks at 1, 2 and 3 seconds each deal 1% of the player's maximum health as true damage, ignoring armor and MR.
- Default radius: 3.6 metres, multiplied by boss skill size. The player must be inside the radius, within the height limit and have an unobstructed strike line of sight.
- Slams during an active pulse do not restart it or count toward the next burst.
- Passive: every ten seconds, heal exactly 1% of the boss's maximum health, independent of healing bonuses. Both intervals and percentages are editable.
- Death, despawn or disabling the ability cancels the pulse and resets its counters. Normal Forest Golems retain their existing behavior.
- Existing slam shockwave, charge/burn and inherited golem skills remain unchanged. The pulse reuses the existing grass shockwave prefab, with one cached visual per boss.

Validation: 20 isolated Play Mode boss-skill checks and 22 previous Forest Golem regression checks passed. Includes real animator contact callbacks, true damage against 10,000 armor/MR, range checks, configurable tick timing, terminal ticks, passive regeneration and death/disable cleanup. Tests used cloned data and a temporary diagnostic scene; SampleScene and persistent player progression were not edited. These are automated checks, not a full player-controlled encounter playtest.

Import the patch ZIP into the project root, preserving its Assets and Docs paths. The existing ForestGrassShockwave prefab/scripts from the previous impact patch are required.
