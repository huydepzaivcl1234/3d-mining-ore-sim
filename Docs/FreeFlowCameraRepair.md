# Freeflow strike/camera ownership repair — 2026-10-10

## Confirmed defect

`ControlsStrikeFacing` previously stopped owning facing on the damage event
(`hitApplied`), before the strike finished. Shift-lock could then rotate the
player during recovery. The normal camera-relative walk vector was also added
to the approach movement. Attack acceptance and Animator evaluation had a
one-frame ownership gap.

## Change

- Commit facing from the accepted click through the outgoing strike blend.
- Sample input/target for each accepted strike; camera orbit is independent.
- Freeze facing at contact. Keep moving-target tracking before contact, bounded
  by the existing travel, range and obstruction checks.
- Publish approach velocity in Update before the existing CharacterController;
  wait for Animator evaluation rather than prematurely cancelling the launch.
- Do not add camera-relative locomotion or walk/run animation blend during a
  committed strike. Existing walk/run and strafe return after recovery.
- Clear unused camera/motor turn velocities at the ownership handoff.
- Reset combat ownership on disable, combat exit and gameplay interruption.

No new motor, input actions, Animator/controller rewrite, enemy behaviour,
counter mechanic or damage-range extension was introduced. Scene/save files
were not saved or migrated by this repair.

## Evidence

The contact-ownership regression test failed before the fix (expected true,
actual false), then passed. EditMode suite: 180 tests, 175 passed; the five
existing failures remain in TreasureChestTests (two) and WeaponAttackTests
(three), matching the previous baseline. FreeFlowCombatTests and
DirectionalCombatTests pass.

`DirectionalCombatPlayValidation.Begin(true)` uses the actual player's rig,
three authored sword clips, real CharacterController motor and orbit camera
in a save-free fixture. It tests held movement, 180-degree camera turns during
each strike, shift-lock changes at contact, damage events, planted recovery,
locomotion handoff, and wall rejection. The fixture restores original roots
and run-in-background state after stopping; it never saves SampleScene.

## Video reference review

Reviewed the complete automatic transcript and all 46 ten-second frame samples
of the supplied 7m35s Mix and Jam video, not continuous real-time playback.
Applied its relevant separation of selected attack target, attack-owned facing/
movement and animation-event contact. The video's counter system, HDRP setup
and additional animation set are outside this camera/strike repair.
