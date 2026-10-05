# Security and verification

Current source is in zephyr-app/. Build requirements and third-party notices are included in this repository.

The current source-file hashes are in zephyr-app/SHA256SUMS.txt. Application-file hashes are included in resources/SHA256SUMS.txt inside the portable package. zephyr-app/release-manifest.json records the downloadable ZIP hash.

The publication workflow checks each uploaded part and the complete ZIP before creating the release. It publishes a locally prepared package and does not rebuild every component. The original application and LeagueToolkit patcher are precompiled inputs.

Local verification covered the launcher and extension builds, 15 interface tests, preparation for the original and customized menus, and package integrity. The user reported successful in-game testing of the customized menu.

No independent security audit has been completed. The precompiled components have not been independently audited by this project. Reproducible builds and complete source-to-binary correspondence have not been independently verified. Matching hashes identify files and do not establish that software is safe.

## Update checks

The app makes an HTTPS request to the public GitHub latest-release API at startup, every four hours while visible, or when manually requested. The User-Agent includes the installed Zephyr version; GitHub also receives normal connection information such as the IP address. Settings and custom-skin files are not included in this request. No GitHub login or access token is required.

Only a newer stable numeric version with the expected uploaded application ZIP is advertised. The release-page URL is constructed for stereolove33/Zephyr; URLs supplied by the release description are not executed. A user click opens the release page in the default browser. The app does not download, replace or execute updates automatically. Network errors leave the skin loaders available.

Version 1.0.0 also passed first-launch path setup tests and a native startup-cover check confirming full coverage of the original Preferences bar, logo rendering, resizing and preservation of the original Start handler. The fade is a WebView visual overlay and does not alter injection logic.
