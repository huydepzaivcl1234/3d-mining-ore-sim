# Currency restoration, gun procs and blue round reward — 2026-10-14

## Scene

Restored the authored Currency HUD, Gem HUD, Shop Panel, Shop Menu Button and MiningShopPanel from the pre-cleanup backup. Serialized references were remapped to current wallet, inventory, coordinator and cloned UI; the temporary imported backup scene was removed after restoration. Active Item Effects/MiningEffectToast remained intact. Chest, legacy mining upgrade stations and rebirth systems remain removed. Edit Mode roots are still PlayerArmature and Map, with zero missing script components.

The optional 90x90 terrain overlapped the main 200x200 terrain at Y=-0.01; both renderers/colliders were enabled. Disabled only `Map/MosswoodDefenseMap/Optional Ground - disable over existing Terrain`. Original terrain, forest, rocks and navigation remain.

SampleScene was not overwritten. Current edited scene remains open and dirty. Copy: `C:/Users/huyancut/Documents/ChatGPT/mining simulator 3d/AgentPreviews/AfterBlueGunUpgrades.unity`. User can review and Ctrl+S.

## Gun data and effects

`Assets/GameData/Player/Weapons/BlueGunUpgradeData.asset` owns the blue SM_LootBox_01 model, pickup radius/delay/scale, splash radius/fraction, bleed fraction/duration/interval, nail percent and popup settings.

- Double hit: direct gun hit applies 30% of falloff-adjusted raw bullet damage to the nearest other living monster within 8 m. Physical resistance applies. Secondary hit does not recursively splash or create another bleed application.
- Sharp rounds: physical bleed does 35% of raw bullet damage each tick. Default 1 second interval, 3 second duration; both editable. Rapid hits refresh one strongest bleed rather than creating unlimited parallel timers.
- Nail: first surviving direct gun hit marks the victim with one stack. Every subsequent received damage event on that marked living victim applies stack count × 0.0001% of victim maximum HP as magic damage, then increases stacks if it still lives. No gameplay stack cap. Nail's own magic damage does not recursively proc itself; other damage, including bleed, advances the stacks.

GunSpecialUpgrades persists unlocks separately under `Player.GunSpecialUpgrades.v1`, with corruption/version preservation. Each ability is unlocked once; already-owned choices are disabled, and no further useless boxes spawn after all three unlock. The same player-owned OnBulletHit API can be called by future guns.

PistolShooting uses actual damage returned by health for its direct damage popup. Splash, bleed and nail show their own actual damage values. MiningDamagePopup is bounded by maximumVisible (64 default) and reuses popup objects; extremely small damage is not rounded to literal zero. Health death notification is owned by the hit crossing zero, preventing duplicate Died events from nested lethal procs.

## Blue box lifecycle

MonsterSpawnZone emits one PeriodCleared event per day/period only after at least one spawn and kill, no living owned monsters, no future ordinary waves for this period, and no eligible pending night thief/skull encounters. Box drops at the last defeated monster, falls 1.2 m onto the sampled ground, then can be picked up. Pickup reuses the card choice panel and existing time/controller/audio/cursor pause ownership; selecting one ability resumes prior state. Red cards remain available and reset their own colors/labels.

## Input/performance

Mouse pointer delta was multiplied by smoothDeltaTime, increasing angular displacement on slow frames. Mouse look now uses fixed 1/60 scaling to preserve old 60 Hz sensitivity; gamepad stick remains time-scaled. No sensitivity setting reset, controller replacement, collision layer changes or package changes.

Controlled Editor walk into UNS_Standard_Rock_03, same probe:

- Baseline: 196 samples, mean 26.58 ms, P95 30.47 ms; max angular gain 1.888 degrees per input unit.
- Candidate: 442 samples, mean 14.18 ms, P95 10.26 ms; max angular gain 0.365 degrees per input unit.

These are bounded Editor captures including occasional tool/startup overhead, not a full performance certification. They support the mouse scaling correction and removal of duplicate terrain, not a claim that every close-up foliage/rendering spike is eliminated. No active MeshColliders were found in this scene; simple colliders and overlapping terrain were inspected rather than assuming high-poly collision meshes.

## Validation

- Unity compilation completed successfully.
- Authored shop button opened the original shop and gold/gem values bound to wallet; visually inspected shop screenshot.
- Isolated tests passed: 30% splash, 3 × 35 damage bleed, nail stacks 1→2→3, one lethal proc death event, choice pause/resume, unlock save/reload.
- Real gun raycast hit test: fired, HP loss 1.01000977, popup active, passed. Popup test searches DontSave pooled objects explicitly.
- Round test: no premature box with living monsters; one box after actual final-monster kill; no repeat during the sample. Future waves were closed only in the test harness to create the bounded final-wave condition.
- Final build's real box pickup opened three blue choices and Time.timeScale=0; selecting through button restored scale=1. Inspected screenshot `AgentPreviews/BlueGunChoice.png`.
- Reward/progression tests used separate GameSave test stores. Test store remained active through Play exit, then was cleared; real profile was not reset or overwritten with test unlocks.
- Final scene copy contains two authored roots and no missing scripts. Console showed no new errors after baseline cursor 4102 during validation (historical unrelated warnings remain).

## Limits

Falling boxes use a lightweight visual fall to sampled ground, not dynamic Rigidbody collision. Splash radius is explicit; it does not perform a wall-occlusion test. Bleed/nail use the existing health resistance rules. Enemy effect timers/marks are runtime-only and do not survive enemy destruction. Blue box claim state is per active encounter period, not a persistent anti-replay ledger. Current user-selected bleed duration was unspecified, so 3 seconds is an editable default, not a locked balance decision.
