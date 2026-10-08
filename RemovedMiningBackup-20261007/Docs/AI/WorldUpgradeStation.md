# World-space upgrade station

The existing upgrade panel and all ten purchase bindings are reused, not copied. A real anvil from the installed Human Crafting package hosts a World Space Canvas. The flat upgrade HUD button is retired at runtime. Other modal menus retain their existing behavior.

Approach within 3.5 m and click the floating interaction button. Movement and orbit-camera control remain available. Close/back/Escape dismiss the panel. Leaving beyond 5.5 m fades, shrinks and lowers it over 0.3 seconds. Player death or another blocking menu also dismisses it.

The station prefab is Assets/Resources/MiningUpgradeStation.prefab. Without an authored station the existing upgrade presenter instantiates it at (-8, 0, 10). Use Mining Simulator > Setup > Place Upgrade Anvil to place one in the scene and move it freely. A scene-authored station takes precedence. Save your scene manually; the patch ZIP does not contain SampleScene.unity.

Edit interaction/closing distances, fade duration, panel width and offsets on Mining Upgrade Station. Its view uses the current main camera and the same camera-rotation billboard pattern as the celestial UI. GraphicRaycaster uses 3D blocking so UI clicks cannot pass through physical walls.

Select the monster spawner to see four amber strips 4-7 m outside the referenced mining bake volume. Preview geometry follows that surface, not the obsolete spawner transform. Green/gray candidate markers validate against the active NavMesh when available. No combat-range gizmos are removed.

Validation: Unity compiled; interaction button opened the original panel; WorldSpace verified; CharacterController remained enabled and moved while open; coordinator did not block gameplay; moving 8 m away closed the panel and finished alpha-zero/inactive. No purchase was made during these tests.
