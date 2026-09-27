# Low sword draw / sheath

Uses the user's Low.prefab and preserves Sword's authored model scale, offsets,
damage, range and speed. Normal stance mounts the same model at Hips; E plays
DrawSword, transfers it to RightHand at drawAttachTime, then enters CombatIdle.
E again plays SheathSword and transfers it back at sheathAttachTime. No duplicate
weapon, Playables graph or scene rewrite is needed.

The asset-only Editor setup makes LowSwordCombat.controller by copying Player's
current Animator Controller (fallback: the existing combat/respawn/hit controller).
Existing locomotion, masks, death/hit layers
remain. New sword Attack uses Sword And Shield Slash; CombatIdle uses the existing
Sword And Shield Idle. Sword locomotion uses Run With Sword. Draw/sheath use the user's imported Mixamo clips. Edit all
states directly in LowSwordCombat.controller. Generated state motions are not
overwritten on rerun. Initial setup/migration runs on import when the sword
controller lacks SwordPose, or run Mining Simulator > Setup > Setup Low Sword Draw And Sheath.

Controllers are separate: UnarmedCombat.controller retains the original fist
combat layer and normal locomotion; LowSwordCombat.controller owns sword states.
Fists.asset references UnarmedCombat; Sword.asset references both controllers.
No second Animator component is added. TryEquipWeapon switches controllers and
preserves locomotion parameters/phase; equipping a sword starts sheathed.
The sword base layer blends normal and sword locomotion through SwordPose.
No sword walk was supplied, so walking uses the sword run at reduced playback.
Idle/run clips loop, sword clips bake root translation/rotation into pose.
Existing footstep events are copied onto the sword run if it has none.
Sword Arms layer plays draw/sheath with only arms and hand IK enabled. Root,
body, head and legs are excluded. Sword Stance is above it and supplies full-body
draw/sheath while stationary, fading out when movement starts. The combat attack
mask remains unchanged. Both equip layers run the same clip phase, so stopping or
starting mid-draw does not restart the animation.
stanceBlendSeconds (default 0.25) controls stance blends separately from attacks.
OnDrawSword / OnSheathSword clip events request attachment in LateUpdate after
the hand pose has evaluated. Duplicate events from two layers are ignored.
The existing attachment time values author the events when Setup is run. Events
can also be edited directly in the imported draw/sheath clips. A completion-only
fallback prevents missing events from leaving the weapon on the wrong bone.

Sword.asset stores draw/sheath normalized attachment times and Hips offsets.
Tune these to the imported clip's hand-contact frame; hip/hand poses depend on
the model pivot and Avatar, so visual alignment must be checked in Play Mode.
AttackSpeed affects attacks, not draw/sheath duration. Movement continues during
masked stance clips. Attacking and changing equipment are blocked while changing
stance. Repeated E presses queue the final requested stance; an attack completes
before sheathing. Right-click locking a monster requests the draw sequence too.
Modal opening blocks stance input; death/disable resets it to sheathed.

Graph repair removed nine unreachable transitions referencing deleted states
from LowSwordCombat, UnarmedCombat and the original respawn/hit controller.
Active reachable states were preserved byte-for-byte; remaining local references
resolve. The backup ZIP is retained outside the Unity project.

Validation: source compilation and live Editor import passed. Desktop Play Mode
preview entered the sword stance with the sword transferred to RightHand.
The existing Editor Graphs.Edge.WakeUp exception recurred on domain reload;
it is not an error in the runtime sword stack. Detailed runtime checks and any
limitations are reported with the handoff; source compile is not visual proof.
Moving draw preview transferred the sword to the hand and moved the player.
Moving sheath preview was triggered, but night lighting prevented confirmation
of final alignment. Physical E input, sheath continuity and the
equipment-swap preview have not been verified end-to-end. No NUnit run claimed.
