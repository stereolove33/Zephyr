@echo off
cd /d "%~dp0"
call npx.cmd --yes pnpm@9.14.2 tauri build
if errorlevel 1 exit /b 1
echo Zephyr installer generated in target\release\bundle\nsis.
pause
