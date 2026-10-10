# Directional sword free-flow integration

Source reviewed: user-provided `FreeFlowCombat.zip` (CombatCore, EnemyHealth,
PlayerMotor, FreeFlowCombat). Integrated ideas into the existing combat owner,
not four parallel scripts. No new enemy layer, health component, input system
setting, Animator Controller or motor is required.

## Improvements

- Movement intent is camera-yaw-relative through StarterAssetsInputs, respecting
  existing rebinding. Without movement, acquisition prefers the forward cone.
  Score combines angle and collider distance; registry order breaks ties.
- Commit one visible, living target per strike. Each combo strike can acquire a
  different target; it does not switch enemies every frame.
- Three authored sword clips remain `Sword Attack 1`, `Sword Attack 2`,
  `Special Attack`. The separate lunge/turn opener is no longer automatically
  inserted. Existing old event receivers/states are retained for compatibility.
- One deliberate follow-up is buffered from early windup, including incoming
  transitions, until the link frame. Contact events mark misses too, so a miss
  does not prevent the next combo strike. Holding the button does not auto-chain.
- Each strike may approach; short launch easing and bounded speed/travel remain
  in CombatLungeMotion. A target-free strike takes a small .25 m step. Moving
  targets retain bounded tracking until contact; invalid/dead/occluded targets
  cancel tracking for that strike. The existing CharacterController owns Move.
- Ray checks reject acquisition and damage through solid walls. Player and
  intended target colliders are excluded, as are towers that player may traverse.
  Ray buffer saturation conservatively rejects rather than ignoring obstacles.
- Damage range, resistance, equipment bonuses, lifesteal, true bonus, impact,
  sword trail, death rewards and animation-event authority remain unchanged.
- Shift-lock camera does not overwrite committed strike facing before contact.
  Camera position/orbit is not driven toward the enemy. Draw/sheath, normal
  walk/run, directional movement and existing layers/masks are unchanged.
- Incapacitation, menus, placement, death and pause clear queued input/movement
  instead of replaying an old click on return.

## Why not copy the ZIP unchanged?

Its hardcoded input would bypass rebinding; standalone EnemyHealth bypasses
resistances/rewards; a second PlayerMotor conflicts with gravity and collision.
Its .3 s input buffer can expire before a late combo window. Its snapshot lunge
has no occlusion check or moving-target tracking. Knockback's direct transform
fallback and forced animator speed reset are unsafe for the current monster AI
and attack-speed system. Existing reactions/feedback are retained; this change
does not add the ZIP's independent knockback or animator-speed hit-stop system.

## Inspector tuning

Player -> PlayerCombatInput -> Directional free-flow targeting:
search radius 7 m, maximum approach travel 5 m, input cone 80 degrees,
idle cone 100 degrees, distance weight 6, air step .25 m.
These are maximum acquisition/travel budgets, not guaranteed hit distances:
contact timing, attack speed, speed cap and physical blockers still limit reach.
Existing lunge speed/time, clearance, combo timings and contact phases remain
editable. No scene/prefab rewrite is needed for new default fields.

## Validation (Unity 6000.5.3f1, D:/3d mining sim)

- 4 DirectionalCombatTests passed: cone/distance preference, camera-relative
  input, wall occlusion, early click/miss links exactly one follow-up.
- 20 existing FreeFlowCombatTests passed: combo/controller mapping, contact
  events, footwork mirrors, normal damage range, lunge envelope/tracking and
  respawn movement reset.
- Bounded Play fixture passed using a cloned actual player rig and controller:
  directional acquisition, actual event damage on all three clips, early-click
  combo links, capsule approach and wall rejection. Original roots/background
  restored; fixture excludes economy/save systems. No scene save/reset/build.
- Earlier fixture attempts timed out before readiness; ready gating and active
  clone initialization corrected. A pre-existing quest-authoring delayed callback
  also reported MarkSceneDirty during Play in those attempts; no new quest edits.
- No persistent/global timescale or camera settings changed. No visual claim of
  fully matching Arkham animation is made: authored clips remain the user's.
