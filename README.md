# Leather Miner Progress HUD — next ore unlock

Extract this ZIP into the **project root, not inside `Assets`**. The current repository already contains `NpcProgressionHud`, `MicroBar`, `MiningUiGradient` and `JuicyButtonTrim`.

1. Let Unity finish compiling.
2. Open your own mining scene with `NPC Progress HUD` and its existing `Experience Bar` MicroBar.
3. Run **Mining Simulator > UI > Build Juicy Miner Progress**.
4. Review the HUD and save **your own scene**.

The existing `NpcProgressionSystem` still awards real mining XP; `NpcProgressionHud` continues to animate the original MicroBar and display actual XP. The new UI uses a leather card, brass miner avatar, XP trench and the next *configured, spawnable* ore at its real mining-power requirement. It does not grant fake gold or invent a rank system. When power crosses a threshold, the existing `MiningUnlockNotifier` announces and spawns that ore.

The menu generates two neutral UI sprites in `Assets/Generated/MiningUI` only when run; the ZIP contains no Candy assets and **no `.unity` scene, including `SampleScene.unity`**. This package is deliberately separate from the earlier upgrade-button draft PR and does not reinstall the repository's moved setup script under `Assets/Assets`.
