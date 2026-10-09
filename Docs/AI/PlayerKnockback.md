# Player knockdown / get-up

## Current behavior: 2026-10-09 death-only ragdoll

Landing/camera follow-up: death camera follows the actual hips while airborne,
not the stationary player root. Free spectator movement and the respawn panel
are unlocked only after a real upward-facing ground contact during descent;
walls and the feet brushing the floor at launch do not unlock it. The countdown
starts at landing, and RespawnNow cannot bypass this gate. Launch speed/lift are
editable on PlayerDeathRespawn under Death Ragdoll Launch (metres/second).
Live isolated follow-up passed with upward speed 5: airborne camera keeps its
offset to hips, panel stays hidden, early RespawnNow is rejected, ground contact
opens the panel, corpse holds, and real respawn restores controls. All 19 direct
Editor NUnit methods passed again; no Console errors, scene roots restored.

This section supersedes the old get-up implementation documented below.
Living knockback uses CharacterController displacement, never ragdoll; ordinary
hits still require explicit knockback configuration. Death launches the 15-body
Humanoid ragdoll away from the last damage source. After ground contact and settled
velocity it becomes kinematic in its actual landed pose until respawn. Animator is
disabled during death; real respawn restores bones, Animator and the capsule.
PlayerDeathRespawn owns input locks and its existing respawn countdown/camera.
Lethal Launch Speed (6) and Lethal Launch Lift (3) are editable there.

Death and Getting Up states were removed from the player Animator controller.
The Get Up and two player death FBXs/metas were moved out of Assets into
`Temp/RetiredPlayerAnimations-20261009` for recovery. The dirty SampleScene was not
saved or rewritten to strip obsolete serialized clip fields. Original Paladin,
sword draw/sheath, living locomotion and player sword trails are retained.

Validation: 19 NUnit test methods passed by direct Editor invocation (not the
Test Runner). Isolated live validation covered living knockback, lethal launch,
landed corpse held for one second, and real respawn. Corpse render inspected at
`Temp/PlayerDeathReview/landed-corpse.png`. Scene roots are restored after checks;
save/progression systems do not run. No full combat crowd/uneven-terrain soak.

The original Paladin player is retained. Smiley is not restored. Existing movement,
combat controller states, sword draw/sheath sockets and events are unchanged.

`PlayerKnockbackRagdoll.ApplyKnockback(worldVelocity)` starts a living player's
knockdown. Ordinary damage does not start it automatically. Golem and Forest Golem
melee have opt-in `combat.knockbackSpeed = 5`, `combat.knockbackLift = 3` in their
existing GameData. Set speed to 0 to disable, or tune these separately per species.
Chest hits, projectile attacks and shockwave/burn ticks do not launch the player.

Only one motion owner runs at a time:

1. Disable movement, combat, hit reaction, CharacterController and gameplay input.
   Sheath the sword immediately because the draw/sheath event could be interrupted.
2. Disable Animator and activate 15 Humanoid-mapped physics bodies. The unused
   Starter Assets skeleton is not touched. No Rigidbody is added to the root.
3. Wait for floor support and low torso/pelvis velocity for the landing interval.
   An airborne apex cannot start recovery. A blocked standing capsule waits for a
   nearby clear supported pose rather than activating inside a wall.
4. Disable ragdoll and physics interpolation, restore the animated skeleton's
   local bone offsets, rebind Animator, align the root near the landed body, blend
   rotations briefly into the provided non-looping `Getting Up.fbx`, and play the
   entire unmasked full-body clip. Combat/arms/footwork overlays are zero-weight.
5. At normalized time 1, clear stale speed/jump/strafe/attack parameters, select
   each overlay's empty/default state, return to grounded Base Layer.Default idle,
   and restore enabled flags, layers, animation speed and input. Reset movement velocity.

The supplied animation-only Mixamo FBX initially imported with a skewed reference
pose (twisted hips, thighs, knees and feet), causing crossed legs even with all
overlays disabled. Its Humanoid reference rotations are calibrated from the original
Paladin Mixamo skeleton, remapping mixamorig1 to mixamorig. Source bone lengths,
animation curves, clip duration and the player's avatar remain unchanged. Do not
replace this corrected reference pose with the imported bent standing frame.

At default playback speed, get-up is 7.6 seconds; lock duration includes falling
and settling as well. `getUpSpeed`, blend/settle durations and ground layers are
editable on Player's component. A new launch while incapacitated restarts falling
without overwriting the original enabled-state snapshot. Death explicitly cancels
knockdown **before** PlayerDeathRespawn captures flags, preventing locked respawn.

MiningOrbitCamera reads CameraFocusPosition from the moving/animated hips during
fall and get-up, not the stationary CharacterController root. Shift lock is released
on launch and cannot be re-enabled while incapacitated. Manual mouse orbit remains
available using the existing RMB controls; camera yaw never sets the recovery root
yaw. Heading auto-recentering is also suppressed until recovery ends. Knockdown
LateUpdate runs before the orbit samples focus so it follows the rendered blended pose.

Setup: `Mining Simulator > Setup > Player Knockback And Get Up`. This is idempotent
and marks the current scene dirty; it does not overwrite a dirty scene automatically.
Save the current scene to persist the installed component.

Validation: `Mining Simulator > Validation > Player Knockback (isolated)` temporarily
disables original scene roots, runs a clone without save/progression scripts over a test floor,
and restores the root activity on leaving Play Mode. No start-scene override is set.
Checks landing, full get-up, capsule lock/restore, repeated hits, death and respawn.
The fixture retains the real motor, combat, input and equipment components to
validate the return to idle with those motion owners enabled. It checks grounded
idle parameters and foot separation, not only the duration/control flags.
PlayerKnockbackTests also checks clip/controller binding and unchanged ordinary hits.
Existing missing-script warnings are unrelated and are not removed by this feature.

Limitations: single supplied get-up variant, blended from the final ragdoll pose;
there are not separate prone/supine clips. Validate appearance near uneven terrain
and tightly packed obstacles in the actual game before tuning launch force higher.

Verified 2026-10-09 in Unity 6000.5.3f1 via MCP: 3/3 EditMode tests passed.
Isolated live physics validation passed fall/landing, 7.6-second recovery, repeated
launches and real PlayerDeathRespawn death/respawn (9.26 seconds to first recovery).
Console had no errors on the clean validation; existing missing-script warning remains.
Camera follow/shift-lock regression additionally exercised continuous orbit during
ragdoll/get-up: root rotation change remained 0 degrees when camera yaw was changed
to 180; animation layer weights were [1,0,0,0]. Reviewed a Play Mode image at 30% of
the supplied clip (knees/hands supporting the body). Physics interpolation is off
during recovery so it cannot overwrite Animator. The old missing-script warning remains.
SampleScene was restored with the original Paladin avatar and WeaponHolder/
ShealthHolder, with no Play Mode Start Scene override. No uneven-terrain visual
soak or combat crowd stress test was performed by this bounded validation.

Follow-up visual regression 2026-10-09: reproduced crossed feet at 90% of the
clip with weights [1,0,0,0]. Corrected source reference rotations; reviewed actual
rendered poses at 42% (hand-supported rise), 90% (standing, separated feet), and
post-recovery idle. Integrated motor/combat/equipment validation passed full 7.6s
get-up -> grounded idle with uncrossed feet, repeat launch, death and respawn
(9.27s to first recovery). The FBX animation data and Paladin avatar were not
rewritten. A source-reference-pose regression test covers both thighs, knees and feet.
