## Zephyr v1.0.1

Windows skin manager for League of Legends, with custom-skin management and an in-game skin menu.

### What changed

- **Close game to reconnect:** closes only the running League match so you can reconnect quickly from the client. Keep Zephyr running.
- **Custom skin organization:** collapse or expand the custom-skin section and drag mods up or down. The section state and order are saved locally; reordering does not change which mods are enabled.
- **Taskbar icon:** uses the Zephyr launcher and icon when pinned. If an old pin still shows the previous icon, unpin it and pin Zephyr again.
- **Interface polish:** aligned custom-skin and Settings arrows, improved title capitalization and added a Discord link beside the version.

### Included features

- Official skin selection through the in-game menu, with a black and white theme.
- Import and manage `.modpkg` and `.fantome` custom skins, with package checks; `.modpkg` checks remain partial.
- English and Portuguese interface.
- Automatic League folder detection on first launch.
- Centered startup logo, thin progress bar and fade into the interface.
- Automatic update notices and a manual check in Settings.

### Download and update

Download **Zephyr-v1.0.1.zip**, extract the complete folder and open **Zephyr.exe**. Keep its runtime, resources and licenses beside the executable.

Version 1.0.0 builds with update checks will show this release as available. **View update** opens this page; downloading and installing the package are manual. Your settings and custom library remain in their existing local storage when run under the same Windows account.

If the in-game menu does not appear, use **Close game to reconnect**, then click **Reconnect** in the League client. The upstream skin-changing component is unchanged in this release.

GitHub also provides the two source archives below. Application checksums are inside `resources/SHA256SUMS.txt`, and component licenses are in `licenses`.

### Support

[Join the Zephyr Discord](https://discord.gg/fjfssqYZx) for bug reports and update news.

### Transparency and credits

The launcher, interface, integration, custom-skin backend and modified menu source are available in this repository. See [build instructions](https://github.com/stereolove33/Zephyr/blob/main/zephyr-app/BUILDING.md) and [security details](https://github.com/stereolove33/Zephyr/blob/main/SECURITY.md). The original application and LTK patcher remain precompiled inputs; no independent security audit has been completed.

Credits: hydy100, LeagueToolkit contributors, R3nzTheCodeGOD, B3akers and R3nzSkin contributors. Component licenses and attribution are documented in `LICENSE` and `docs/THIRD_PARTY_NOTICES.md`.
