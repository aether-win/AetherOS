@echo off
title Aether OS - Reparar red
rem Pide permisos de administrador si hace falta.
net session >nul 2>&1
if errorlevel 1 (
  powershell.exe -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Reparar-Red-Aether.ps1"
echo.
pause
