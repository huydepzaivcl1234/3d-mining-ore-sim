# Underground world

The portal unlocks a separate Underground mining area after **3 Rebirths and 1,000,000 Coins** by default. The cost is paid once; later entries are free after unlocking.

- Ground and Underground use separate ore spawners.
- The editable Underground ore table is `Assets/Resources/MiningSimulator/UndergroundOreSpawnData.asset`. Its defaults include Stone, Coal, Iron, Gold, Diamond, Light Stone, Dark Stone and Netherite; Copper and Gem are excluded.
- Add `MiningWorldAreaController` to an object in a gameplay scene to edit area bounds and unlock conditions. The runtime creates one with defaults when none is authored.
- In the Unity Editor, `Mining Simulator > Setup > Create Underground World In Open Scene` creates a visible hierarchy object. For `SampleScene`, use `Mining Simulator > Setup > Create Gameplay Scene Copy With Underground` and save the new scene separately.
- Underground ambience is configured in `Assets/GameData/Audio/MiningAudioData.asset`.

In Play Mode, check that the portal blocks entry below either requirement, Underground spawns only from its own table, ambience changes on entry and return, and NPCs continue routing around ore obstacles.
