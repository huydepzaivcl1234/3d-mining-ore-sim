# Monster card drops

## Setup and authoring

Run `Mining Simulator > Setup > Card Drops And Choices` in Edit Mode if importing this ZIP into another checkout. The live SampleScene has already been configured; press Ctrl+S to save its authored Card Choice Panel. SampleScene is intentionally excluded from this ZIP.

Select `Assets/GameData/Cards/CardDrops.asset`:

- Monster Drop Percent and Boss Drop Percent: separate chance to drop one card on a confirmed death.
- Tiers: prefab, beam color, rarity Weight, Damage, Attack Speed and Health for each card. Weights are relative among eligible prefab entries, not independent drop percentages.
- Default mappings: RedCard/common, GreenCard/uncommon, GoldCard/rare; rare defaults grant larger bonuses.
- Damage, Attack Speed and Health are percentage points. 10 means +10%. Choice labels show the percent. Percentages add rather than compound: +10% and +5% means +15% of the current level-scaled base stat. The default AS bonuses are 3%, 6%, 12%.
- Spawn Height, impulses, scale, Pickup Radius/Delay, Beam Height/Width/Material are editable.
- Grounded Card Hover: bottom clearance (Hover Height), Bob Amplitude/Frequency, Rotation Degrees Per Second, Hover Lift Duration and landing layer/normal filter. After landing, physics freezes, the blocking box is disabled, the card stands upright and lifts smoothly into a stationary bob/spin. Pickup measures proximity to its ground anchor, not the raised visual. It never homes toward the player.
- Panel dimensions, font and colors are editable defaults. Existing authored panel layout is not overwritten when the setup tool runs again. Button Color/hover colors and SmoothButtonPunch settings can be edited on the scene buttons.

Walk close to a landed card to pick it up. Cards do not use MiningWorldItem or its attraction behavior; they remain at their physical landing position until pickup or scene unload. The light pillar remains vertical during the throw and matches the tier color. New cards cannot be picked up while another modal is open, while a choice is pending, or when the player is dead.

Pick exactly one of Damage / Attack Speed / Max Health. Bonuses accumulate in MiningPlayerStats, affect the existing combat Damage/AttackSpeed and health maximum, and are saved with player progress. Save version 3 stores new percentage bonuses; old version 1/2 saves remain supported and their previously earned flat bonuses are preserved separately. Reset Data clears both through the existing ResetSavedProgress path. Base PlayerStatsData is not mutated. As with other world loot, uncollected cards and a not-yet-chosen pending card are not persisted across closing the game.

The shared MushroomMonster death hook covers both Mushroom and Golem and their boss variants. Boss timeout/despawn does not count as a kill and does not drop cards. No cards are added to ore/lucky-block drop hooks.

## Shared rewards and species unlocks

- Monster gold: reward data × monster-level/boss multiplier × Money Reward upgrade × permanent/achievement money × item money buff. Uses the same money upgrade calculation as ore rewards. Popups show the actual credited amount.
- Monster player XP: reward data × boss XP multiplier × existing NPC Experience upgrade × permanent/achievement experience. MiningPlayerStats.AddExperience then applies GameManager PlayerXpMultiplier exactly once. This extends the existing XP upgrade; no parallel upgrade/currency is introduced.
- On the Mining Monster Spawner, expand Monsters/Element and set Minimum Player Level for each prefab. Existing entries default to 1, preserving current availability. Locked species are excluded from the daily weighted pool and rechecked before spawning. Level resets skip now-invalid planned slots without blocking other waves. Species unlocked during a day enter the next daily forecast; the current forecast is not rewritten.
- Debug Ignore Species Level is explicit, disabled by default, and does not modify saves. Boss level/debug settings remain separate. DebugSpawnBoss uses the registered species entry so it cannot silently bypass its level gate.
- Minimum Player Level is the ordinary-species gate, not only a boss gate. For example, setting Golem's entry to level 3 blocks ordinary Golems before 3; its boss still waits for both the species gate and its Boss Minimum Player Level.
- Show Unlock Notifications queues localized messages through the existing MiningUnlockNotifier toast (its fade/pop timings remain in MiningUiData). Display Name on each entry optionally overrides the localized prefab name. Normal-species and boss unlocks are separate messages. Multiple thresholds crossed in one XP gain are all queued. Disabled entries (Chance 0), debug bypasses and already-unlocked species on save load do not produce fake unlock messages. A level decrease/reset establishes a new baseline without announcements.

## Validation

Unity MCP, Unity 6000.5.3f1, D:/3d mining sim:

- Editor compilation and final Play Console: no errors.
- Three tier prefabs launched onto an isolated test platform in Play Mode; all switched to kinematic, disabled their box and hovered above the floor. Two samples showed changing height and rotation. Positioned screenshot inspected: upright red/green/gold cards, vertical matching pillars, no attraction.
- Isolated player fixture with saveBlocked=true and non-asset PlayerStatsData: +10% gave damage 10 -> 11, attack speed 2 -> 2.2 and health 100 -> 110. No test wrote actual player bonuses.
- Species gate fixture: requirement 5 rejected level 1, accepted level 5.
- Isolated upgrade fixture: base gold 10, two Money Reward stacks and permanent multiplier 2 returned 22; base XP 10 and three XP upgrade stacks returned 11.5, matching the existing upgrade data.
- Save DTO compatibility: v2 flat damage 6 retained, missing percent defaulted to 0; v3 damage percent 10 deserialized correctly. Quit/relaunch persistence and manual mouse playtesting were not performed.
- Unlock notification follow-up: isolated save-blocked player, ordinary Mushroom requirement 3 and Golem requirement 4. Jump 1 -> 4 displayed the Mushroom message and queued Golem; repeated check and level decrease queued no extra messages. Chance-zero entry produced no message. Separate 3 -> 5 test queued Mushroom boss at its authored requirement 5. Actual spawner resolved the existing notifier, and its TMP label was active in the hierarchy.
- MCP screenshot capture produced Editor PlayerLoop recursive-call errors; screenshots did not validate overlay rendering. Clean Play Mode run without capture reported zero errors. Native/manual visual playtest remains outstanding.
