# Tasks: Mod Library Folders

**Input**: Design documents from `/specs/001-mod-folders/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested — test tasks omitted. Manual verification checklist in quickstart.md.

**Organization**: Tasks grouped by user story. US1 and US2 are both P1 but US2 depends on US1's backend.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Data Model & Types)

**Purpose**: Define new Rust structs and TypeScript types that all stories depend on

- [x] T001 Add `LibraryFolder` struct with serde/TS derives and `ROOT_FOLDER_ID` constant in `src-tauri/src/mods/mod.rs`
- [x] T002 Extend `LibraryIndex` with `folders: Vec<LibraryFolder>` and `folder_order: Vec<String>` fields (both `#[serde(default)]`) in `src-tauri/src/mods/mod.rs`
- [x] T003 Add `folder_id: Option<String>` field to `InstalledMod` struct in `src-tauri/src/mods/mod.rs` (folder name resolved at runtime from folders query)
- [x] T004 Add backward-compat migration in `load_library_index()`: if no root folder exists, create one containing all existing mods; populate `folder_order` if empty in `src-tauri/src/mods/mod.rs`
- [x] T005 Add `LibraryFolder` TypeScript type and all new folder API bindings (`createFolder`, `renameFolder`, `deleteFolder`, `moveModToFolder`, `toggleFolder`, `reorderFolderMods`, `reorderFolders`, `getFolders`, `getFolderOrder`) in `src/lib/tauri.ts`

**Checkpoint**: Data model defined, types available on both sides. `cargo clippy` and `pnpm typecheck` should pass (commands not yet wired).

---

## Phase 2: Foundational (Backend Business Logic & Commands)

**Purpose**: Core backend operations that MUST be complete before any frontend story can be implemented

**⚠️ CRITICAL**: No user story frontend work can begin until this phase is complete

- [x] T006 Create `src-tauri/src/mods/folders.rs` with `flatten_folder_order()` helper that derives flat `mod_order` from `folder_order` + folder contents, and `sync_folders()` helper that reconciles folders with current mods (removes orphaned entries, appends new mods to root folder)
- [x] T007 [P] Implement `create_folder()` method on `ModLibrary` in `src-tauri/src/mods/folders.rs` — validate name non-empty, generate UUID, add folder to `folders` and `folder_order`, persist via `mutate_index`
- [x] T008 [P] Implement `rename_folder()` method on `ModLibrary` in `src-tauri/src/mods/folders.rs` — find folder by ID, validate new name, update name field
- [x] T009 [P] Implement `delete_folder()` method on `ModLibrary` in `src-tauri/src/mods/folders.rs` — move contained mods to root folder, remove folder from `folders` and `folder_order`, re-derive profile mod_orders. Root folder protected from deletion.
- [x] T010 Implement `move_mod_to_folder()` method on `ModLibrary` in `src-tauri/src/mods/folders.rs` — remove mod from source folder's `mod_ids`, append to target folder's `mod_ids`, re-derive profile mod_orders (moving to root = removing from user folder)
- [x] T012 Implement `toggle_folder()` method on `ModLibrary` in `src-tauri/src/mods/folders.rs` — iterate folder's `mod_ids`, add/remove each from active profile's `enabled_mods`, invalidate overlay
- [x] T013 [P] Implement `reorder_folder_mods()` method on `ModLibrary` in `src-tauri/src/mods/folders.rs` — validate mod IDs match folder contents (sorted set comparison), replace `mod_ids`, re-derive profile mod_orders, invalidate overlay
- [x] T014 [P] Implement `reorder_folders()` method on `ModLibrary` in `src-tauri/src/mods/folders.rs` — validate folder IDs match current `folder_order` set, replace `folder_order`, re-derive profile mod_orders, invalidate overlay
- [x] T015 [P] Implement `get_folders()` and `get_folder_order()` read-only methods on `ModLibrary` in `src-tauri/src/mods/folders.rs`
- [x] T016 Update `get_installed_mods()` in `src-tauri/src/mods/library.rs` to populate `folder_id` on each `InstalledMod` by building a mod→folder ID lookup map from all folders
- [x] T017 Update `sync_profile_mod_orders()` in `src-tauri/src/mods/mod.rs` to call `sync_folders()` and derive `profile.mod_order` from `flatten_folder_order()` instead of only reconciling the flat list
- [x] T018 Create command wrappers in `src-tauri/src/commands/folders.rs` for all folder operations: `create_folder`, `rename_folder`, `delete_folder`, `move_mod_to_folder`, `toggle_folder`, `reorder_folder_mods`, `reorder_folders`, `get_folders`, `get_folder_order` — each returns `IpcResult<T>` via `.into()`
- [x] T019 Export folder commands from `src-tauri/src/commands/mod.rs` and register all in `generate_handler![]` in `src-tauri/src/main.rs`
- [x] T020 Add folder query keys to `src/modules/library/api/keys.ts` (`folders`, `folderOrder`)

**Checkpoint**: All backend commands functional. Can verify via `pnpm tauri dev` + Rust logs. `cargo clippy` and `pnpm check` must pass.

---

## Phase 3: User Story 1 — Create a Folder and Add Mods (Priority: P1) 🎯 MVP

**Goal**: Users can create folders, move mods into them, and see grouped mods in both grid and list views with expand/collapse and navigation.

**Independent Test**: Create a folder, move mods into it, verify folder appears in library, expand/collapse in list view, navigate into it in grid view, rename and delete folder.

### Implementation for User Story 1

- [x] T021 [P] [US1] Create `useLibraryViewStore` Zustand store in `src/stores/libraryView.ts` with `expandedFolders: Set<string>` (persisted to localStorage key `ltk-library-view`), `currentFolderId: string | null` (not persisted), and actions `toggleFolderExpanded`, `setCurrentFolderId`, `clearCurrentFolder`
- [x] T022 [P] [US1] Create `useFolderMutations` hook in `src/modules/library/api/useFolderMutations.ts` — `useCreateFolder`, `useRenameFolder`, `useDeleteFolder` mutations with optimistic updates, invalidating `libraryKeys.folders()` and `libraryKeys.mods()` on settle
- [x] T023 [P] [US1] Create `useMoveMod` hook in `src/modules/library/api/useMoveMod.ts` — `useMoveModToFolder`, `useReorderFolderMods`, `useReorderFolders` mutations with optimistic updates (moving to root = removing from user folder)
- [x] T024 [P] [US1] Create `useFolders` and `useFolderOrder` query hooks in `src/modules/library/api/queries.ts` using `api.getFolders` and `api.getFolderOrder`
- [x] T025 [US1] Create `CreateFolderDialog` component in `src/modules/library/components/CreateFolderDialog.tsx` — popover with text input for folder name, uses `useCreateFolder` mutation, validates non-empty name
- [x] T026 [US1] Create `FolderContextMenu` component in `src/modules/library/components/FolderContextMenu.tsx` — uses `ContextMenu` compound component from `@/components` (right-click only) with items: Rename (inline input), Enable All, Disable All, Delete
- [x] T027 [US1] Create `FolderRow` component in `src/modules/library/components/FolderRow.tsx` — list view expandable row showing folder name, mod count, expand/collapse chevron icon, uses `useLibraryViewStore.expandedFolders`. When expanded, renders contained mods as indented `ModCard` items. Includes `FolderContextMenu`.
- [x] T028 [US1] Create `FolderCard` component in `src/modules/library/components/FolderCard.tsx` — grid view card showing folder name, mod count, 2x2 thumbnail grid of first 4 contained mods (using `useModThumbnail`). On click, sets `currentFolderId` in `useLibraryViewStore`. Includes `FolderContextMenu` on right-click.
- [x] T029 [US1] Create `FolderHeader` component in `src/modules/library/components/FolderHeader.tsx` — header bar shown when navigated into a folder in grid view. Shows back arrow button, folder name, mod count. Clicking back clears `currentFolderId`.
- [x] T030 [US1] Update `LibraryContent` in `src/modules/library/components/LibraryContent.tsx` to render folders (FolderRow/FolderCard) alongside root mods, with grid folder navigation and flat search/filter mode.
- [x] T031 [US1] Add "New Folder" button to `LibraryToolbar` in `src/modules/library/components/LibraryToolbar.tsx` — `CreateFolderDialog` popover
- [x] T032 [US1] Add right-click context menu on empty library space via `LibraryContextMenu` component wrapping `LibraryContent` — shows "New Folder" with inline name input
- [x] T033 [US1] Implement flat search/filter mode in `LibraryContent` — when `searchQuery` is non-empty or filters are active, render all mods flat without folder structure
- [x] T034 [US1] Export all new components and hooks through barrel exports in `src/modules/library/api/index.ts`, `src/modules/library/components/index.ts`, and `src/modules/library/index.ts`

**Checkpoint**: Folder CRUD fully functional in UI. Users can create, rename, delete folders, move mods via context menu, see folders in grid/list views, navigate into folders in grid, expand/collapse in list.

---

## Phase 4: User Story 2 — Enable/Disable All Mods in a Folder (Priority: P1)

**Goal**: Users can batch-toggle all mods in a folder with a single action. Tri-state indicator shows folder enabled status.

**Independent Test**: Create a folder with mods, toggle folder on/off, verify all mods change state. Check mixed state indicator.

### Implementation for User Story 2

- [x] T035 [P] [US2] Create `useToggleFolder` mutation hook in `src/modules/library/api/useFolderMutations.ts` — calls `api.toggleFolder`, invalidates mods on settle
- [x] T036 [US2] Add tri-state enabled indicator to `FolderRow` in `src/modules/library/components/FolderRow.tsx` — Checkbox with indeterminate support, toggle calls `useToggleFolder`, disabled when patcher active
- [x] T037 [US2] Add tri-state toggle to `FolderHeader` in `src/modules/library/components/FolderHeader.tsx` — Checkbox with indeterminate, visible when navigated inside a folder in grid view
- [x] T038 [US2] Add "Enable All" / "Disable All" items to `FolderContextMenu` in `src/modules/library/components/FolderContextMenu.tsx` — calls `useToggleFolder`, disabled when patcher active
- [x] T039 [US2] Show toast notification on successful folder toggle (e.g., "Enabled 5 mods in Ranked Skins") via `useToast()` in the toggle folder mutation's `onSuccess` callback

**Checkpoint**: Batch toggle works from list row, grid header, and context menu. Tri-state indicator accurate. Patcher guard enforced.

---

## Phase 5: User Story 3 — Drag and Drop Mod Organization (Priority: P2)

**Goal**: Users can drag mods into/out of folders and reorder mods within folders and at the top level, including reordering folders themselves.

**Independent Test**: Drag a mod onto a folder card/row, verify it moves into the folder. Drag a mod out, verify it returns to top level. Reorder within folder.

### Implementation for User Story 3

- [x] T040 [US3] Mutation hooks for folder reorder and move already exist in `src/modules/library/api/useMoveMod.ts` — `useReorderFolderMods`, `useReorderFolders`, `useMoveModToFolder`
- [x] T041 [US3] Created `DroppableFolderCard` and `DroppableFolderRow` components using `@dnd-kit/core` `useDroppable` — folders highlight with ring-2 ring-accent-500 when a mod is dragged over them
- [x] T042 [US3] Implemented unified `UnifiedDndGrid` in `LibraryContent.tsx` — folders and root mods in same grid within one `DndContext`. `onDragEnd` detects drop on `folder:*` ID prefix and calls `useMoveModToFolder`. Otherwise standard reorder.
- [x] T043 [US3] Folders and mods are in the same grid. Folder reordering not yet sortable (folders are drop targets only, not sortable items) — deferred to polish.
- [x] T044 [US3] Within-folder mod reordering works via `SortableModList` when navigated into a folder in grid view
- [x] T045 [US3] DnD disabled when search/filter active or sort != priority (handled in `dndDisabled` flag)
- [x] T045b [US3] Added "Remove from folder" menu item to `ModCard` context menu — moves mod back to root folder via `useMoveModToFolder`

**Checkpoint**: Full DnD support — move mods into/out of folders, reorder at all levels. DnD disabled when filtering/searching.

---

## Phase 6: User Story 4 — Folder State Persists Across Profiles (Priority: P3)

**Goal**: Folder structure remains stable when switching profiles. Only mod enabled/disabled states change per profile.

**Independent Test**: Create folders with mods, switch profiles, verify folders stay but enabled states reflect the new profile.

### Implementation for User Story 4

- [x] T046 [US4] Verify and fix `sync_profile_mod_orders()` in `src-tauri/src/mods/mod.rs` — verified: `flatten_folder_order()` correctly derives each profile's `mod_order` from the shared `folder_order` + folder contents without altering folder structure
- [x] T047 [US4] Reset `currentFolderId` to `null` in `useLibraryViewStore` when profile switches — added `clearCurrentFolder()` call in `useSwitchProfile` mutation's `onSuccess`
- [x] T048 [US4] Verified: `useSwitchProfile` invalidates `libraryKeys.mods()` which triggers refetch of `get_installed_mods()` — folder_id populated from library-level folders (unchanged), enabled state from new profile's enabled_mods

**Checkpoint**: Profile switching preserves folder structure, updates enabled states. Grid view resets to top level on switch.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, backward compatibility, and final quality pass

- [x] T049 Verify backward compatibility: load a `library.json` without `folders` or `folder_order` fields and confirm migration creates root folder with all existing mods — test in `src-tauri/src/mods/mod.rs` migration logic
- [x] T050 [P] Handle mod uninstall edge case: update `uninstall_mod_by_id()` in `src-tauri/src/mods/library.rs` to remove the mod from its containing folder's `mod_ids`
- [x] T051 [P] Handle mod install edge case: update `install_single_mod_to_index()` in `src-tauri/src/mods/library.rs` to append new mod to root folder's `mod_ids`
- [x] T052 [P] Ensure `useLibraryViewStore` cleans up stale folder IDs — if a folder in `expandedFolders` no longer exists (was deleted), silently remove it on next render
- [x] T053 Run `pnpm check` and `cargo clippy -p ltk-manager` and fix any errors/warnings
- [x] T054 Run `cargo fmt -p ltk-manager` and verify no formatting diffs
- [ ] T055 Manual end-to-end verification following the testing checklist in `specs/001-mod-folders/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all frontend work
- **US1 (Phase 3)**: Depends on Phase 2 completion — first user-facing functionality
- **US2 (Phase 4)**: Depends on Phase 3 (needs folder UI to exist for toggle placement)
- **US3 (Phase 5)**: Depends on Phase 3 (needs folder UI to exist as DnD targets)
- **US4 (Phase 6)**: Depends on Phase 2 (backend only) — can run in parallel with US1/US2/US3 frontend work
- **Polish (Phase 7)**: Depends on all stories being complete

### User Story Dependencies

- **US1 (P1)**: Foundational → US1. No other story dependencies. **This is the MVP.**
- **US2 (P1)**: US1 → US2. Needs folder UI components to add toggle controls.
- **US3 (P2)**: US1 → US3. Needs folder UI components as DnD targets.
- **US4 (P3)**: Foundational only. Backend verification + minor frontend hook. Can partially overlap with US1.

### Within Each User Story

- Hooks/stores before components (data layer first)
- Simpler components before complex ones (FolderRow before SortableModList integration)
- Core functionality before context menus and polish

### Parallel Opportunities

- **Phase 1**: T001-T003 sequential (same file), T005 parallel with T001-T004
- **Phase 2**: T007, T008, T009, T013, T014, T015 all parallel (independent methods). T010-T012 sequential (depend on T006 helpers). T018 after all methods. T016-T017 parallel with command methods.
- **Phase 3**: T021, T022, T023, T024 all parallel (different files). T025-T029 parallel (independent components). T030-T033 sequential (integrate into existing components).
- **Phase 4**: T035 parallel with T036-T038 prep. T036-T038 parallel (different components).
- **Phase 5**: T041-T045 mostly sequential (same DnD integration file).
- **Phase 7**: T049, T050, T051, T052 all parallel (different files/concerns).

---

## Parallel Example: Phase 3 (US1)

```
# Launch all hooks/stores in parallel:
T021: Create useLibraryViewStore in src/stores/libraryView.ts
T022: Create useFolderMutations in src/modules/library/api/useFolderMutations.ts
T023: Create useMoveMod in src/modules/library/api/useMoveMod.ts
T024: Create useFolders/useItemOrder queries in src/modules/library/api/queries.ts

# Then launch all new components in parallel:
T025: CreateFolderDialog in src/modules/library/components/CreateFolderDialog.tsx
T026: FolderContextMenu in src/modules/library/components/FolderContextMenu.tsx
T027: FolderRow in src/modules/library/components/FolderRow.tsx
T028: FolderCard in src/modules/library/components/FolderCard.tsx
T029: FolderHeader in src/modules/library/components/FolderHeader.tsx
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (data model + types)
2. Complete Phase 2: Foundational (all backend commands)
3. Complete Phase 3: User Story 1 (folder CRUD + UI)
4. **STOP and VALIDATE**: Create folders, move mods, verify grid/list views
5. Ship as initial release if desired

### Incremental Delivery

1. Phase 1 + Phase 2 → Backend complete
2. Add US1 → Folder creation and organization (MVP!)
3. Add US2 → Batch toggle (core value prop)
4. Add US3 → DnD polish
5. Add US4 → Profile persistence verification
6. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 and US2 are both P1 priority, but US2 depends on US1's UI components
- US3 (DnD) and US4 (profiles) are independent of each other
- The `reorder_mods` command may need deprecation or adaptation since `reorder_folders` supersedes it for top-level ordering
- All folder operations except `toggle_folder`, `reorder_folder_mods`, and `reorder_folders` are allowed while patcher is running
- Root folder model: all mods always belong to a folder. Ungrouped mods live in root folder (sentinel ID `"root"`). No separate `moveModToTopLevel` — use `moveModToFolder` with root ID.
