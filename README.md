# 3D Mining Ore Simulator

Unity mining simulator with NPC miners, ore progression, rebirths, Wandering Trader,
NavMesh navigation and A* shortest-reachable-ore selection.

## Underground Area

The project includes a portal that unlocks and enters a separate Underground mining area.

- Default unlock requirement: **3 Rebirths + 1,000,000 Coins**.
- The cost is paid once; later entries are free after unlocking.
- Ground and Underground use separate ore spawners.
- Underground uses its own editable ore table:
  `Assets/Resources/MiningSimulator/UndergroundOreSpawnData.asset`
- The default Underground table contains Stone, Coal, Iron, Gold, Diamond, Light Stone,
  Dark Stone and Netherite. Copper and Gem are excluded by default.
- Underground area center, size and unlock conditions can be edited by adding
  `MiningWorldAreaController` to any gameplay object in your own scene.
- To make it visible in the Hierarchy, open your gameplay scene and run:
  `Mining Simulator > Setup > Create Underground World In Open Scene`
- If the only scene you have is `SampleScene`, run:
  `Mining Simulator > Setup > Create Gameplay Scene Copy With Underground`
  and save it as a new scene such as `Assets/Scenes/MiningGameplay.unity`.
- Underground ambience can be changed in:
  `Assets/GameData/Audio/MiningAudioData.asset`
- No sample scene is modified or required. If no controller is authored, the runtime creates
  one with the default settings automatically.

## Install / Test

1. Close Unity and extract the update ZIP into the project root.
2. Replace existing files when prompted.
3. Reopen Unity and wait for compilation/import to finish.
4. Enter Play Mode and interact with the portal.

Check that the portal blocks entry below either requirement, Underground spawns only from its
own table, the ambience changes on entry/return, and NPCs continue routing around ore obstacles.

More detailed notes are available in `README_UNDERGROUND_V11.txt`.
