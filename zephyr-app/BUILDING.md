# Building Zephyr

The portable application starts through Zephyr.exe, a .NET Framework executable with the Zephyr icon. It uses the tested original application with a separate WebView2 menu extension. The previous Tauri implementation remains in ../source for reference.

Required local inputs, excluded from public source: originals/j5nYI6re.exe and originals/R3nzSkin.dll, addon/webview2 DLLs, and the LTK patcher resources. Original hashes are recorded in verification/originals-sha256.json.

The corresponding custom-skin backend source is included in zephyr-app/ltk. Install .NET Framework 4.8, Rust MSVC and Visual Studio Build Tools 2022. Run BUILD.cmd to compile the extension, backend and launcher. Build in-game-source/R3nzSkin/R3nzSkin.vcxproj with Configuration=RiotGamesServers, Platform=x64, and copy the DLL to customized/R3nzSkin.dll. Update its SHA-256 in customized/menu-sha256.json and launcher/ZephyrLauncher.cs before rebuilding the launcher.

The portable package contains the corresponding third-party licenses. Runtime assembly and loader filenames remain stable because the application depends on them.

## Update notifications

The menu extension also compiles addon/UpdateChecker.cs and references System.Net.Http.dll. The current version is defined in UpdateChecker.CurrentVersion; keep it aligned with the launcher assembly version and interface footer for each release.

Publish a stable release with a numeric tag such as v1.0.2 and upload Zephyr-v1.0.2.zip before marking it latest. Drafts, prereleases, older versions and releases without the matching uploaded ZIP do not produce an update notice.

Run interface tests with Node: node --test --test-isolation=none ui/*.test.mjs. To test release validation, compile verification/UpdateCheckerTests.cs together with addon/UpdateChecker.cs using the .NET Framework C# compiler, referencing System.dll, System.Core.dll, System.Web.Extensions.dll and System.Net.Http.dll, then run the resulting console executable.

Version numbers are changed only when the maintainer explicitly requests a new version. The current application and release are 1.0.1.

## League 16.20 compatibility port

The active `in-game-source` menu ports only the Champion skin-array layout correction from upstream commit `bf481c06b4dd80a6ddadf334a5804519af3429be` (release 16.20 points to `7cf380a74ba90fbf979572e38d080d5cd084cd7b`). The padding changes from `0xB0` to `0xC0`. The other upstream commit adds turret/minion persistence and is intentionally outside this minimal compatibility port. Upstream offsets, memory code and injector sources are unchanged between 16.19 and 16.20.

Zephyr's theme, `NextHook` export, original host, reflection integration and loader filenames are preserved. `original-source` and the legacy `source` tree remain historical references. The `--original-menu` option still selects the unchanged 16.19 DLL and is not a 16.20 compatibility fallback.

The recorded customized DLL hash is from a local RiotGamesServers/x64 build with MSVC 14.44.35207. Build paths/toolchains can change the binary hash: when preparing a package, record the actual rebuilt DLL hash in both `customized/menu-sha256.json` and `launcher/ZephyrLauncher.cs`, then rebuild the launcher. Keep the original host/DLL hashes unchanged. This source port does not replace the already published ZIP or its release manifest.

`portable-menu-check.yml` compiles the active menu and launcher, verifies the required `NextHook` export and runs interface regression tests on pull requests. Its DLL artifact is a build result, not a complete portable package. The full package still requires the external inputs above. Compilation does not establish in-match compatibility; a game test remains required before release.
