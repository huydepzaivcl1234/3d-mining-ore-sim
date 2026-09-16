# Safe Area Position Fix

Extract this ZIP into the Unity project root and allow it to overwrite `MiningSafeAreaInset.cs`.

The component no longer executes in Edit Mode or restores an old cached position. It reads the RectTransform position saved in your Scene when Play Mode starts, applies only the device safe-area offset, and restores the Scene-authored position when disabled.

After Unity recompiles:

1. Move `NPC Progress HUD` to the desired position in the Scene.
2. Save the Scene.
3. Enter Play Mode and confirm it remains at that position.

The ZIP contains no `.unity` files, including `SampleScene.unity`.
