## Zephyr v1.0.0

Windows skin manager for League of Legends, featuring custom skin selection before a match and an in-game skin menu.

### Current build

- Automatic League folder detection and saving on the first launch, respecting existing settings.
- Centered Zephyr logo during startup, followed by a smooth fade into the interface.
- Original Preferences bar covered during startup; original injector controls stay intact.
- Automatic update checks at startup and every four hours while visible.
- English and Portuguese update notices with a button to open the new GitHub release.
- Manual update check in Settings; connection failures do not interrupt skin loading.
- Downloading and installation remain manual. Install this version once to receive future update notices.

### Features

- Integrated launcher with a Start button.
- Custom skin import and selection.
- In-game skin selection menu.
- English and Portuguese interface.
- Executable launcher with the Zephyr icon and a clean portable folder.
- Black and white in-game menu.
- Bundled components; separate R3nzSkin or LTK Manager installations are not required.

### Downloads

- **Windows app:** download `Zephyr-v1.0.0.zip`, extract the complete folder and open `Zephyr.exe`.
- **Source code:** GitHub provides ZIP and TAR.GZ source archives below.
- **Verification and licenses:** file checksums are included inside the application package, and component licenses are in its `licenses` folder.

### Usage

If skins do not apply on the first try, close League of Legends, apply them again in Zephyr and relaunch the game.

### Transparency

Application, loader and modified menu source are available in this repository. Current build instructions are in `zephyr-app/BUILDING.md`. The release publication workflow is public.

The LTK patcher is a bundled, precompiled third-party component. No independent security audit has been completed. See SECURITY.md for verification details and limitations.

### Credits and licensing

Based on work by hydy100, League Toolkit contributors, R3nzTheCodeGOD, B3akers and R3nzSkin contributors.

See LICENSE, R3NZ-LICENSE, LTK-PATCHER-LICENSE.md and THIRD_PARTY_NOTICES.md for component licenses and attribution.

### Startup polish

The centered logo keeps its original aspect ratio and remains visible for at least 1.5 seconds, then fades once the interface is ready. This package remains version 1.0.0.
A thin white progress bar below the startup logo fills during the loading hold and fades with the logo.
