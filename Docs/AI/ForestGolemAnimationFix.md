# Forest Golem locomotion animation fix

The controller's Idle/Walk assets contained Humanoid muscle curves while the
current Forest FBX and attack clips use a Generic rig. Sampling the previous
clips left every tested Forest pose unchanged. This explains the missing Idle
and frozen walking legs.

Copied the current native Forest FBX Idle/Walk clips into the existing animation
assets, preserving their GUIDs, controller references and names. Enabled looping
and removed the unrelated exported Camera curve. Attack, ChargeUp, GameData,
prefab collider, model scale and saved scene remain unchanged. No arbitrary
model-height offset was introduced.

Validation in an isolated Play scene: actual Animator Idle, Walk, Hammer and
ChargeUp sampled at normalized phases 0, .25, .5, .75 and 1.25. Idle/Walk now
change bone poses and repeat correctly. CharacterController settles at root Y=0
with isGrounded=true. Forest planted left-foot bone is approximately .031-.032 m
above the floor (.042-.045 m for regular Golem); walking lift is .178 m versus
.191 m for regular Golem. Screenshot comparison confirms the grounded model.
No Console warnings/errors after checks. Uneven terrain and live gameplay were
not tested this turn. SampleScene and player save data were not modified.
