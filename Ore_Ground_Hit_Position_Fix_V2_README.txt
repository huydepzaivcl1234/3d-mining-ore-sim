ORE GROUND + FIRST-HIT POSITION FIX V2
========================================

Install
-------
1. Finish or abort any Git rebase currently in progress.
2. Close Unity.
3. Extract this ZIP into the Unity project root (the folder containing Assets).
4. Allow overwrite for the two included C# files.
5. Reopen Unity and wait for script compilation.

Root cause
----------
Ore.Initialize configured MiningHitPunch before OreSpawner finished these steps:
1. KeepAboveSurface collider correction.
2. The OreData SpawnHeightOffset used to embed the ore slightly in the ground.

MiningHitPunch therefore stored the earlier Y position. On the first hit it restored
that stale position, moving the complete ore root, collider and NavMesh obstacle down.
This also made the Scene-view navigation/health gizmos jump or flicker and could
invalidate an NPC route.

Fix
---
- The completed spawn transform is now captured after all surface placement.
- Hit feedback returns to the visible spawned position instead of the stale Y value.
- Existing authored ground embedding remains unchanged:
  - Stone/Coal/Copper/Iron/Gold/Diamond/Light Stone/Dark Stone/Netherite: -0.5
  - Gem: -0.8
- Includes the previous protection against spawning ores on top of NPC/ore colliders.

Files included
--------------
- Assets/Scripts/Ores/Ore/Ore.cs
- Assets/Scripts/Ores/Ore/OreSpawner.cs

Suggested Unity test
--------------------
1. Enter Play Mode and watch a newly spawned ore before its first hit.
2. Hit it repeatedly with the player and an NPC.
3. Confirm its resting Y position never changes after a hit.
4. Confirm the collider/NavMesh obstacle gizmo does not permanently jump underground.
5. Let pooled ores respawn and repeat the same test.

Validation performed before packaging
--------------------------------------
- The supplied 21.6-second, 2560x1440, 60 FPS video was reviewed frame-by-frame.
- The spawn and hit-feedback execution order was traced in source.
- Every OreData SpawnHeightOffset was checked.
- git diff --check passed.
- Package contents were compared byte-for-byte with the edited source files.
- Unity Play Mode/compilation was unavailable in the packaging environment.
