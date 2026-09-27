# Fists, sword, and cinematic VFX

## Setup

Ready-made PunchStrike, PunchImpact, SwordStrike and SwordImpact prefabs are
in Assets/FX/WeaponFeedback and assigned to the existing Fists/Sword data.
Edit their colors, widths and particle settings directly in their prefabs.
Mining Simulator > Setup > Create And Assign Weapon VFX creates missing assets
and fills empty references only; it does not replace authored scenes or Animator.

The hit sector now measures angle and range horizontally, with a separate
editable hitHalfHeight on each weapon. The previous 3D angle rejected short
mushrooms below the fist even when directly ahead. Gizmos use the same sector
and height limits. Generated strike VFX use the current weapon range/angle;
impact VFX are spawned only on actual damage, not merely on a swing.

Run Mining Simulator > Setup > Create Fists And Sword Data.
Assets appear in Assets/GameData/Weapons/Fists.asset and Sword.asset.
Only an empty PlayerStatsData.defaultWeapon is set to Fists; existing data is preserved.
Without weapon data, combat also defaults to a narrow single-target punch.

Fists: nearest living target in a 30-degree forward sector, one target per strike.
Sword: all living targets in a 110-degree forward sector, each character once
even when it has multiple colliders. Range and angle are editable.
Damage = level-scaled player damage * weapon damageMultiplier.
Animation speed = player attackSpeed * weapon animationSpeedMultiplier.
Hit Time is normalized Attack time; damage and strike VFX share that time.

Set PlayerStatsData.defaultWeapon to Sword to test sword targeting. This does
not automatically create a sword model or supply a sword animation.
For a future inventory call PlayerCombatInput.TryEquipWeapon(data). It returns
false during an attack/transition: defer the inventory change and retry after
the strike rather than changing its damage halfway through. Passing null unequips.
Equipment persistence and inventory UI are not implemented by this patch.

Use an Animator Override Controller based on the existing Player controller to
replace Attack/CombatIdle clips for a sword. Assign it to animationOverrides.
Keep combat layer, Attack/CombatIdle states and parameters unchanged. Do not
swap the model Avatar merely to use a different Humanoid attack clip.

## Cinematic punch prefab (Particle System, URP)

1. Create an empty PunchStrike. Use +Z as forward. Add a child Particle System;
   disable Looping, enable Play On Awake, Simulation Space = World, Duration 0.15.
2. Disable Rate over Time; use one Burst at time 0: 5-8 particles. Start Lifetime
   0.08-0.14, Start Speed 1-2, Start Size 0.06-0.12. Use a narrow Cone (angle 5-10).
3. Use a URP Particles/Unlit additive material with a soft streak texture. Renderer
   = Stretched Billboard; point the streak forward. Size over Lifetime quickly
   expands then contracts; Color over Lifetime fades alpha to zero.
4. Make PunchImpact separately: burst 8-12 short sparks and one expanding soft
   ring, lifetime 0.08-0.2. Keep it small (roughly 0.15-0.3 world units), not a giant explosion.
5. Save both as prefabs. Assign Fists.strikeVfxPrefab and impactVfxPrefab. Strike
   spawns in front at contact time; impact spawns only when damage hits a target.
6. Scrub the punch in Animation preview: find the frame of full fist extension.
   Hit Time = contact time / clip length. Tune this before changing particle delay.
7. Punching straight depends on the clip/upper-body mask as well as damage geometry.
   This patch changes hit selection, not the existing punch pose or mask.

## Cinematic sword

1. Attach the sword model to the hand bone. Use a real sword slash clip via the
   weapon override controller; set Hit Time at blade contact, not wind-up.
2. For a true blade-following ribbon, put a Trail Renderer on the blade tip:
   time 0.08-0.12, min vertex distance 0.02, width 0.06-0.12, emissive/additive
   gradient with alpha fading to zero. Normally keep Emitting off.
3. Add presentation-only animation events for trail start/end around the fast
   slash frames, with receiver methods on the same object as Animator. The current
   patch does NOT automatically enable a blade-tip Trail Renderer or supply these
   callbacks. Never keep it emitting during walking; never deal damage from them.
4. For the already-integrated prefab path, author SwordStrike with a crescent mesh
   or shaped particle burst aligned to the slash plane. Duration 0.12-0.2,
   no loop, Play On Awake. Assign Sword.strikeVfxPrefab.
5. Make SwordImpact with one tiny contact flash plus 10-16 sparks, lifetime
   0.1-0.25. Assign impactVfxPrefab: each struck enemy receives its own effect.
6. Start with restrained emission and optional Bloom on the existing Volume.
   Motion, timing and contrast matter more than excessive glow. Camera impulse
   and hit stop are optional later work, not enabled by this patch.

## Validation

Provided EditMode tests cover single target, sweep, duplicate colliders, rear/
out-of-range targets and unequipping. Run them through Unity Test Runner.
Manually test attack while moving, fast attacks, disable/respawn and equipment
changes. Ensure custom VFX lifetime exceeds longest particle lifetime; prefabs
must reference their materials so shaders are included in builds.
Editor compilation completed without errors. Controlled Editor regression:
the old fist left the low target at 100/100; the fix reduced it to 90/100.
Single-target selection, sword sweep and multi-collider deduplication passed.
In Play Mode the real Player Animator's Attack state reduced the actual
mushroom prefab from 10 to 9 HP and spawned PunchStrike/PunchImpact. The
temporary runtime probe was removed afterward. Keyboard/mouse injection was
not conclusively verified. NUnit tests were added but not run through Test Runner.
A sword model/override animation and full inventory remain separate work.
