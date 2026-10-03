# Implementation Plan: Per-Mod WAD Footprint Visibility

**Branch**: `010-mod-wad-report` | **Date**: 2026-04-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/010-mod-wad-report/spec.md`

## Summary

Surface, on every mod card, a badge showing how many game WADs the mod affects, with a popover detail view listing the specific WADs grouped by category. Reports are computed for free as a byproduct of real patches and on demand for never-patched mods, persisted to a sidecar cache, and invalidated when either the mod content fingerprint or the game WAD index fingerprint changes.

The per-mod bucketing is added to the upstream `ltk_overlay` crate (published first, no path deps), exposed through `OverlayBuilder` so `ensure_overlay()` can drain reports after a successful build. A new `WadReportStore` Tauri-managed state persists `{storage_dir}/wad-reports.json`. Two new Tauri commands (`get_mod_wad_report`, `analyze_mod_wads`) feed a `useModWadReport` hook consumed directly by a new `WadCountBadge` rendered inside `ModCard`.

## Technical Context

**Language/Version**: Rust (latest stable, workspace) + TypeScript 5 / React 19 (strict)
**Primary Dependencies**: Tauri v2, `ltk_overlay` (workspace external at `X:\dev\league-mod\crates\ltk_overlay`), TanStack Query, Zustand, base-ui (wrapped via `@/components`), `@tanstack/react-form`, Zod
**Storage**: JSON sidecar `{storage_dir}/wad-reports.json` written via temp-file-then-rename, alongside the existing `library.json`
**Testing**: Manual end-to-end via `pnpm tauri dev`; `pnpm check`; `cargo clippy -p ltk-manager`; `cargo fmt -p ltk-manager --check`
**Target Platform**: Windows / Linux / macOS desktop (Tauri WebView)
**Project Type**: Desktop app (Rust backend + React frontend, single repo)
**Performance Goals**: Library opens with <100 ms overhead from cache load (SC-005); on-demand single-mod analysis completes in <2 s (SC-003); UI never blocks during analysis or patch
**Constraints**: Per-mod WAD footprint MUST be load-order-independent; cache MUST tolerate corruption without crashing; report computation MUST stay off the main Tauri thread; upstream `ltk_overlay` change MUST ship to crates.io before manager bumps the dep (no path deps)
**Scale/Scope**: Libraries up to a few hundred mods; reports up to ~500 affected WADs per mod in pathological cases

## Constitution Check

_GATE: Must pass before Phase 0 research. Re-check after Phase 1 design._

| Principle                         | Compliance                                                                                                                                                                                                                                                                                                                          |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| I. Code Quality & Maintainability | New backend logic implemented as methods on `OverlayBuilder` and `WadReportStore` (no free functions). Frontend imports via `@/components` and module barrels. New `WadCountBadge` consumes hooks directly — no prop drilling. No premature abstractions.                                                                           |
| II. Type Safety & Error Handling  | New commands return `IpcResult<T>`; reuse existing `ErrorCode` variants (`Modpkg`, `Fantome`, `InternalState`, `MutexLockFailed`). Frontend uses `invokeResult<T>` and `Result<T,E>` discriminated union; errors classified with `hasErrorCode()`. New TS types in `lib/tauri.ts` mirror Rust structs 1:1.                          |
| III. Testing Standards            | Manual E2E scenarios documented in `quickstart.md`. `pnpm check` and `cargo clippy` are part of the verification step. PR description will include reproduction steps for the staleness flow.                                                                                                                                       |
| IV. UX Consistency                | Badge and popover built from wrapped `@/components` (`Popover`, `Tooltip`, `IconButton`). Toasts surfaced via `useToast()` for analyze success/failure. Loading state (spinner) shown during analyze. Tested in dark + light themes. Color tiering uses `accent-*` / amber tokens, no hard-coded colors.                            |
| V. Performance                    | Cache load is O(n) JSON parse on app startup, off-thread. Per-mod hash → WAD lookups happen inside the existing patcher worker thread (no main-thread blocking). On-demand analyze runs on a Tauri async command (Tokio), not the UI thread. WadCountBadge subscribes via TanStack Query, so re-renders are scoped to single cards. |

**Result**: PASS — no violations, Complexity Tracking section omitted.

## Project Structure

### Documentation (this feature)

```text
specs/010-mod-wad-report/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (Tauri command contracts)
│   └── tauri-commands.md
└── checklists/
    └── requirements.md  # Spec quality checklist (already created)
```

### Source Code (repository root)

```text
src-tauri/src/
├── overlay/
│   └── mod.rs                      # MODIFY: drain ModWadReport vec from OverlayBuilder, hand to store
├── mods/
│   ├── mod.rs                      # MODIFY: extend reconcile to invalidate stale reports by content fp
│   └── wad_reports.rs              # NEW: WadReportStore (load/save/get/upsert/invalidate)
├── commands/
│   ├── mod.rs                      # MODIFY: re-export new commands
│   └── mods.rs                     # MODIFY: add get_mod_wad_report, analyze_mod_wads
├── state.rs                        # MODIFY: add WadReportState wrapper
├── error.rs                        # (no change — reuse existing variants)
└── main.rs                         # MODIFY: register WadReportState + new handlers

src/
├── lib/
│   └── tauri.ts                    # MODIFY: ModWadReport type, api.getModWadReport, api.analyzeModWads
├── modules/library/
│   ├── api/
│   │   ├── keys.ts                 # MODIFY: libraryKeys.wadReport(modId)
│   │   ├── useModWadReport.ts      # NEW: query hook
│   │   ├── useAnalyzeModWads.ts    # NEW: mutation hook
│   │   └── index.ts                # MODIFY: barrel export
│   ├── components/
│   │   ├── ModCard.tsx             # MODIFY: render WadCountBadge near LayerBadge
│   │   ├── WadCountBadge.tsx       # NEW: badge + popover
│   │   └── index.ts                # MODIFY: barrel export
│   └── index.ts                    # MODIFY: barrel re-export
└── modules/patcher/api/
    └── useOverlayProgress.ts       # MODIFY: invalidate libraryKeys.wadReports() on Complete

# Upstream (separate repo, must publish first)
X:\dev\league-mod\crates\ltk_overlay\src\
├── builder/
│   ├── metadata.rs                 # MODIFY: retain per-mod metadata for report generation
│   ├── resolve.rs                  # MODIFY: expose per-mod hash → wad distribution
│   └── mod.rs                      # MODIFY: pub fn take_mod_wad_reports() -> Vec<ModWadReport>
└── lib.rs                          # MODIFY: pub use ModWadReport
```

**Structure Decision**: Existing single-repo Tauri layout — Rust under `src-tauri/src/` and React under `src/modules/library/`. New work fits the established `mods/` and `library/` module boundaries; one new Rust file (`wad_reports.rs`), one new React component, two new hooks, two new Tauri commands. The upstream `ltk_overlay` change is tracked separately and is a strict prerequisite to the manager-side work.

## Phase 0: Outline & Research

See [research.md](./research.md). All NEEDS CLARIFICATION resolved before this gate.

## Phase 1: Design & Contracts

See [data-model.md](./data-model.md), [contracts/tauri-commands.md](./contracts/tauri-commands.md), and [quickstart.md](./quickstart.md).
