# Tasks: Per-Mod WAD Footprint Visibility

**Feature**: `010-mod-wad-report`
**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Data Model**: [data-model.md](./data-model.md) | **Contracts**: [contracts/tauri-commands.md](./contracts/tauri-commands.md)

> Tests are not requested in the spec; this feature ships with manual E2E verification per `quickstart.md`. No automated test tasks generated.

---

## Phase 1: Setup

- [x] T001 Verify upstream `ltk_overlay` crate has been published with the new `ModWadReport` API and bump the dependency in `X:\dev\ltk-manager\src-tauri\Cargo.toml` to the published version (per the no-path-deps rule). Run `cargo check -p ltk-manager` and confirm a clean build before any further work.

---

## Phase 2: Foundational (blocking prerequisites)

- [x] T002 Add `ModWadReport` and `CachedWadReport` Rust structs in a new file `X:\dev\ltk-manager\src-tauri\src\mods\wad_reports.rs`, deriving `Serialize`/`Deserialize` and serializing `u64` fingerprints as JSON strings to avoid JS precision loss (per `data-model.md`).
- [x] T003 Implement `WadReportStore` in `X:\dev\ltk-manager\src-tauri\src\mods\wad_reports.rs` with methods `load`, `save` (temp-file-then-rename), `get` (fills `is_stale`), `upsert`, `upsert_many`, `invalidate_by_content`, `set_current_game_index_fp`, `prune_orphans`. All business logic as methods on the struct, not free functions. Tolerate missing/corrupt cache files by starting empty and logging at WARN.
- [x] T004 Wire `mod wad_reports;` into `X:\dev\ltk-manager\src-tauri\src\mods\mod.rs` and re-export `ModWadReport`, `WadReportStore`.
- [x] T005 Add `WadReportState(pub Mutex<WadReportStore>)` to `X:\dev\ltk-manager\src-tauri\src\state.rs` following the existing `SettingsState` pattern.
- [x] T006 In `X:\dev\ltk-manager\src-tauri\src\main.rs`, load `WadReportStore` from `{storage_dir}/wad-reports.json` during setup, register it as Tauri-managed `WadReportState`, and call `set_current_game_index_fp` once the game index is loaded.
- [x] T007 Add the `ModWadReport` TypeScript interface in `X:\dev\ltk-manager\src\lib\tauri.ts` with `u64` fields typed as `string` to match the Rust serialization, mirroring the contract in `contracts/tauri-commands.md`.

---

## Phase 3: User Story 1 — At-a-glance WAD count badge (P1)

**Goal**: Every mod that has been included in a successful patch shows a numeric, color-tiered WAD-count badge on its card.

**Independent Test**: Run a patch with a mix of small and large mods; confirm each card shows a count and that overhauls land in the warning tier.

- [x] T008 [US1] In upstream `X:\dev\league-mod\crates\ltk_overlay\src\builder\mod.rs` (or wherever `OverlayBuilder` lives), add `pub fn take_mod_wad_reports(&mut self) -> Vec<ltk_overlay::ModWadReport>` that drains the per-mod reports collected during `build()`. (Prerequisite published in T001 — this task tracks the upstream change that must already be in place.)
- [x] T009 [US1] In `X:\dev\ltk-manager\src-tauri\src\overlay\mod.rs`, after a successful `OverlayBuilder::build()` in `ensure_overlay()`, call `take_mod_wad_reports()`, convert to `ModWadReport`, lock `WadReportState`, and call `upsert_many(reports)`. Surface any persist error via the existing logging path without failing the patch.
- [x] T010 [US1] Add `#[tauri::command] pub async fn get_mod_wad_report(state: State<'_, WadReportState>, mod_id: String) -> IpcResult<Option<ModWadReport>>` in `X:\dev\ltk-manager\src-tauri\src\commands\mods.rs`. Lock the store, call `get`, return the option. Use `MutexResultExt`.
- [x] T011 [US1] Re-export `get_mod_wad_report` from `X:\dev\ltk-manager\src-tauri\src\commands\mod.rs` and register it in the `generate_handler![]` macro in `X:\dev\ltk-manager\src-tauri\src\main.rs`.
- [x] T012 [P] [US1] Add `api.getModWadReport(modId): Promise<Result<ModWadReport | null, AppError>>` to the `api` object in `X:\dev\ltk-manager\src\lib\tauri.ts` using `invokeResult<T>()`.
- [x] T013 [P] [US1] Add `wadReport: (modId: string) => ['library', 'wad-report', modId] as const` and `wadReports: () => ['library', 'wad-report'] as const` to `X:\dev\ltk-manager\src\modules\library\api\keys.ts`.
- [x] T014 [US1] Create `X:\dev\ltk-manager\src\modules\library\api\useModWadReport.ts` exposing a `useModWadReport(modId)` TanStack Query hook with `staleTime: 5 * 60 * 1000` and `queryFn: queryFnWithArgs(api.getModWadReport, modId)`.
- [x] T015 [US1] Export `useModWadReport` from `X:\dev\ltk-manager\src\modules\library\api\index.ts` and re-export from `X:\dev\ltk-manager\src\modules\library\index.ts`.
- [x] T016 [US1] Create `X:\dev\ltk-manager\src\modules\library\components\WadCountBadge.tsx`. Component takes only `{ modId: string }`, calls `useModWadReport(modId)` directly (no prop drilling), and renders:
  - Loading: `<Skeleton />` placeholder.
  - No data: muted "?" pill.
  - Has data: numeric count pill with color tier (1–5 neutral via `surface-*`, 6–25 via `accent-*`, 26+ via amber/warning token). Use only design tokens, no hard-coded colors. Wrap in a `Tooltip` showing "Affects N WAD(s)" for the simple state.
    Imports must come from `@/components` and `@/modules/library`, never raw HTML or `@base-ui-components/react`.
- [x] T017 [US1] Export `WadCountBadge` from `X:\dev\ltk-manager\src\modules\library\components\index.ts`.
- [x] T018 [US1] Render `<WadCountBadge modId={mod.id} />` in `X:\dev\ltk-manager\src\modules\library\components\ModCard.tsx` next to `LayerBadge` in both the grid view (~line 309) and the list view (~line 149). Read the file before editing per the project rule.
- [x] T019 [US1] In `X:\dev\ltk-manager\src\modules\patcher\api\useOverlayProgress.ts`, when the `Complete` overlay stage fires, call `queryClient.invalidateQueries({ queryKey: libraryKeys.wadReports() })` so badges refresh after a patch.

**Checkpoint**: After Phase 3, US1 is fully shippable as an MVP — patch once, see counts. Verify Scenario 1 in `quickstart.md`.

---

## Phase 4: User Story 2 — Detail popover (P2)

**Goal**: Clicking the badge opens a popover with grouped WAD list and re-analyze action.

**Independent Test**: Click a populated badge and verify the popover lists expected WADs grouped by category, scrolls when long, and exposes re-analyze.

- [x] T020 [US2] Add a `groupWadsByCategory(wads: string[]): { champions: string[]; maps: string[]; ui: string[]; other: string[] }` helper inside `X:\dev\ltk-manager\src\modules\library\components\WadCountBadge.tsx` (private to the component, no separate utility file — three similar lines beats premature abstraction). Group by top-level WAD path segment with sensible mapping for `Champions/`, `Maps/`, `UI/`, fallback `other`.
- [x] T021 [US2] Extend `WadCountBadge.tsx` to wrap the badge pill in a `@/components` `Popover` (not a `Tooltip`). The popover content shows: total count, override count, last-computed timestamp (formatted), and the four category groups, each collapsible. The full WAD list inside each group must be scrollable and use the `.scroll-fade` utility on the bottom.
- [x] T022 [US2] Inside the popover, render a "Re-analyze" `Button` from `@/components` wired to `useAnalyzeModWads()` (added in Phase 5). For Phase 4 standalone testability, gate the button behind `mutation === undefined ? null : <Button …/>` so the file compiles before Phase 5 lands. Show a `Spinner` from `@/components` while the mutation is pending.
- [x] T023 [US2] Verify the popover renders correctly in both dark and light themes per Constitution IV by manually toggling `data-theme` during dev.

**Checkpoint**: Verify Scenario 2 in `quickstart.md`.

---

## Phase 5: User Story 3 — On-demand analyze (P2)

**Goal**: Users can populate a single mod's badge without running a full patch, even while the patcher is busy.

**Independent Test**: Install a fresh mod, click Analyze, confirm the badge populates within 2 seconds without disturbing any in-progress patch.

- [x] T024 [US3] In upstream `X:\dev\league-mod\crates\ltk_overlay`, expose `pub fn analyze_single_mod(provider: impl ModContentProvider, game_index: &GameIndex) -> Result<ModWadReport, ...>` that runs `collect_single_mod_metadata` + per-hash `find_wads_with_hash` for one mod against the current game index without writing any overlay artifacts. (Already published per T001.)
- [x] T025 [US3] Add `#[tauri::command] pub async fn analyze_mod_wads(settings: State<'_, SettingsState>, library: State<'_, LibraryState>, reports: State<'_, WadReportState>, mod_id: String) -> IpcResult<ModWadReport>` in `X:\dev\ltk-manager\src-tauri\src\commands\mods.rs`. Resolve the mod entry, build the appropriate `ModContentProvider` (reuse the existing helper from `mods/library.rs`), run `ltk_overlay::analyze_single_mod` inside `tokio::task::spawn_blocking`, then `upsert` the result into `WadReportState`. Map archive errors to `Modpkg`/`Fantome`, missing mod to `ModNotFound`, missing game to `LeagueNotFound`. Do NOT take the patcher mutex.
- [x] T026 [US3] Re-export `analyze_mod_wads` from `X:\dev\ltk-manager\src-tauri\src\commands\mod.rs` and register it in `generate_handler![]` in `X:\dev\ltk-manager\src-tauri\src\main.rs`.
- [x] T027 [P] [US3] Add `api.analyzeModWads(modId): Promise<Result<ModWadReport, AppError>>` to `X:\dev\ltk-manager\src\lib\tauri.ts`.
- [x] T028 [US3] Create `X:\dev\ltk-manager\src\modules\library\api\useAnalyzeModWads.ts` with a TanStack mutation that uses `mutationFn(api.analyzeModWads)`. On success, call `qc.setQueryData(libraryKeys.wadReport(report.modId), report)` and fire a success `useToast()`. On error, classify with `hasErrorCode()` from `utils/errors.ts` and show a user-friendly toast (no raw error codes, per Constitution IV).
- [x] T029 [US3] Export `useAnalyzeModWads` from `X:\dev\ltk-manager\src\modules\library\api\index.ts` and re-export from `X:\dev\ltk-manager\src\modules\library\index.ts`.
- [x] T030 [US3] Wire `useAnalyzeModWads()` into the Re-analyze button in `WadCountBadge.tsx` (replacing the gate from T022). Also surface an `Analyze` affordance when the badge is in the no-data state so the user can populate it without opening the popover first.

**Checkpoint**: Verify Scenarios 3 and 4 in `quickstart.md`.

---

## Phase 6: User Story 4 — Staleness detection (P3)

**Goal**: Reports are marked stale when the mod's content fingerprint or the game index fingerprint drifts; auto-refresh on next patch.

**Independent Test**: Modify a mod archive externally, reopen the library, see stale indicator; run a patch that includes the mod, see indicator clear.

- [x] T031 [US4] In `X:\dev\ltk-manager\src-tauri\src\mods\mod.rs`, extend the existing reconcile pass (around lines 441-447 where archive mtime drift is detected) to also call `WadReportStore::invalidate_by_content(mod_id)` for any mod whose archive content fingerprint no longer matches the cached `content_fingerprint`. The cached entry stays in place — only the staleness flag flips on next read.
- [x] T032 [US4] In `X:\dev\ltk-manager\src-tauri\src\mods\mod.rs`, after any uninstall or library mutation, call `WadReportStore::prune_orphans(&valid_ids)` to drop reports for removed mods.
- [x] T033 [US4] In `WadCountBadge.tsx`, when `report.isStale === true`, render a small overlay icon (e.g., `RefreshCw` from `lucide-react`) on the badge with a `Tooltip` reading "Report may be outdated — patch or re-analyze to refresh". Use design tokens only.
- [x] T034 [US4] Confirm Phase 3's patch piggyback path (T009) overwrites stale entries automatically — no extra code needed, just verify in the quickstart Scenario 5 walkthrough.

**Checkpoint**: Verify Scenarios 5 and 6 in `quickstart.md`.

---

## Phase 7: Polish & Cross-Cutting

- [x] T035 [P] Run `cargo fmt -p ltk-manager` and `cargo clippy -p ltk-manager -- -D warnings` from the repo root; resolve any diagnostics in files touched by this feature only.
- [x] T036 [P] Run `pnpm check` from the repo root; resolve any typecheck/lint/format diagnostics in files touched by this feature only.
- [ ] T037 Walk through every scenario in `X:\dev\ltk-manager\specs\010-mod-wad-report\quickstart.md` end-to-end via `pnpm tauri dev`, in both dark and light themes. Note any failures back into the relevant phase rather than patching ad hoc.
- [ ] T038 Verify cache resilience (Scenario 7): corrupt `{storage_dir}/wad-reports.json`, relaunch, confirm graceful fallback and a WARN log line in `%APPDATA%\dev.leaguetoolkit.manager\logs\ltk-manager.log`.
- [ ] T039 Verify cold-start performance (Scenario 8): confirm library opens with ≥100 cached reports adds <100 ms compared to an empty cache.
- [x] T040 Confirm no `@base-ui-components/react` imports were added in module code (search `src/modules/library/`), no raw HTML interactive elements added, no prop drilling of hook state, and no hard-coded color values introduced.

---

## Dependencies

```
T001 (upstream published + dep bump)
  └─> Phase 2 (T002 → T003 → T004 → T005 → T006 → T007)
        └─> Phase 3 / US1 (T008 published; T009 → T010 → T011; T012 [P], T013 [P]; T014 → T015 → T016 → T017 → T018 → T019)
              ├─> Phase 4 / US2 (T020 → T021 → T022 → T023)        — depends on US1's WadCountBadge
              ├─> Phase 5 / US3 (T024 published; T025 → T026; T027 [P]; T028 → T029 → T030)  — independent of US2
              └─> Phase 6 / US4 (T031, T032, T033, T034)            — depends on US1 cache + UI; independent of US2/US3
                    └─> Phase 7 / Polish (T035 [P], T036 [P], T037, T038, T039, T040)
```

US2, US3, and US4 are mutually independent once US1 is in place — they can be picked up by separate developers in parallel.

## Parallel Execution Examples

**Within Phase 3 (US1)** — after T011 lands, these can run together:

- T012 [P] (TS api binding)
- T013 [P] (query keys)

**Across stories after US1**:

- One developer takes Phase 4 (T020–T023)
- A second takes Phase 5 (T025–T030)
- A third takes Phase 6 (T031–T034)

**Phase 7**:

- T035 [P] (Rust lint) and T036 [P] (frontend check) run in parallel.

## Implementation Strategy

**MVP scope**: Phases 1, 2, and 3 only (US1). Ship the badge, populated by patch piggyback. This already satisfies SC-001, SC-002, SC-005, SC-006 and the originating user request ("see which mods touch the most WADs").

**Increment 2**: Phase 5 (US3) — on-demand analyze closes the never-patched gap (SC-003).

**Increment 3**: Phase 4 (US2) — popover detail view for drill-down.

**Increment 4**: Phase 6 (US4) — staleness safeguards (SC-004).

**Increment 5**: Phase 7 polish.

Each increment is independently shippable behind the same feature, so the PR can be split if review burden warrants it; otherwise a single PR after Phase 7 is appropriate given the cohesive UI surface.

---

**Total tasks**: 40
**Per story**: Setup 1 · Foundational 6 · US1 12 · US2 4 · US3 7 · US4 4 · Polish 6
**Parallel-marked tasks**: T012, T013, T027, T035, T036
