@echo off
cd /d "%~dp0"
if not exist "src-tauri\resources\r3nz\R3nzSkin.original.dll" exit /b 1
copy /y "src-tauri\resources\r3nz\R3nzSkin.original.dll" "src-tauri\resources\r3nz\R3nzSkin.dll"
if errorlevel 1 exit /b 1
call UPDATE-ZEPHYR.cmd
