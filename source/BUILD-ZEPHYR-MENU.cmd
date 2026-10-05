@echo off
cd /d "%~dp0"
where msbuild.exe >nul 2>&1
if errorlevel 1 (
 echo Open the Visual Studio x64 Native Tools terminal.
 exit /b 1
)
msbuild.exe "zephyr-menu-source\R3nzSkin\R3nzSkin.vcxproj" /m /p:Configuration=RiotGamesServers /p:Platform=x64 /p:TargetName=ZephyrMenu /p:OutDir="%CD%/zephyr-menu-build/"
if errorlevel 1 exit /b 1
if not exist "zephyr-menu-build\ZephyrMenu.dll" exit /b 1
if not exist "src-tauri\resources\r3nz\R3nzSkin.original.dll" copy /y "src-tauri\resources\r3nz\R3nzSkin.dll" "src-tauri\resources\r3nz\R3nzSkin.original.dll"
if errorlevel 1 exit /b 1
copy /y "zephyr-menu-build\ZephyrMenu.dll" "src-tauri\resources\r3nz\R3nzSkin.dll"
if errorlevel 1 exit /b 1
call UPDATE-ZEPHYR.cmd
