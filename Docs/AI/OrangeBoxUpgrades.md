# Orange Box upgrades (2026-10-16)

## Data and integration

`Assets/GameData/Player/Weapons/OrangeUpgrades/OrangeBox.asset` references the
existing orange SM_LootBox_02 prefab and three separate upgrade assets. The existing
BlueGunUpgradeData asset references this catalogue; no scene/component setup or
scene saving was needed. BlueGunBoxReward now listens to the same PeriodCleared
event for a second, orange drop. Default orange chance is 100%, with a separate
offset to keep blue and orange pickups apart. Stop dropping once all three are owned.

The existing paused card-choice UI is reused: at most three random unowned upgrades,
orange colours and localized title/descriptions. Owned flags append to save v1;
existing blue IDs and the blue all-owned condition retain their meanings. Reset
through WorldProgressReset invokes the existing upgrade reset and clears orange
runtime effects as well. No upgrades or real save data were reset by installation.

## Upgrades

- Cant Die: intercepts a fatal player hit before death callbacks, leaves 1 HP,
  ignores damage/target selection while recovering toward 20% max HP over 3 seconds.
  120-second scaled-time cooldown starts on activation. Cooldown is runtime state,
  not persisted across application restarts. Health regeneration pauses during the
  recovery so it does not change its healing total; ordinary bonus healing can
  still benefit the player. Targeting resumes when recovery ends.
- Divine Cannon: default 35% per on-hit proc; visible large orange homing round adds
  30% of triggering gun damage as physical damage to the original target, not AoE.
  Chance, damage fraction, visual size/colour, speed, lifetime and concurrent budget
  are editable. Timed-out/over-budget rounds resolve their remaining damage;
  dead/despawned targets cancel. No retained projectile pool.
- Ethereal Weapon: direct shots open a non-refreshing 3-second window per victim.
  Owned effects can apply on-hit upgrades once per second during that window.
  After expiry, wait 1 second and fire a fresh direct shot to reopen; effects cannot
  reopen it. Direct shots remain normal on-hit events, not limited by the effect gate.
  See `EtherealWindowPopupBulletFlyNerf.md` for the current balance and validation.
  Cooldown is reserved before nested procs to prevent synchronous infinite loops.
  Bullet Fly, splash, bleed refresh/Nail marks and cannon share the on-hit path.
  They do not grant a second copy of all-damage effects (lifesteal, bomb hit counts).
  Without Ethereal Weapon, effect damage still cannot trigger Bullet Fly.

Direct shots now explicitly label the existing global damage notification with a
basicAttack boolean, preserving the original source and actual damage value even
through nested procs. All project subscribers were checked and updated.

## Hit reactions

MushroomMonster-based enemies keep navigation/movement during their existing Damage
clip. A pending reaction flag prevents Walk overwriting CrossFade before Animator
has evaluated the new state. Already committed jump/contact frames remain protected.
This does not add a hit-reaction clip to God Boss, whose supplied animation set has
no Damage clip; its existing movement/actions are otherwise retained.

## Validation

Live Unity Editor compilation passed. Tests used isolated GameSaveStore fixtures:

- Orange drop at 100%, no drop at 0% or after owning all orange upgrades.
- Three unique orange choices, owned exclusion, pause, unchanged blue-only choices.
- New orange unlocks survive reload; blue all-owned condition remains separate.
- Fatal-hit rescue, immunity, AI target rejection, exact 20% recovery after 3 seconds,
  120-second cooldown and actual death on another fatal hit during cooldown.
- Cannon chance 0/100 boundaries; a 100-damage primary plus a 30-damage cannon round
  removed exactly 130 HP on an undefended fixture target.
- No effect proc without Ethereal; effect proc with it; same-target lockout,
  independent target readiness, real-time reopening after 1.1 seconds, direct shot
  not duplicated, and bleed/Nail on-hit application.
- Both Lean language tables resolve all four new keys with matching placeholders.
- Real-frame movement test: 0.603m pre-hit movement, then 0.110m while Damage played.
  Same-frame manual navigation tests were not accepted as movement evidence because
  path requests require later frames to resolve.
- Orange choice UI captured and visually inspected. Screenshot used cloned cannon
  chance=100 for testing; shipped asset is confirmed at 35%.

Returned to Edit Mode, detached test store, did not save the user's dirty scene.
