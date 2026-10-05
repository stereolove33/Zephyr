# Zephyr <img src=source/injector-ui/zephyr-icon.png width=40 alt=Zephyr>

A Windows desktop app for selecting official and custom League of Legends skins.

- Start button and in-game skin menu
- Custom skin import and selection before a match
- English/Portuguese interface and build instructions

## Downloads

[Download](https://github.com/stereolove33/Zephyr/releases) the Windows installer. Source code archives and any published verification files are available alongside each release.

## Build

Install Git, Node.js, Rust with the MSVC target, and Visual Studio Build Tools 2022 with Desktop development with C++, MSVC v143 and a Windows SDK. Open the x64 Native Tools Command Prompt.

See `BUILDING.md` for required local resources. Once they are available:

```bat
source\BUILD.cmd
```

For an incremental app build without rerunning the full test suite:

```bat
source\UPDATE-ZEPHYR.cmd
```

To rebuild and bundle the Zephyr menu, retaining a backup of the previous DLL:

```bat
source\BUILD-ZEPHYR-MENU.cmd
```

Installer output: `source/target/release/bundle/nsis`. Main executable: `source/target/release/zephyr.exe`.

## Credits

- **stereolove33**: Zephyr maintainer; launcher, integration and customization.

- **hydy100**: credit for the R3nz version used as a reference and during testing. This does not establish authorship of all source code or permission to redistribute that version's binaries.
- **R3nzTheCodeGOD, B3akers and contributors**: original menu (MIT).
- **LeagueToolkit and contributors**: LTK Manager and custom skin components. Manager code uses GPL-3.0-or-later. Patcher binaries have a separate license in `LTK-PATCHER-LICENSE.md`.

Zephyr is not an official or endorsed release of those projects and is not affiliated with Riot Games.

## Transparency and verification

Zephyr makes its application code, loader and modified in-game menu source available for public review.

- **Inspect the source:** review the implementation and its Git commit history.
- **Build it yourself:** follow [BUILDING.md](BUILDING.md) using the included build scripts.
- **Review automated builds:** the Windows build workflow is available under the Actions tab.
- **Check file integrity:** compare files against the recorded SHA-256 hashes in [SHA256SUMS.txt](SHA256SUMS.txt).
- **Read the component notices:** authors, licenses and third-party components are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

The LTK patcher is bundled as a precompiled third-party component. An independent security audit has not been completed. Source availability and matching hashes support verification but do not guarantee malware-free software. See [SECURITY.md](SECURITY.md) for the verification status and limitations.

## Licensing

LTK Manager-derived code and corresponding changes: **GPL-3.0-or-later**. R3nzSkin retains its MIT license. Patcher binaries are governed separately. See `LICENSE`, `THIRD_PARTY_NOTICES.md` and component notices.

The original icon was prepared from a user-provided image. Its provenance and public distribution rights have not been confirmed. This public source archive excludes that artwork and prebuilt binaries. The code license does not grant rights to third-party artwork.
