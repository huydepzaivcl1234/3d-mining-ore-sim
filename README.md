# Juicy Buy Miner Button

Extract this ZIP into the root of your existing Unity project, keeping the `Assets/` paths.

1. Let Unity finish compiling.
2. Open your own mining scene (the one containing `MiningHud` and the existing buy-miner `Button`).
3. Run **Mining Simulator > UI > Build Juicy Buy Miner Button**.
4. Inspect the `BuyMinerButton` object under `NPC Shop`, then save **your own scene**.

The command modifies the existing button in place so the `MiningHud` purchase listener stays wired. The component only renders UI/animations; it does not fake purchases. The price and miner rate come from `NpcShop/NpcData`. If `NpcShop` is missing, the price is shown as a dash until you assign it.

The ZIP does **not** contain `SampleScene.unity` or any other scene. It uses the miner/coin sprites already referenced by `Assets/GameData/UI/MiningUiData.asset`; no legacy Candy assets are needed.

`MiningUiGradient.cs` is bundled because it is required for the leather/brass uGUI Images and may not yet exist on your `main` branch.

If your project still contains the old `Assets/Scripts/Ores/Editor/MiningCandyUiSetupMenu.cs` with the `CandyGlossAlpha` compile error, remove that obsolete Candy editor script before running the menu command. It is not part of this package.
