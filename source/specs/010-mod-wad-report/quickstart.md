# Quickstart — Per-Mod WAD Footprint Visibility

End-to-end manual verification.

## Prerequisites

- A working dev environment (`pnpm install`, Rust toolchain).
- The upstream `ltk_overlay` change has been published (per the no-path-deps rule) and the manager's `Cargo.toml` has been bumped to the new version.
- A configured League installation and storage path.
- A mix of installed mods: at least one single-champion skin and one larger overhaul.

## Setup

```bash
pnpm install
pnpm tauri dev
```

Verify both `pnpm check` and `cargo clippy -p ltk-manager` are clean before starting.

## Scenario 1 — Patch piggyback populates badges (US1, FR-001/002/003, SC-001)

1. With at least 3 mods enabled, click **Patch**.
2. Wait for completion.
3. Confirm every enabled mod card now shows a `WadCountBadge` with a numeric count.
4. Confirm the count for the overhaul mod is visibly higher and rendered in the warning (amber) tier.

## Scenario 2 — Detail popover (US2, FR-006)

1. Hover/click the badge on any mod with a populated count.
2. Confirm the popover shows:
   - Total count (matches badge).
   - Override count.
   - Last computed timestamp.
   - Affected WADs grouped under Champions / Maps / UI / Other.
   - A scrollable list when the count is large (`scroll-fade` on the bottom).
3. Click **Re-analyze** in the popover. Confirm it succeeds and the timestamp updates.

## Scenario 3 — On-demand analyze (US3, FR-007, SC-003)

1. Install a fresh mod that has not yet been included in any patch.
2. Confirm its badge shows the unknown placeholder ("?") with an Analyze affordance.
3. Click Analyze. Confirm:
   - Spinner appears within ~100 ms.
   - Badge populates within 2 s.
   - A success toast fires.
4. Repeat with a corrupted archive (rename or truncate). Confirm:
   - Analyze fails with a user-friendly toast.
   - The badge returns to the prior state (empty) — no crash.

## Scenario 4 — Patcher concurrency (US3 acceptance scenario 2)

1. Start a long-running patch.
2. While it is in progress, click Analyze on a different mod.
3. Confirm the analyze completes without disturbing the patch and without UI freeze.

## Scenario 5 — Content staleness (US4, FR-008, FR-010, SC-004)

1. With a populated badge on mod X, close the app.
2. Externally modify the archive on disk (touch / rewrite the file).
3. Reopen the app and navigate to the library.
4. Confirm mod X's badge shows the stale indicator overlay.
5. Run a patch that includes mod X.
6. Confirm the stale indicator clears and the count refreshes.

## Scenario 6 — Game index staleness (FR-009)

Manually mutate the persisted `current_game_index_fp` in `wad-reports.json` (set to a different value), restart, and confirm every badge is marked stale.

## Scenario 7 — Cache resilience (FR-012, SC-006)

1. Quit the app.
2. Corrupt `{storage_dir}/wad-reports.json` (e.g., truncate to a single `{`).
3. Relaunch the app.
4. Confirm:
   - The library loads without error.
   - Every badge shows the unknown placeholder.
   - A WARN log entry is present at `%APPDATA%\dev.leaguetoolkit.manager\logs\ltk-manager.log` mentioning the dropped cache.

## Scenario 8 — Cold start performance (SC-005)

1. With ≥100 cached reports, time the library route from launch to first paint.
2. Compare against an empty cache. Difference MUST be under 100 ms.

## Verification commands

```bash
pnpm check
cargo clippy -p ltk-manager
cargo fmt -p ltk-manager --check
```

All three MUST pass with zero diagnostics before opening the PR.

## Theme verification

Walk through Scenarios 1–3 with `data-theme="dark"` and again with `data-theme="light"`. The badge tiering and popover MUST be legible in both.
