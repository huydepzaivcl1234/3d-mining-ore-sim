# E drop / floating weapon pickup — 2026-10-26

Implemented in D:/3d mining sim.

Controls: E drops the equipped gun about 2.2 m in front of the camera's horizontal facing direction. It arcs down for .55 seconds, then floats roughly .25 m above the surface, bobs .035 m and spins at 85 degrees per second. Walk within 1.2 m to collect and equip it automatically after a .85 second release grace period.

The pickup is a static mesh/material snapshot, not a cloned firing script or Animator. Hidden/inactive and zero-sized meshes are excluded. The original hand animations, ADS, recoil and reload assets were not changed. Inventory ownership prevents Q or slot 1 from recreating a dropped gun. Loaded and reserve ammunition are retained. Ground placement checks forward obstacles and pickup checks line of sight. Drops are session objects and are cleaned up with their owner.

Changed existing PlayerWeaponProgression.cs and PistolShooting.cs; added DroppedWeapon.cs. Existing rifle unlock rules retained. Grounded weapons do not receive daytime ammunition refills.

## Actual Play Mode validation
Validation.json and Frames/ are the final input-driven test:
- E generated one gun pickup; repeated E did not duplicate it.
- Gun settled and rotated 85 degrees over one measured second.
- Minimum visual bound was .2655882 m above ground at the sampled bob phase.
- Q and slot 1 could not equip the dropped weapon remotely.
- A shot attempted while disarmed did not fire.
- Actual W movement traveled 1.6724813 m and auto-collected/equipped the gun.
- One shot after pickup fired successfully: initial 5/37 ammunition became 4/37.
- Console checked after execution: zero errors.
- Screenshots 0080 (floating gun) and 0120 (equipped after pickup) were opened and reviewed.
- Temporary fill light and damage isolation were removed/restored after the test.
- Returned Editor to Edit Mode.

Drop_Pickup_Test.mp4: approximately 5.17 seconds, no audio. Night preview used a temporary fill light, not a permanent lighting change.

No multiplayer replication, inventory persistence across scene reloads, standalone build or dedicated rifle playtest was performed. Runtime drop placement and line-of-sight code are shared by both loadouts.
