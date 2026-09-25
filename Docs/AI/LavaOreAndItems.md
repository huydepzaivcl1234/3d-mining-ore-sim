# Lava ore and timed items

Two new Lava World OreData assets are in `Assets/GameData/Ores/Lava/`: Dry Lava (Uncommon) and Lava Core (Legendary). Their existing Ember Ore and Molten Core prefab references are temporary models. Assign your own prefab to each OreData asset when ready; the ore spawning system initializes the prefab with the chosen OreData at runtime.

To add the two entries to an existing scene's Lava table without replacing your other entries, select the portal in the Hierarchy and run `Mining Simulator > Portal > Setup Lava World On Selected Gate`. Save the scene yourself. This adds Dry Lava at a default weight of 12 and Lava Core at 2; adjust both weights, power, health and rewards in the Inspector. A third new ore has not been named, so no placeholder asset or spawn entry is created.

Three timed consumables are registered in `Assets/GameData/Items/MiningItemDatabase.asset`. Rename them and assign a world model/inventory icon on each asset:

- Mining Speed Boost (Rare): 15% faster NPC mining swings and animation impact events; default 180 seconds.
- Ore Lucky Critical (Rare): default 15% chance on each player or NPC hit to add 145% damage to Ore or Lucky Block; no critical damage on chests. The chance and bonus are editable on the item asset.
- Event Catalyst (Epic): adds 10 percentage points to both Coin Rain and Stalked event roll probabilities, capped at 100%; default 180 seconds.

Effects activate when the item is consumed from the inventory, following the existing timed item behavior. The event bonus applies to event rolls made while active, not to events already rolled for the current morning or night.
