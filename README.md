# Leather Upgrade Panel — 10 live upgrade cards

Extract this ZIP into the Unity **project root**, not into `Assets`. This archive has no scene files. The two SVGs in `DesignReferences/` are visual references; the Unity Editor menu builds editable uGUI elements instead of baking placeholder prices, captions or cards into a picture.

1. Let Unity import and finish compiling.
2. Open your own mining scene, with the existing `Upgrade Panel` and `MiningUpgradePanel` controller.
3. Run **Mining Simulator > UI > Build Leather Upgrade Panel**.
4. Review both languages and affordability, then save **your own scene**.

The setup menu preserves all ten existing purchase Button roots, controller references, hover/click feedback, audio, wallet and save/progression rules. It only skins the panel and attaches presentation components to existing cards. No fake gold-grant button, independent levels, duplicate purchases or invented reward mechanics are added. Real prices, stack levels, balance and locked/MAX state always come from `MiningUpgradeSystem` and `PlayerWallet`.

The tool creates `Assets/Generated/MiningUI/UpgradeLeatherRounded.png` on first use for sliced, rounded UI surfaces. It is safe to rerun on the same scene, marks the scene dirty but never saves it. **No `SampleScene.unity` or other `.unity` file is included.**
