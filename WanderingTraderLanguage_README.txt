Wandering Trader Language Fix

Install
1. Extract this ZIP into the Unity project root: D:\3d mining sim
2. Allow Unity to overwrite the existing files.
3. Wait for Unity to finish compiling/importing.
4. Open SampleScene and press Play.
5. Open Settings and press the Language button. The Wandering Trader updates
   its title, Buy/Sell page, buttons, balance, timer, notes, item names, and
   error messages with the selected language.

Included files
- Assets/Scripts/Ores/Trader/WanderingTraderPanel.cs
- Assets/Scripts/Ores/Mining/JuicyBuyMinerButton.cs
- Assets/Scripts/Ores/Editor/MiningJuicyBuyMinerSetupMenu.cs
- Assets/GameData/Localization/English.txt
- Assets/GameData/Localization/Vietnamese.txt
- Assets/Scenes/SampleScene.unity

The lightning Unicode symbol was replaced with font-safe text so the
LiberationSans SDF missing-glyph warning does not appear for the miner card.

This package does not change trader offers, prices, inventory behavior, SFX,
or scale-only animations.
