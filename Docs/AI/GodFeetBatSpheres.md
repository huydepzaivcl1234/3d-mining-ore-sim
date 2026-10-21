# God feet and Bat player collision (2026-10-16)

God's movement controller was grounded, while its visual root sat 0.7963 m above its parent origin. Live standing mesh geometry was about 0.64 m above the ground. A fixed root offset alone does not account for differences between Idle, Walk, Run and attack clips.

Following the user's request for direct Scene/Prefab authoring, the automatic LateUpdate foot correction and its GameData fields were removed. GodBoss.prefab -> Mutant now has local Position Y = 0.15632354 instead of 0.79632354. Adjust that child Transform in Prefab Mode or as an explicit scene-instance override. No script rewrites its position each frame. Visual position and scale both scale when configured boss height changes.

Bat has two bone-bound SphereCollider damage triggers, radii approximately 0.2322 and 0.2137 local units, replacing the two BoxColliders. Its body reference points to the body bone's sphere. The tall CharacterController remains an internal dependency of ground movement/pathfinding, but MonsterHitbox explicitly ignores its collision pairs with the player's colliders. It is excluded from bullet hits by the existing IgnoreShot rule. Ignore pairs are applied during Initialize and on re-enable, and restored on disable. Other species are unaffected.

Validation in Play Mode:
- Actual controller-to-controller test under Bat: blocked after 1.24 m before exclusion, traversed all 4 m after exclusion.
- Ignore pair restoration and reapplication both passed after disabling/enabling Bat.
- Body sphere ray hit passed; movement capsule rejected as damage hit.
- Six God animation states sampled at 20 points per state for contact inspection; Idle was also rendered from a separate side camera. After final calibration, Idle mesh bottom ranged from -0.0144 to -0.0038 m on ground at -0.0100 m. Other clips can place parts of the mesh below that plane; this correction is not per-vertex collision or full foot IK. No attack timings or clips were changed.
- Jump state bypass was checked explicitly: invoking grounding did not move the visual root.
- Temporary God/Bat instances and camera were removed by stopping Play. No SampleScene save, encounter-day change, player-data reset or boss kill reward was performed.

The validation bullets above record the earlier automatic-grounding experiment. The final implementation is the authored Transform offset, with no runtime foot IK. Animation-specific sole placement can be authored in animation clips.

## Jump landing follow-up

God's CharacterController previously collided with the player's upper collider while descending and treated it as a grounded support surface. It now ignores that collider pair during Jump, retains the original pair setting, and restores it only after the bodies separate. OnDisable restores the setting; re-enabling during Jump reapplies the exclusion. World collision remains enabled. A fallback Ground ray projects the committed destination onto terrain when the navigation grid cannot project it. The impact wave begins only after the motor reaches ground.

An isolated Play Mode test exercised the real Chase -> Windup -> Jump -> Chase transition with a stationary player-sized capsule directly at the destination. God landed at Y = 0.08 on floor Y = 0, rather than on the 1.78 m target; collision restoration passed after separation. No boss reward or player progression was changed.

A second test placed the target 1.5 m above the floor and disabled/re-enabled God during Jump. It again landed at Y = 0.08, completed the jump and restored collision after separation. Both temporary arenas were removed.
