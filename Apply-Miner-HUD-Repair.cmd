@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Apply-Miner-HUD-Repair.ps1"
if errorlevel 1 (
  echo HUD repair failed. Read the error above.
  pause
  exit /b 1
)
pause
