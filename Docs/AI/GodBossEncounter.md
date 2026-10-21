# God Boss encounter and rifle reload — 2026-10-15

## Configuration

- `Assets/GameData/Monsters/GodBoss/GodBossData.asset`: separate encounter/attack data. Assigned to the active MonsterSpawnZone; save the authored scene with Ctrl+S to persist that assignment.
- Every 50 days by default. One primary zone spawns the God; ordinary scheduled monsters are suppressed. Roar summons are intentional exceptions. A living God persists across the day boundary until defeated.
- Punch is faster than swipe. Melee lunges, punch knockback, swipe true-damage bleed (1% player maximum HP each second for two seconds), committed jump after 0.5 seconds, and expanding landing shockwave are implemented.
- Roar only rolls while injured, heals 5% maximum HP, and summons each configured species plus its boss variant. Defaults cap living summons at 24; blocked terrain can prevent individual summons. Missing boss HP increases summoned health/damage.
- Run/walk thresholds, height, health, defenses, damage, ranges, attack speeds, healing, summon multipliers, cooldowns and bleed settings are editable in GodBossData.
- Killing a credited God on day 50 or a later configured encounter unlocks rifle. Ordinary sessions still start with pistol. World reset clears the encounter and bleed with existing progression reset.
- New completion key: `World.GodBoss.DefeatedDay.v1`; the existing rifle unlock key is retained. Restarting an unfinished encounter starts that boss at full health.

## Model and animation

Nine user-provided Mutant FBXs are under `Models`. The base Humanoid Avatar needed an explicit LeftHand mapping. The new prefab's visual hierarchy is unpacked from the model prefab so its Animator/controller/scale survive model reimport; meshes still reference the imported assets.

The controller contains Idle, Walk, Run, Punch, Swipe, Jump, Roar and Dying. Gameplay owns locomotion and leap translation; root motion is disabled. The leap clip is time-scaled to match configured flight time. Review its fast timing in normal gameplay when tuning jump speed.

## Rifle

`Assets/GameData/Player/Weapons/Rifle/RifleReloadProfile.asset` and `RifleReload.anim` animate magazine/weapon offsets and hand release using the existing full-body presentation. This model has no separate slide assignment, so reload now supports magazine-only mechanisms. Existing pistol slide behavior is retained. Rifle recoil settings were reduced/tuned in RifleWeaponData.

This is an independently authored approximation using the existing system, not copied commercial animations and not verified as identical to FPS Animation Framework. The clip is editable; it is not a complete new humanoid hand-animation asset.

## Validation and limits

Connected Unity Editor compilation passed. Isolated Play Mode save fixtures were used, not a reset of the player's real save.

- One God, ordinary spawn suppression and 50/100-day eligibility checked.
- Committed jump tested after player re-entered melee range; approximately 0.50-second windup and 0.65-second flight.
- Roar restored 250 of 5000 maximum HP; spawned 12 minions including six boss variants.
- Credited death unlocked/equipped rifle and did not respawn the same-day God.
- All eight animation states were sampled on the actual Humanoid prefab; left-hand movement was nonzero and rendered Punch/Roar views inspected.
- Empty rifle reload completed at 30/30, reserve 120 to 90; magazine travel about 0.316m and measured magazine grip error about 0.0012m.
- Bleed used to lose the final tick through floating-point subtraction; replaced with capped elapsed time and integer tick boundaries.

Still needs subjective combat balancing and manual sustained playtesting: every obstacle/jump landing, knockback feel, shockwave visual polish, and rifle motion at every view angle. Shockwave currently uses a simple flat expanding ring, not a terrain-conforming effect. Existing daily forecast does not yet have a dedicated God row; God has its own world-space health bar. No claim of complete visual parity with the demo.

## Delivery

ZIP preserves project-relative paths and excludes SampleScene.unity. On another copy of the project, assign GodBossData to the MonsterSpawnZone's God Boss encounter field. Existing project dependencies (health bar assets, locomotion, rifle model/loadout) must already be present.
