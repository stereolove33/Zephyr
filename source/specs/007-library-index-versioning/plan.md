# Implementation Plan: Library Index Schema Versioning

**Branch**: `007-library-index-versioning` | **Date**: 2026-03-28 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-library-index-versioning/spec.md`

## Summary

Add a `version` integer field to the `library.json` schema and build a sequential migration framework so the app can upgrade indexes written by older versions, reject indexes from newer versions, and back up the original file before migrating. The current ad-hoc folder migration code in `load_library_index` is absorbed into the formal v1 migration step.

## Technical Context

**Language/Version**: Rust (latest stable) backend, TypeScript (strict) frontend
**Primary Dependencies**: serde/serde_json (serialization), tauri v2 (IPC), TanStack Query (frontend state)
**Storage**: Local JSON file (`library.json` in mod storage directory)
**Testing**: `cargo test` for Rust unit tests, manual UI verification
**Target Platform**: Windows (primary), Linux/macOS (secondary) — Tauri desktop app
**Project Type**: Desktop application (Tauri)
**Performance Goals**: Index load/migration must complete in <100ms for typical indexes (<1000 mods)
**Constraints**: Migration must be atomic from the user's perspective — either fully applied or not at all. Backup must exist before any mutation.
**Scale/Scope**: Single local file, typically <1MB, <1000 mod entries

## Constitution Check

_GATE: Must pass before Phase 0 research. Re-check after Phase 1 design._

| Principle                         | Status | Notes                                                                                                                                       |
| --------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| I. Code Quality & Maintainability | PASS   | Business logic stays in `mods/` module as methods. No premature abstractions — migrations are simple functions. Public APIs get `///` docs. |
| II. Type Safety & Error Handling  | PASS   | New `SchemaVersionTooNew` error code added to `ErrorCode` enum. Frontend uses `hasErrorCode()` guard. `IpcResult<T>` pattern preserved.     |
| III. Testing Standards            | PASS   | Unit tests for each migration step, version detection, backup creation, and downgrade rejection. `cargo clippy` and `pnpm check` must pass. |
| IV. User Experience Consistency   | PASS   | Downgrade error shown via toast notification. No new UI screens needed — migration is transparent.                                          |
| V. Performance Requirements       | PASS   | Migration runs during existing `load_library_index` call. No additional I/O beyond one backup file write.                                   |

No violations. All gates pass.

## Project Structure

### Documentation (this feature)

```text
specs/007-library-index-versioning/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src-tauri/src/
├── error.rs                    # Add SchemaVersionTooNew variant to ErrorCode
├── mods/
│   ├── mod.rs                  # LibraryIndex gains `version` field, load/save updated
│   └── schema_migration.rs     # NEW: migration framework + v0->v1 migration
└── ...

src/
├── lib/bindings.ts             # ts-rs auto-generated — ErrorCode gains SCHEMA_VERSION_TOO_NEW
├── utils/errors.ts             # No changes needed (generic hasErrorCode works)
└── ...
```

**Structure Decision**: Migrations live in a new `schema_migration.rs` submodule under `mods/`, keeping the migration logic close to the `LibraryIndex` struct it operates on. No new directories or modules beyond this single file.

## Complexity Tracking

No constitution violations to justify.
