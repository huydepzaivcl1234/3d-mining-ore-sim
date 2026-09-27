# Player hit reaction and health-bar effects

## Apply the opt-in setup

Leave Play Mode, select Player, then run:
`Mining Simulator > Setup > Player Hit Reaction (Keep Attacking)`.
Save the scene. ZIP delivery does not include SampleScene.

The tool copies the CURRENT assigned controller, including its existing death,
movement and combat states. It never changes vendor controllers or source FBX
clips. Re-running with the assigned hit controller preserves authored states,
transitions, masks, clips and existing layer weights.

## Edit animations in Animator

Layer order: Base Layer, Hit Reaction, combat layer. Hit Reaction uses Override,
default weight 0 and an upper-body mask. Its weight fades up to 1 only while the
HitReaction state runs, then releases to 0; Empty cannot leave a stale pose.
Root, legs and foot/hand IK are excluded; body,
head, arms and fingers are included. Locomotion keeps control of the legs.

Parameters: `Hit` is a Trigger. Graph:

- Entry -> Empty (default, no Motion, Write Defaults off).
- Any State -> HitReaction: Hit trigger, Has Exit Time off, Fixed Duration on,
  duration 0.06 seconds, Can Transition To Self on for repeated hits.
- HitReaction -> Empty: Has Exit Time on, Exit Time 1, Fixed Duration on,
  duration 0.1 seconds, no conditions.
- HitReaction Motion defaults to the existing HumanM@CombatDamage01 clip.
  Replace Motion here, not on the gameplay component. Use a non-looping humanoid clip.
  Keep Write Defaults off in this layer.

`MiningHitReaction` listens to health damage and sends Hit; it does not disable
input or movement, change CombatMode, reset Attack, or pause attack timing.
The combat layer is above Hit Reaction: attacking wins on shared torso/arm bones.
CombatIdle lowers its weight while a hit reaction plays, so it cannot hide the
reaction when standing. An active punch remains visually prioritized; showing
two full poses on the same arm simultaneously requires a separately authored
additive clip, which this setup deliberately does not invent from the normal clip.
Lethal damage skips Hit and belongs to Death. Respawn retains the existing layer
restore flow.

## Health-bar Flash versus Fill

On Player's referenced Combat Health Bar MicroBar, edit Simple Bar > Damage
Animation: Flash, plus its flash color and duration. Edit Heal Animation separately.
The old health code passed skipAnimation=true on every update, bypassing BOTH
Flash and Fill. It now uses Damage for ApplyDamage and Heal for Heal/regeneration;
only initialization and respawn snap without an animation. No authored MicroBar
effect, color, speed, layout, ghost-bar or prefab overrides are reset by code.
For advanced (non-simple) bars, edit that bar's Damage/Heal animation commands.

## Verification

Static checks: non-lethal damage uses the animation-enabled Damage path; Heal
uses Heal; spawn/respawn snap; Hit has no writes to gameplay attack or movement.
Manual checks in Editor: stand normally and in combat, take non-lethal damage,
then take damage while running and striking; attacks must still contact and
reduce enemy HP. Repeat hits, then lethal damage and respawn. Check Flash color
on damage and configured Heal effect on regeneration. Run Setup twice and
confirm one Hit Reaction layer/component and unchanged authored layout.

This change was not Play-tested or confirmed compiled in Editor in this turn:
computer control was not requested, and no Unity API provider was connected.
