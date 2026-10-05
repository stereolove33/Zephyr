# Zephyr <img src=zephyr-app/ui/zephyr-icon.png width=40 alt=Zephyr>

A portable Windows app for selecting official and custom League of Legends skins.

[Join the Zephyr Discord](https://discord.gg/fjfssqYZx)

## Features

- **Official skins:** select skins through the in-game menu.
- **Custom skins:** import `.modpkg` and `.fantome` packages, manage your collection and enable selected mods before a match.
- **Custom mod checks:** check imported packages for issues and conflicts; `.modpkg` checks are partial.

## Download and use

[Download Zephyr](https://github.com/stereolove33/Zephyr/releases/latest), extract the complete ZIP into a new folder and open **Zephyr.exe**.

Keep `runtime`, `resources` and `licenses` beside the executable.

The package includes the application components. Separate R3nzSkin and LTK Manager installations are not required.

The app requires Windows x64, .NET Framework 4.8 and Microsoft Edge WebView2 Runtime. Choose English or Portuguese in Settings.

Select your skins before entering a match. If they do not apply on the first try, close League of Legends, apply them again in Zephyr and relaunch the game.

On the first launch, Zephyr attempts to detect and save the League folder if no path is configured. Existing paths are respected. If detection fails, use Browse or Auto-detect in Settings.

Zephyr checks for updates when opened and every four hours while visible. Use **Settings → Check for updates** to check manually. **View update** opens the new GitHub release page; downloading and installation are manual. Download the current application package once to receive future notifications; earlier builds of 1.0.0 do not include this feature.

The release download list contains the application ZIP and GitHub's two source archives: ZIP and TAR.GZ.

## Repository layout

- [zephyr-app](zephyr-app): current launcher, WebView2 interface, integration, custom-skin backend and modified in-game menu source
- [zephyr-app/BUILDING.md](zephyr-app/BUILDING.md): current build requirements and instructions
- [docs](docs): build overview, release checksums and component notices
- `source`: preserved earlier Tauri implementation

The portable application folder contains `Zephyr.exe`, `README.txt`, `runtime`, `resources` and `licenses`. Source code and build tools belong to the repository, rather than the application folder.

## Build

Install .NET Framework 4.8, Rust with the MSVC target and Visual Studio Build Tools 2022 with Desktop development with C++. Prepare the external component inputs listed in [zephyr-app/BUILDING.md](zephyr-app/BUILDING.md), then run:

```bat
zephyr-app\BUILD.cmd
```

To rebuild only the executable launcher:

```bat
zephyr-app\BUILD-LAUNCHER.cmd
```

The in-game menu has a separate C++ build described in the build instructions. The current version uses an existing original application with a menu extension. Rebuilding the integration requires the original component inputs.

## Transparency and verification

The current launcher, integration, interface, custom-skin backend and modified in-game menu source are available in [zephyr-app](zephyr-app).

- **Inspect the source:** review the files and Git commit history.
- **Build locally:** follow [zephyr-app/BUILDING.md](zephyr-app/BUILDING.md), including its required external components.
- **Review publication:** the [release workflow](.github/workflows/publish-portable.yml) verifies the assembled portable ZIP before publishing it. It publishes the locally prepared package and does not rebuild every component.
- **Check source integrity:** [zephyr-app/SHA256SUMS.txt](zephyr-app/SHA256SUMS.txt) records current source-file hashes.
- **Check the download:** [SHA256SUMS.txt](docs/SHA256SUMS.txt) records the current release ZIP hash.
- **Check application integrity:** `resources/SHA256SUMS.txt` inside the extracted package records its application files. The release package hash is recorded in [zephyr-app/release-manifest.json](zephyr-app/release-manifest.json).
- **Read component notices:** see [THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md).

Local checks covered launcher and extension compilation, 26 interface tests, update release validation and startup-cover checks, preparation for both in-game menus and package integrity. The user tested the in-game menu. The original application and LeagueToolkit patcher remain precompiled components.

No independent security audit has been completed. Source availability and matching hashes support verification but do not guarantee malware-free software. See [SECURITY.md](SECURITY.md).

## Credits

- **stereolove33**: Zephyr maintainer, launcher, integration and customization
- **hydy100**: R3nz version used as a reference and during testing. This credit does not establish authorship of all source code or permission to redistribute that version's binaries.
- **R3nzTheCodeGOD, B3akers and contributors**: original menu, under the MIT license
- **LeagueToolkit and contributors**: LTK Manager and custom skin components. Manager code uses GPL-3.0-or-later. Patcher binaries have a separate license.

Zephyr is not an official or endorsed release of those projects and is not affiliated with Riot Games.

## Licensing

LTK Manager-derived code and corresponding changes use **GPL-3.0-or-later**. R3nzSkin retains its **MIT** license. Patcher binaries are governed separately.

See [LICENSE](LICENSE), [R3NZ-LICENSE](docs/licenses/R3NZ-LICENSE), [LTK-PATCHER-LICENSE.md](docs/licenses/LTK-PATCHER-LICENSE.md) and the component notices. The downloaded application includes a `licenses` folder.
