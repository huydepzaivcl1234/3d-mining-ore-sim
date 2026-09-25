@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Remove-Duplicate-Ore-Scripts.ps1"
echo.
pause
