# Building Zephyr

This source archive excludes prebuilt binaries and artwork. Prepare these resources locally before building:

- `src-tauri/resources/ltk_patcher_host.exe` and `ltk_patcher_dll.dll`: compatible patcher binaries under their separate license
- `src-tauri/resources/r3nz/R3nzSkin.dll`: menu built from `zephyr-menu-source/R3nzSkin`
- `injector-ui/zephyr-icon.png` and icons under `src-tauri/icons`: original or authorized artwork in the formats configured in `tauri.conf.json`

The app's own loader is built from `native/official-loader.cpp` by `BUILD.ps1`.

To build the menu alone in the Visual Studio x64 terminal:

```bat
msbuild.exe "zephyr-menu-source\R3nzSkin\R3nzSkin.vcxproj" /m /p:Configuration=RiotGamesServers /p:Platform=x64 /p:TargetName=ZephyrMenu /p:OutDir="%CD%/zephyr-menu-build/"
```

Copy the generated DLL to the resource path above before running `BUILD.cmd`. The script restores the pinned dxbc-spirv submodule, installs dependencies, builds the frontend before generating types, runs checks and creates the installer.

Keep lockfiles and the local `target` folder to reuse previous build outputs. Do not upload `target` to GitHub.

`BUILD-ZEPHYR-MENU.cmd` backs up the current resource DLL, replaces it only after a successful menu build and rebuilds the installer. `RESTORE-MENU.cmd` restores that backup and rebuilds the installer. Restart the game to test a newly installed DLL.
