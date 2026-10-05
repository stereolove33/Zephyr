# Tasks: Library Index Schema Versioning

**Input**: Design documents from `/specs/007-library-index-versioning/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Add the new error code and create the migration module file

- [x] T001 [P] Add `SchemaVersionTooNew` variant to `ErrorCode` enum and `AppError` enum in `src-tauri/src/error.rs`, with `AppErrorResponse` conversion that includes `{ "fileVersion", "maxSupported" }` context
- [x] T002 [P] Create `src-tauri/src/mods/schema_migration.rs` with module declaration, `CURRENT_VERSION` constant (set to `1`), and the `migrate_index` function signature that takes raw JSON string and storage dir path, returns `AppResult<LibraryIndex>`
- [x] T003 Register `schema_migration` submodule in `src-tauri/src/mods/mod.rs` (`mod schema_migration;`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the `version` field to `LibraryIndex` and update serialization

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Add `#[serde(default)] pub(crate) version: u32` field to `LibraryIndex` struct in `src-tauri/src/mods/mod.rs` and set `version: CURRENT_VERSION` in `LibraryIndex::default()`
- [x] T005 Regenerate TypeScript bindings by running `cargo test` (ts-rs export) — verify `SCHEMA_VERSION_TOO_NEW` appears in `src/lib/bindings.ts`

**Checkpoint**: `LibraryIndex` now serializes/deserializes with a `version` field. Existing files without the field deserialize with `version: 0`.

---

## Phase 3: User Story 1 — Seamless Upgrade Across App Versions (Priority: P1) MVP

**Goal**: Older library.json files are automatically migrated to the current schema version on load, preserving all data.

**Independent Test**: Write a v0 index (no version field, no folders), load it, verify all mods/profiles/folders/ordering preserved and version is now 1.

### Implementation for User Story 1

- [x] T006 [US1] Implement `extract_version` function in `src-tauri/src/mods/schema_migration.rs` — parses raw JSON string into `serde_json::Value`, reads the `version` field (defaulting to `0` if absent), returns `(u32, Value)`
- [x] T007 [US1] Implement `v0_to_v1` migration function in `src-tauri/src/mods/schema_migration.rs` — takes `serde_json::Value`, ensures root folder exists in `folders` array, populates `folderOrder`, sets `version` to `1`, returns transformed `Value`
- [x] T008 [US1] Implement `LibraryIndex::load_and_migrate` associated function in `src-tauri/src/mods/schema_migration.rs` — chains migrations sequentially (v0→v1, future v1→v2, etc.) based on extracted version, then deserializes the final `Value` into `LibraryIndex`
- [x] T009 [US1] Refactor `load_library_index` in `src-tauri/src/mods/mod.rs` — replace the inline ad-hoc folder migration code with a call to `LibraryIndex::load_and_migrate`, removing the duplicated root-folder and `folderOrder` logic
- [x] T010 [US1] Update `save_library_index` in `src-tauri/src/mods/mod.rs` — ensure the `version` field is always set to `CURRENT_VERSION` before writing (defensive, in case struct was constructed without it)
- [x] T011 [US1] Add unit tests in `src-tauri/src/mods/schema_migration.rs` — test `v0_to_v1` with: (a) legacy JSON without folders/folderOrder, (b) legacy JSON with folders but no version field, (c) current-version JSON (no migration runs). Verify data integrity in all cases.

**Checkpoint**: User Story 1 complete — existing users upgrade seamlessly. `cargo test` passes.

---

## Phase 4: User Story 2 — Downgrade Protection (Priority: P2)

**Goal**: App rejects library.json files written by a newer version with a clear error instead of silently corrupting data.

**Independent Test**: Create a library.json with `"version": 99`, attempt to load — verify `SchemaVersionTooNew` error with correct context.

### Implementation for User Story 2

- [x] T012 [US2] Add version-too-new check in `LibraryIndex::load_and_migrate` in `src-tauri/src/mods/schema_migration.rs` — if extracted version > `CURRENT_VERSION`, return `AppError::SchemaVersionTooNew` with `{ fileVersion, maxSupported }` context
- [x] T013 [US2] Add unit test in `src-tauri/src/mods/schema_migration.rs` — load JSON with `"version": 99`, assert `SchemaVersionTooNew` error is returned, assert original file is untouched
- [x] T014 [US2] Handle `SCHEMA_VERSION_TOO_NEW` error in the frontend — show a dedicated error state in `LibraryErrorState` with amber warning icon and descriptive message via `hasErrorCode()` guard in `src/modules/library/components/LibraryStates.tsx`

**Checkpoint**: User Story 2 complete — downgrading users see a clear error, no data corruption.

---

## Phase 5: User Story 3 — Backup Before Migration (Priority: P3)

**Goal**: A backup file is created before any migration so users can recover manually if needed.

**Independent Test**: Trigger a v0→v1 migration, verify `library.v0.json.bak` exists in the storage directory with the original file contents.

### Implementation for User Story 3

- [x] T015 [US3] Implement `LibraryIndex::backup` method in `src-tauri/src/mods/schema_migration.rs` — copies `library.json` to `library.v{N}.json.bak` where N is the source version. Called by `load_and_migrate` before running any migration steps.
- [x] T016 [US3] Add unit test in `src-tauri/src/mods/schema_migration.rs` — trigger migration from v0, verify backup file exists with original content, verify migrated file has version 1

**Checkpoint**: User Story 3 complete — every migration creates a recoverable backup.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [x] T017 [P] Run `cargo clippy -p ltk-manager` and fix any warnings in changed files
- [x] T018 [P] Run `pnpm check` (typecheck + lint + format:check) and fix any errors
- [x] T019 Verify existing `load_library_index` tests in `src-tauri/src/mods/mod.rs` still pass — especially `load_library_index_migrates_legacy_without_folders` and `load_library_index_no_reconciliation_when_clean`
- [ ] T020 Manual smoke test: launch app with an existing library.json (no version field), verify mods load correctly and `library.json` now contains `"version": 1`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. T003 depends on T002.
- **Foundational (Phase 2)**: Depends on Phase 1. T005 depends on T004.
- **User Story 1 (Phase 3)**: Depends on Phase 2. T007 depends on T006. T008 depends on T007. T009 depends on T008. T010 and T011 can parallel after T009.
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs `migrate_index` function). T013 depends on T012. T014 can parallel with T013.
- **User Story 3 (Phase 5)**: Depends on Phase 3 (needs `migrate_index` function). Can run in parallel with Phase 4.
- **Polish (Phase 6)**: Depends on all user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Requires Foundational phase — no other story dependencies
- **User Story 2 (P2)**: Requires US1 (needs the migration framework to add the version check)
- **User Story 3 (P3)**: Requires US1 (needs the migration framework to add backup step). Can run in parallel with US2.

### Parallel Opportunities

- T001 and T002 can run in parallel (different files)
- T017 and T018 can run in parallel (different toolchains)
- US2 (Phase 4) and US3 (Phase 5) can run in parallel after US1 completes

---

## Parallel Example: User Story 1

```
# After Phase 2, these run sequentially within US1:
T006 → T007 → T008 → T009

# Then these can run in parallel:
T010 (update save_library_index)
T011 (add unit tests)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T005)
3. Complete Phase 3: User Story 1 (T006–T011)
4. **STOP and VALIDATE**: Run `cargo test`, verify migration works with real library.json
5. This alone delivers the core value — schema versioning with forward migration

### Incremental Delivery

1. Setup + Foundational → version field exists in schema
2. Add User Story 1 → migration framework works, existing users upgrade seamlessly (MVP!)
3. Add User Story 2 → downgrade protection prevents data corruption
4. Add User Story 3 → backup safety net for migrations
5. Each story adds safety without breaking previous functionality

---

## Notes

- All migration logic lives in a single new file: `src-tauri/src/mods/schema_migration.rs`
- The only existing file with significant changes is `src-tauri/src/mods/mod.rs` (struct field + load/save refactor)
- `error.rs` gets one new enum variant — minimal blast radius
- Frontend changes are minimal — only one toast handler for the downgrade error (T014)
- Commit after each phase checkpoint for clean git history
