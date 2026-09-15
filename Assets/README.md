# Juicy Upgrade Button (leather + cyan crystal)

Extract this ZIP into your existing Unity project root and keep the `Assets/` paths.

1. Let Unity import and compile the files.
2. Open your **own** mining scene (containing `MiningUpgradePanel` and its existing `Open Upgrades` Button).
3. Run **Mining Simulator > UI > Build Juicy Upgrade Button**.
4. Review `NPC Shop/Open Upgrades` and save your scene.

The tool changes the existing Button in place, so `MiningUpgradePanel` and `MiningUiPanelCoordinator` still own opening, closing and modal transitions. The new component provides the 8 px pressed body, clipped shimmer and English/Vietnamese labels. The menu generates two neutral rounded uGUI sprites (`LeatherPillRounded.png` and `BrassMedalCircle.png`) in `Assets/Generated/MiningUI` when you run it; no Candy-themed image is required.

This is a focused overlay for the current GitHub `main`, which already includes `MiningUiGradient`, `JuicyButtonTrim` and the earlier BuyMiner button. It changes only the new upgrade button scripts, the existing setup-menu guard and three English/Vietnamese translation keys.

No `.unity` scene is included, including `SampleScene.unity`. If an older copy of `MiningCandyUiSetupMenu.cs` still throws a `CandyGlossAlpha` error in your installation, that obsolete script is not part of this package and must be removed from your existing project before either menu can run.
