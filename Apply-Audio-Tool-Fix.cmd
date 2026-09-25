@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Apply-Audio-Tool-Fix.ps1"
if errorlevel 1 (
  echo Audio tool repair failed. Read the error above.
  pause
  exit /b 1
)
pause
