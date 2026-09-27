# Player sword and mouse target lock

Only Player was changed; no MiningNpc behavior, scene layout or Animator
controller was rewritten. The existing Sword data is now the default Player
weapon, with the repository Human_Sword prefab attached to the Humanoid right
hand. Existing damage/range/attack speed values were preserved.

Edit Assets/GameData/Weapons/Sword.asset: Model Prefab, Hand Bone, Model Local
Position/Euler Angles/Scale. PlayerCombatInput has an optional Weapon Hand
override for a custom hand socket. The spawned model is presentation-only;
its colliders are disabled and damage still comes from the existing hit sector.
Assign animationOverrides if a different sword Attack clip is wanted. This
change intentionally does not replace the user's authored punch/sword clips.

Right mouse on a living MushroomMonster locks it and enters combat mode. Right
mouse on terrain, a wall, empty space or a non-monster clears the lock. The
nearest non-Player raycast collider wins (no click selection through walls;
Player's own capsule and held model cannot block selection). Selection range
is measured from Player. A locked cursor selects at the camera center.
Attack remains a separate action: left mouse by default. Legacy right-mouse
Attack bindings are overridden to left mouse at runtime only when Aim also uses
right mouse, so cancellation cannot trigger an attack. Other custom bindings
are preserved. E leaving combat clears the lock.

While locked, movement stays camera-relative and Player facing follows the
monster. At contact Player faces it exactly before both VFX and damage queries.
Range/angle still apply: no remote damage or automatic movement. Single-target
punch prefers the selected monster only if inside the hit sector. Sword keeps
its multi-target forward sweep. Dead/disabled/destroyed/out-of-range targets,
Player death, component disable, pause or open modal panels release the lock.
Current monster selection recognizes MushroomMonster (the existing monster
type); add support explicitly when another monster runtime type is introduced.

Validation: the repository Roslyn helper compiled all three affected assemblies.
Unity Editor also rebuilt Unity.StarterAssets, Assembly-CSharp and the Editor
assembly; Library/Bee/tundra.log.json recorded successful compiler/postprocessing
and assembly copy results. No command-line Unity build was used.
New EditMode cases cover non-monster rejection, dead-target invalidation,
selected target priority, unlocked nearest-target behavior, self-collider
exclusion and wall occlusion. They need Unity
Test Runner execution. No desktop interaction or Play Mode test was requested
in this turn; hand alignment and click/movement feel remain to verify in Editor.
