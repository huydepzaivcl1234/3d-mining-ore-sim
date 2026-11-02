# RPG upgrades and locked homing shots

Completed 2026-10-29 in D:/3d mining sim.

Figma (editable native layers): https://www.figma.com/design/9GC36WP5jGDsYDEeM1fjzF?node-id=2-2
Account: real figma (user selected). Native frames/text/icons, no flattened UI image and no paid image generation. Figma mockup uses Anton/Roboto; Unity retains the user's Square font and existing sprites. RPG bronze shell, three rarity-colored offers, level badges and live build summary.

## Upgrade rules

- 12 basic cards and six Common abilities from Plan Game.txt; shared cap 99 per upgrade identity, regardless of rolled quality. Maxed cards are excluded, offers contain no duplicate identities, all-max produces no offer.
- Each slot rolls independently: Common 80%, Uncommon 17%, Rare 3%. Basic-card bonus is 1x / 2x / 3x. Defaults were not specified by the doc: they are editable in Assets/GameData/Survivor/SurvivorUpgradeData.asset. These rates and amounts are initial balance assumptions.
- The doc's Uncommon ability list is empty. No invented Uncommon/Rare abilities were added; the six specified abilities roll as Common. Higher-quality BASIC cards can coexist with Common abilities in one selection.
- Basic defaults per Common selection: damage +5%, attack speed +5%, reload speed +10%, range +5%, projectile speed +5%, armor +3, magic resistance +3, move speed +5%, magazine +1 shot, life steal +1%, regeneration +1 HP per existing regen tick, max HP +5%.
- Bonuses and levels persist in existing player progression (v6); v1-v5 still load. Old gun-card damage/projectile bonuses remain compatible. No player save format reset.

## Formula interpretations

Let L be the ability level; initial constants apply at L=1, per-level additions use L-1.
- Split Shot: one other living monster in range; 20% BASE damage + 1 + 20*(L-1). No extra bolt when only one monster is present.
- Fleet Force: floor(added run speed / 20) * (1 + 10*(L-1)). Excludes base run speed; includes card and existing item move-speed buffs.
- Armor Strike: an extra physical hit of 5 + (L-1) + 2% of the player's total armor and MR.
- Life Spring: heal 1 + 5*(L-1) + 5% max HP every five seconds.
- Power Surge: add 1 + 0.5*(L-1) + 20% of physical damage before this ability's own bonus. Does not repeatedly multiply the original damage or recursively compound.
- Burn: 0.25*L + 2% of target max HP as magic damage per one-second tick. Duration defaults to three seconds, refreshed by primary hits (duration/tick unspecified by doc).
- Life steal heals from actual primary-hit damage, not theoretical damage. Existing healing modifiers remain active.

## Homing and magazines

- Acquire nearest valid unobstructed target in the existing attack radius. Every launched shot keeps that target.
- Flight duration is based on launch distance / upgraded projectile speed. The endpoint follows the locked target; sharp turns and moving out of acquisition range do not evade it. Target death/deactivation cancels the shot. Flight does not collide with unrelated enemies or new obstacles; launch selection still checks line of sight.
- Automatic magazine/reload provides real effects for reload speed and magazine cards. Defaults: nine volleys, 1.5-second reload; editable in PlayerCoreStatsData. Per-shot cadence and existing SFX remain. No FPS gun model/ADS was restored.
- Camera, orbit/zoom and actor frustum culling/freeze behavior are retained.

## Verification

- RulesValidation.json: 45,000 independent slots; Common 80.116%, Uncommon 16.942%, Rare 2.942%. Mixed quality, unique identities, all 18 cap99, max exclusion and invalid-input checks pass.
- RuntimeValidation.json: all 12 basic stats change actual gameplay; added-speed-only damage, save round trip, a locked target moving 18m, cancellation for inactive targets, split single/dual-target conditions, three magic burn ticks, armor strike hits, life steal, five-second healing and reload/refill pass.
- UiSaveValidation.json: all three buttons tested through Button PointerClick events; correct level/amount, pause/resume, Skip and old v5-save migration pass. Bindings are refreshed on every modal open to avoid stale callbacks after Editor reload.
- RPG-Offers-Final.png: actual 1920x1080 Game view, personally inspected. Both common ability text and three rarity colors are visible without clipping. Test progression shown is isolated from the real profile.
- Unity compilation and current Console checked. Existing unrelated obsolete-API warnings are not refactored; no standalone build or exhaustive balance playtest.

## Files and setup

New: Assets/GameData/Survivor/Scripts/SurvivorUpgradeData.cs and SurvivorUpgradeData.asset.
Modified: PlayerStats.cs, PlayerCoreStatsData.cs, PlayerController1.cs, SurvivorAutoCombat.cs, PlayerStatsPanel.cs, MiningCardData.cs, MiningCardSystem.cs, CardDrops.asset and SampleScene.unity.
Existing fonts, UI icon sprites, audio, damage popup/equipment/rune systems reused. No packages added.

Sources and test harnesses: C:/Users/huyancut/Documents/ChatGPT/mining simulator 3d/Deliverables/RPGUpgrades-20261029. Before/ contains source backups; BeforeScene.unity and BeforeCardDrops.asset are here. Tests used isolated profiles. Initial integration errors (multiline health expression/new-file refresh) were repaired before gameplay tests. A pointer test found an unbound runtime callback; reopening now rebinds listeners and all three PointerClick checks pass.
- Final current Console: zero errors, zero warnings; compilation succeeded. Historical tool/compile diagnostics remain in Pipeline history. Unity returned to Edit Mode and isolated save override cleared after stop.
