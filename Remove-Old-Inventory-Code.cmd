@echo off
setlocal
set "ROOT=%~dp0"
if not exist "%ROOT%Assets\Scripts\Ores\Mining\MiningUiSmoothFade.cs" (
  echo Extract this ZIP into the Unity project root first.
  exit /b 1
)
for %%F in (
 "Assets\Scripts\Ores\Mining\MiningHudFlyIn.cs"
 "Assets\Scripts\Ores\Mining\MiningHudFlyIn.cs.meta"
 "Assets\Scripts\Ores\Mining\JuicyInventoryButton.cs"
 "Assets\Scripts\Ores\Mining\JuicyInventoryButton.cs.meta"
 "Assets\Scripts\Ores\Mining\MiningInventoryButtonGraphic.cs"
 "Assets\Scripts\Ores\Mining\MiningInventoryButtonGraphic.cs.meta"
 "Assets\Scripts\Ores\Mining\MiningInventoryIconMaskGraphic.cs"
 "Assets\Scripts\Ores\Mining\MiningInventoryIconMaskGraphic.cs.meta"
) do (
  if exist "%ROOT%%%~F" del /q "%ROOT%%%~F"
)
echo Old Inventory visuals code removed. Open Unity, run:
echo Mining Simulator ^> UI ^> Remove Old Inventory Candy Visuals
