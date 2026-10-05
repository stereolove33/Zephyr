@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0BUILD.ps1"
if errorlevel 1 (
  echo Build failed. Copy the error above.
  pause
  exit /b 1
)
pause
