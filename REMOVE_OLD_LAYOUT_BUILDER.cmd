@echo off
setlocal
set "PROJECT_ROOT=%~dp0"
del /q "%PROJECT_ROOT%Assets\Scripts\Ores\Editor\MiningJuicyUpgradePanelSetupMenu.cs" 2>nul
del /q "%PROJECT_ROOT%Assets\Scripts\Ores\Editor\MiningJuicyUpgradePanelSetupMenu.cs.meta" 2>nul
echo Old upgrade-panel layout builder removed. Scene layout remains unchanged.
pause
