# Third-party notices

| Component | Source | License / notice |
|---|---|---|
| LTK Manager 1.25.0 | LeagueToolkit/ltk-manager, commit 3b78087aaabbd6b4afb44ec1e6e73b7dd0916d19 | GPL-3.0-or-later, `LICENSE` |
| R3nzSkin 16.19 source | User-provided archive, copyright R3nzTheCodeGOD and B3akers | MIT, `R3NZ-LICENSE` |
| ltk_patcher_host.exe and ltk_patcher_dll.dll | Resources in the supplied LTK Manager archive | Separate license, `LTK-PATCHER-LICENSE.md` |
| R3nz version used for testing | Attributed to hydy100 by the user | Credit retained. Specific binary redistribution permission not confirmed |
| Icon artwork | User-provided image with background removed | Provenance and public distribution rights not confirmed |

Prebuilt binaries and the unverified artwork are excluded from this public source archive. The MIT notice remains in the source and license files even when the copyright footer is not displayed in the menu.

Other dependencies retain their own licenses. Manifests and lockfiles record their versions. Before a binary release, generate the dependency inventory with `pnpm generate:licenses` (requires cargo-about) and review non-Cargo resources as well.

The patcher license permits redistribution with another app under its conditions, including removal of LeagueToolkit's code signature. Do not redistribute the original signed files with Zephyr without the written permission described in that license. Zephyr does not claim endorsement by its upstream projects.
