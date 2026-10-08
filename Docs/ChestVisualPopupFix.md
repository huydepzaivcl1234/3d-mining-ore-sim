# Chest visual and gold popup repair

Connected project: D:/3d mining sim. No SampleScene file saved or packaged.

The imported chest clips include constant position keys near (0, 0.00257, 0.00244), while the authored hinge is at approximately (0, 0.24, -0.26). Sampling the clips had overwritten the hinge anchor and collapsed the lid into the body. The prefab hinge is restored; the runtime sampler preserves its local position/scale while applying the clip rotation. Static collider bounds are calculated from the corrected closed model.

Gold popup now pairs the existing HUD coin sprite with the amount, e.g. coin +125. Font size defaults to 38 instead of 30; icon size is 44. Layout centers the pair according to measured text width, with a gap to prevent overlap. Both elements rise and fade together. Font size, icon size and lifetime are editable in TreasureChestData.

The current authored GameData lives at Assets/GameData/Base/TreasureChestData.asset; that location was retained. This patch does not create a duplicate Resources asset or change spawn/progression rules.

Validation: four TreasureChestTests passed; corrected closed/open poses and popup were inspected through Unity camera renders; compilation has no errors. This turn did not run a full gameplay soak. A pre-existing missing-script warning was recorded, but no scripts were removed to hide it.

ZIP contains root-relative changes and metadata only, excluding SampleScene. It uses the coin sprite already present at Assets/Prefabs/UI/Đồng xu vàng biểu tượng khai khoáng.png.
