UNDERGROUND AREA V11
====================

WHAT THIS UPDATE ADDS
---------------------
- A separate Underground OreSpawner with its own OreSpawnData asset.
- Ground and Underground no longer share the same ore roll table.
- The Underground table is editable at:
  Assets/GameData/Spawning/UndergroundOreSpawnData.asset
- The default Underground table uses Stone, Coal, Iron, Gold, Diamond, Light Stone,
  Dark Stone and Netherite. Copper and Gem are intentionally not in the default table.
- Underground spawning does not use the ground Day/Night special-ore roll.
- Underground area center and size are editable on GameManager >
  MiningWorldAreaController.
- The cave floor, cave walls, decorative rocks and spawn bounds use that same configured size.
- Underground ambience is played as a looping crossfade while Underground is active.

INSPECTOR SETTINGS
------------------
On GameManager > MiningWorldAreaController:

  Required Rebirths       3
  Required Coins          1,000,000
  Underground Spawn Data  UndergroundOreSpawnData
  Underground Area Center (0, 0, 0)
  Underground Area Size   (20, 0, 20)

On GameManager > MiningAudioManager > MiningAudioData:

  Underground Ambience        NightForest by default
  Underground Ambience Volume 0.55 by default

Replace the ambience clip with any cave/wind/drone clip you prefer. The existing master/music
volume and mute controls still apply.

INSTALL
-------
1. Exit Play Mode and close Unity.
2. Extract the ZIP into the Unity project root, the folder containing Assets.
3. Replace files when prompted.
4. Reopen Unity and wait for compilation/import to finish.
5. Enter Play Mode and use the portal.

No manual scene wiring is required for SampleScene: GameManager already contains the
MiningWorldAreaController and references the separate UndergroundOreSpawnData asset.

TEST CHECKLIST
--------------
- Ground uses only the normal ground table.
- Underground uses only UndergroundOreSpawnData and does not roll Copper or Gem by default.
- Changing Underground Area Size changes both the cave footprint and ore spawn footprint.
- Below either entry requirement, the portal button stays disabled.
- At both requirements, the configured coin cost is paid once and the area unlock persists.
- Returning to Ground and entering Underground again is free.
- Underground ambience starts on entry and stops/reverts to ground ambience on return.
- NPCs choose ores from the active area's table and retain the shortest reachable path behavior.

VALIDATION NOTE
---------------
Static source/YAML validation and archive checks are performed before delivery. Unity Editor
Play Mode must still be run in the user's project to verify the exact scene, imported audio and
NavMesh rebuild on the target machine.
