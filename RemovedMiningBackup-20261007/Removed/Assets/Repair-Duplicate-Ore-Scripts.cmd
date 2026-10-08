@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Repair-Duplicate-Ore-Scripts.ps1" -ProjectRoot "%~dp0"
echo.
pause
