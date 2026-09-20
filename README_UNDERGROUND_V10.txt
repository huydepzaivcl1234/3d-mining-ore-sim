UNDERGROUND AREA V10
====================

WHAT THIS UPDATE ADDS
---------------------
- The existing portal now opens an Underground unlock/travel panel.
- Permanent unlock requirement: 3 Rebirths + 1,000,000 Coins.
- Coins are paid once. Later portal use is free.
- Full save reset locks Underground again; a normal Rebirth does not.
- Ground and Underground use separate OreSpawner instances.
- The active miner NPCs, NPC shop, progression XP, unlock toast and Miner Progress UI
  automatically follow the active area's spawner.
- Ground UI still says "All configured ores unlocked".
- Underground UI says "All underground ores unlocked" and uses the Underground table.
- Underground reuses the configured ore table/prefabs and has a generated cave theme:
  basalt floor, cave walls/ceiling, colored rock variants, crystal lights, fog and ambient light.
- Entering/leaving asks MiningNavMeshBuilder to rebuild after the floor changes.
- Includes all V9 shortest reachable A* ore-selection and NavMesh fixes.

HOW TO USE
----------
1. Extract this ZIP into the Unity project root and overwrite when asked.
2. Open Assets/Scenes/SampleScene.unity.
3. Let Unity finish compiling.
4. Enter Play Mode and hover/click the existing portal with the normal interaction key.

No manual scene setup is required. The Underground world is built once at runtime by the
portal's MiningWorldAreaController. The current ore table is reused automatically.

BALANCE SETTINGS
----------------
Defaults are serialized in:
Assets/Scripts/Ores/Portal/MiningWorldAreaController.cs

  requiredRebirths = 3
  requiredCoins    = 1,000,000

To tune these in the Inspector, add MiningWorldAreaController to a scene object before Play
Mode. The portal will reuse that authored instance instead of creating one at runtime.

TEST CHECKLIST
--------------
- Below 3 Rebirths: unlock button is disabled.
- At 3+ Rebirths but below 1,000,000 Coins: unlock button is disabled.
- At both requirements: unlock spends exactly 1,000,000 Coins and enters Underground.
- Returning to Ground and entering again costs nothing.
- Normal Rebirth keeps Underground unlocked.
- New Game / full reset locks Underground again.
- Ores from the inactive area disappear and its spawning loop stops.
- NPCs release their previous target and choose reachable ores in the active area.
- Miner Progress text changes between Ground and Underground.
- NavMesh is rebuilt once after each area switch.

NOTE
----
The cave is generated with project-native Unity primitives/materials, so no third-party asset or
texture import is needed. Decorative rocks have disabled colliders so they cannot trap NPCs.
