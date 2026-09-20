NPC ORE SPAWN + PATH FIX
========================

Install
-------
1. Finish or abort any Git rebase currently in progress.
2. Close Unity.
3. Extract this ZIP into the Unity project root (the folder that contains Assets).
4. Allow overwrite for the three included C# files.
5. Reopen Unity and wait for script compilation.

What this fixes
---------------
- Ground placement ignores NPC, ore, and Lucky Block colliders, so a rapidly
  respawning ore cannot use the top of an NPC's head as the ground surface.
- Spawn candidates too close to any active NPC are rejected.
- A mining NPC now routes toward the closest point on the target ore collider
  instead of an artificial radial stand-point around the ore.
- Lucky Block targeting keeps its existing reserved stand-point behavior.

Files included
--------------
- Assets/Scripts/Ores/Npc/MiningNpc.cs
- Assets/Scripts/Ores/Ore/Ore.cs
- Assets/Scripts/Ores/Ore/OreSpawner.cs

Suggested Unity test
--------------------
1. Set the miner to maximum movement/mining speed.
2. Let ores deplete and respawn while NPCs cross the spawn area.
3. Confirm no ore appears above an NPC or overlaps an NPC.
4. Select an NPC in Play Mode and confirm its final destination follows the
   nearest surface of the ore collider.

Validation performed before packaging
--------------------------------------
- git diff --check passed.
- Package contents were compared byte-for-byte with the edited source files.
- Unity Play Mode/compilation was not available in the packaging environment.
