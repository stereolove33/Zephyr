# Phase 0 Research — Per-Mod WAD Footprint Visibility

All Technical Context fields resolved. No remaining NEEDS CLARIFICATION markers.

## Decision 1 — Where bucketing logic lives

**Decision**: Add a new public API on `OverlayBuilder` (upstream `ltk_overlay`) that exposes per-mod `ModWadReport` values, computed by reusing the existing `collect_single_mod_metadata` (`builder/metadata.rs:17-170`) and `GameIndex::find_wads_with_hash` (used in `builder/resolve.rs:17-54`) on a per-mod basis before the cross-mod merge.

**Rationale**: The bucketing answer the user wants ("which game WADs does this mod's overrides land in") is already computed for the merged set. Lifting it to per-mod is a small refactor and load-order independent by construction. Doing it upstream keeps the manager free of duplicated bucketing logic that would inevitably drift.

**Alternatives considered**:

- **Reimplement bucketing in `ltk-manager`**: Rejected — duplicates non-trivial logic, will rot when WAD path conventions change.
- **Pre-merge dry-run via existing build pipeline per mod**: Rejected — quadratic in number of mods, unnecessarily expensive when we already have the data inside one `build()` call.

## Decision 2 — How reports get into the cache

**Decision**: Two paths feed the same `WadReportStore`:

1. **Patch piggyback**: After `OverlayBuilder::build()` succeeds in `src-tauri/src/overlay/mod.rs`, drain the per-mod report vec and upsert into the store.
2. **On-demand analyze**: A new Tauri command `analyze_mod_wads(mod_id)` constructs a single-mod `OverlayBuilder` (or calls a dedicated upstream `analyze_single_mod` helper) against the current `GameIndex` without writing any overlay files.

**Rationale**: Most reports come for free with normal patcher use. The on-demand path closes the "never patched" gap (US3) without forcing users to wait on a full patch.

**Alternatives considered**:

- **Patch-only**: Rejected — first-run UX shows nothing useful until the user patches.
- **Eager analyze on every install**: Rejected — wasteful; users may install many mods before they ever patch.

## Decision 3 — Cache shape and persistence

**Decision**: New file `{storage_dir}/wad-reports.json` containing a `BTreeMap<ModId, CachedWadReport>`. Loaded once at startup into a Tauri-managed `WadReportState(Mutex<WadReportStore>)`. Writes use the existing temp-file-then-rename pattern from `mods/mod.rs`. Schema versioned with a top-level `version: u32` for future migrations.

**Rationale**: Mirrors the existing `library.json` pattern, keeps the hot library index lean, and is trivially droppable on corruption (FR-012).

**Alternatives considered**:

- **Embed inside `library.json`**: Rejected — bloats every library read with per-WAD lists; reports can be large for overhauls.
- **SQLite**: Rejected — overkill, adds a dependency, no relational queries needed.

## Decision 4 — Invalidation strategy

**Decision**: Each `CachedWadReport` carries two fingerprints:

- `content_fingerprint: u64` — value of `ModContentProvider::content_fingerprint(&self)`.
- `game_index_fingerprint: u64` — a stable hash of the current `GameIndex` (set of WAD names + their chunk hashes; computed once per process from upstream and cached).

A report is **stale** when either fingerprint differs from the current value. Stale reports are still displayed (with a stale indicator per FR-008/FR-009), and refreshed automatically the next time the mod participates in a successful patch (FR-010) or when the user triggers analyze.

The existing reconcile pass at `mods/mod.rs:441-447` already detects external archive edits — it gains one extra step that bumps stored content fingerprints (or clears reports whose content hash no longer matches).

**Rationale**: Two independent fingerprints capture both axes of staleness without requiring a full recompute on every library open.

**Alternatives considered**:

- **mtime-only invalidation**: Rejected — misses game updates entirely.
- **Drop on any mismatch**: Rejected — loses the user-visible count; the spec wants to display a stale value rather than a placeholder.

## Decision 5 — UI surface

**Decision**: A new `WadCountBadge` rendered inside `ModCard.tsx` next to `LayerBadge` (lines 309 grid view / 149 list view). It owns its own data via `useModWadReport(mod.id)` and an `Analyze` mutation. Hover/click opens a wrapped `Popover` (`@/components/Popover`) — not a `Tooltip`, since content is grouped, scrollable, and contains an action button.

Color tiering uses existing tokens:

- 1–5 WADs: `surface-*` neutral
- 6–25 WADs: `accent-*`
- 26+ WADs: amber/warning token

Grouping in the popover splits WAD paths by top-level directory into Champions / Maps / UI / Other.

**Rationale**: Matches the user's "scan and prune" workflow (US1) and the drill-down workflow (US2). Hooks-in-component complies with the "no prop drilling" rule. Popover (not Tooltip) is mandatory for non-trivial content.

**Alternatives considered**:

- **Tooltip with full list**: Rejected — not scrollable, no action affordance, fails accessibility for long lists.
- **Dedicated route/dialog**: Rejected — overkill for a peek-and-go interaction.

## Decision 6 — Threading model

**Decision**: `analyze_mod_wads` is a `#[tauri::command] async fn` that performs the upstream call inside `tokio::task::spawn_blocking`. Patch-time report capture happens on the existing patcher worker thread; the only main-thread work is the lock-and-upsert into `WadReportStore`.

**Rationale**: Constitution V mandates I/O off the main thread. Analyze can run concurrently with the patcher because it neither writes overlay state nor takes the patcher mutex.

**Alternatives considered**:

- **Block on the patcher mutex**: Rejected — would freeze the UI during long patches.
