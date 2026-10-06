# Continuous movement / turn correction

Baseline reproduced on the actual player in SampleScene Play: unlocked direction reversal played a stationary turn while translation continued at normal speed. Moving camera yaw could also select stationary sword turns. Previous clip-name tests did not cover continuous input/camera interaction.

## Changes

- Existing Combat Footwork layer now has a continuous eight-direction walk/run blend tree. Local direction and gait blend without restarting the animation on each direction change.
- Camera-driven stationary turns are eligible only when stationary. Starting movement interrupts them immediately. Stationary turns remain available in either camera-lock mode.
- Unlocked 180-degree reversal retains the authored turn, but translation eases from a configurable reduced fraction back to normal as the feet turn.
- CharacterController remains the sole movement owner. Locomotion's temporary movement fraction is independent of combat's movement fraction and is cleared on interruption, disable and respawn.
- Walk/run cycle reference speeds and direction damping are Inspector-editable. Authored animation events, combat damage and save formats remain unchanged.
- No SampleScene serialization or vendor animation edits.

## Verification

MCP Play testing on the actual SampleScene player: locked forward/right/back/left, walk/run and simultaneous camera yaw, with an uninterrupted Directional Movement state and advancing cycle phase; unlocked reversals show the movement envelope recovering to 1. Captures are kept under Captures, not packaged as assets. A missing-script warning already exists on the user's scene and is outside this change.

This is an Arkham-inspired movement correction, not a claim that the project's animations reproduce Arkham's entire combat system.

Isolated cloned-player regression: Lunge travelled about 1.12m, applied contact damage once and returned to movement; queued combat 1/2/Special continued applying hits. Stationary sword Turn was separately injected and interrupted with movement to check that the lower layer changed to Directional Movement. Temporary diagnostic scene/component removed before handoff.
