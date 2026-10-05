# Tasks: Mod Layer Selection

**Input**: Design documents from `/specs/004-mod-layer-selection/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/tauri-commands.md, quickstart.md

**Tests**: Not explicitly requested — test tasks omitted. Manual verification per constitution.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Foundational — Backend Data Model & Overlay Extension

**Purpose**: Core backend changes that MUST be complete before any user story can be implemented. Extends the Profile struct with layer state storage and the overlay builder with layer filtering.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T001 Add `layer_states: HashMap<String, HashMap<String, bool>>` field with `#[serde(default)]` to `Profile` struct in `src-tauri/src/mods/mod.rs`
- [x] T002 Update `read_installed_mod()` in `src-tauri/src/mods/library.rs` to accept the active profile's `layer_states` and derive `ModLayer.enabled` from them instead of hardcoding `true` (missing entries default to `true`)
- [x] T003 Update `get_installed_mods()` in `src-tauri/src/mods/library.rs` to pass the active profile's `layer_states` to `read_installed_mod()`
- [x] T004 Add `enabled_layers: Option<HashSet<String>>` field to `EnabledMod` struct in `league-mod/crates/ltk_overlay/src/builder/mod.rs`
- [x] T005 Filter layers in `collect_single_mod_metadata()` in `league-mod/crates/ltk_overlay/src/builder/metadata.rs` — skip layers not in `enabled_layers` when `Some`, process all when `None`
- [x] T006 Update `get_enabled_mods_for_overlay()` in `src-tauri/src/mods/library.rs` to read the active profile's `layer_states` and pass `enabled_layers` on each `EnabledMod` (convert `HashMap<String, bool>` to `HashSet<String>` of enabled layer names; pass `None` if no layer states exist for a mod)

**Checkpoint**: Profile persists layer states, overlay builder respects them. Existing behavior unchanged (all layers enabled by default).

---

## Phase 2: User Story 1 — View and Toggle Mod Layers (Priority: P1) 🎯 MVP

**Goal**: Users can see all layers for a multi-layer mod and toggle individual layers on or off. Single-layer mods show no layer UI.

**Independent Test**: Install a multi-layer `.modpkg`, open its details, toggle a layer off, run the patcher, verify disabled layer content is not applied.

### Backend

- [x] T007 [US1] Implement `set_mod_layers()` business logic in `src-tauri/src/mods/library.rs` — validate mod exists, update `Profile.layer_states[mod_id]` in active profile, save index, invalidate overlay
- [x] T008 [US1] Add `set_mod_layers` command wrapper in `src-tauri/src/commands/mods.rs` per contract signature (accepts `mod_id: String, layer_states: HashMap<String, bool>`, returns `IpcResult<()>`)
- [x] T009 [US1] Export `set_mod_layers` in `src-tauri/src/commands/mod.rs` and register in `generate_handler![]` in `src-tauri/src/main.rs`

### Frontend — Bindings & Hooks

- [x] T010 [US1] Add `api.setModLayers` TypeScript binding in `src/lib/tauri.ts` — `setModLayers: (modId: string, layerStates: Record<string, boolean>) => invokeResult<void>("set_mod_layers", { modId, layerStates })`
- [x] T011 [US1] Create `useSetModLayers` mutation hook in `src/modules/library/api/useSetModLayers.ts` with optimistic updates on `InstalledMod.layers[].enabled` (follow `useToggleMod` pattern: cancel queries, snapshot, optimistic update, rollback on error, invalidate on settled)
- [x] T012 [US1] Export `useSetModLayers` from `src/modules/library/api/index.ts` and `src/modules/library/index.ts`

### Frontend — UI Components

- [x] T013 [US1] Create `LayerToggleList` component in `src/modules/library/components/LayerToggleList.tsx` — renders a list of `ModLayer` items sorted by priority (lowest first), each with a `Switch` toggle. Shows layer name and description (if available). Accepts `layers`, `onToggle`, and `disabled` props. Scrollable with max height for 10+ layers.
- [x] T014 [US1] Create `LayerPopover` component in `src/modules/library/components/LayerPopover.tsx` — wraps `LayerToggleList` in a `Popover` from `@/components`. Trigger is a layers icon button. Calls `useSetModLayers` on toggle. Only rendered for mods with >1 layer.
- [x] T015 [US1] Update `ModDetailsDialog` in `src/modules/library/components/ModDetailsDialog.tsx` — replace the read-only layer display (lines 161-185) with interactive `LayerToggleList` using `Switch` toggles. Wire `useSetModLayers` to handle toggle events. Hide layers section entirely for single-layer mods.

**Checkpoint**: Multi-layer mods show interactive layer toggles in details dialog and via popover. Toggling a layer persists to profile, invalidates overlay. Single-layer mods unchanged.

---

## Phase 3: User Story 2 — Layer Picker on Mod Enable (Priority: P2)

**Goal**: When enabling a multi-layer mod, a layer picker popover appears allowing users to choose which layers to activate before the mod is enabled. Single-layer mods enable immediately with no picker.

**Independent Test**: Disable a multi-layer mod, toggle it on, verify picker appears, deselect some layers, confirm, verify only selected layers are active.

### Backend

- [x] T016 [US2] Implement `enable_mod_with_layers()` business logic in `src-tauri/src/mods/library.rs` — atomically add mod to `enabled_mods` (same insertion logic as `toggle_mod_enabled`) AND set `layer_states[mod_id]` in a single `mutate_index` call
- [x] T017 [US2] Add `enable_mod_with_layers` command wrapper in `src-tauri/src/commands/mods.rs` per contract signature (accepts `mod_id: String, layer_states: HashMap<String, bool>`, returns `IpcResult<()>`)
- [x] T018 [US2] Export `enable_mod_with_layers` in `src-tauri/src/commands/mod.rs` and register in `generate_handler![]` in `src-tauri/src/main.rs`

### Frontend — Bindings & Hooks

- [x] T019 [US2] Add `api.enableModWithLayers` TypeScript binding in `src/lib/tauri.ts` — `enableModWithLayers: (modId: string, layerStates: Record<string, boolean>) => invokeResult<void>("enable_mod_with_layers", { modId, layerStates })`
- [x] T020 [US2] Create `useEnableModWithLayers` mutation hook in `src/modules/library/api/useEnableModWithLayers.ts` with optimistic updates on `mod.enabled` + `mod.layers[].enabled`
- [x] T021 [US2] Export `useEnableModWithLayers` from `src/modules/library/api/index.ts` and `src/modules/library/index.ts`

### Frontend — UI Components

- [x] T022 [US2] Create `LayerPickerPopover` component in `src/modules/library/components/LayerPickerPopover.tsx` — a `Popover` containing `LayerToggleList` with all layers defaulting to enabled, plus Confirm and Cancel buttons. On confirm: calls `useEnableModWithLayers` with selected states. On cancel/dismiss: reverts the mod toggle (mod stays disabled).
- [x] T023 [US2] Update `ModCard` in `src/modules/library/components/ModCard.tsx` and `useLibraryActions` in `src/modules/library/api/useLibraryActions.ts` — intercept the enable toggle for multi-layer mods (layers.length > 1): instead of calling `toggleMod` directly, show the `LayerPickerPopover`. Single-layer mods continue to use `toggleMod` as before.

**Checkpoint**: Enabling a multi-layer mod shows layer picker. Confirm enables with selected layers. Cancel keeps mod disabled. Single-layer mods unaffected.

---

## Phase 4: User Story 3 — Layer State Persistence Per Profile (Priority: P2)

**Goal**: Each profile independently maintains its own layer configuration. Switching profiles reflects that profile's layer states. Layer states survive profile round-trips.

**Independent Test**: Set different layer states for the same mod in two profiles, switch between them, verify each shows its own configuration.

**Note**: The foundational data model (T001-T003) already stores layer states per profile. This phase focuses on ensuring correctness during profile operations.

- [x] T024 [US3] Verify and update `switch_profile()` in `src-tauri/src/mods/profiles.rs` — ensure switching profiles triggers frontend cache invalidation so `get_installed_mods` re-derives layer states from the new active profile's `layer_states`
- [x] T025 [US3] Verify and update `create_profile()` in `src-tauri/src/mods/profiles.rs` — new profiles start with empty `layer_states` (all layers default to enabled)
- [x] T026 [US3] Verify and update `delete_profile()` in `src-tauri/src/mods/profiles.rs` — deleting a profile removes its `layer_states` along with all other profile data (no orphaned state)
- [x] T027 [US3] Verify and update `duplicate_profile()` in `src-tauri/src/mods/profiles.rs` (if it exists) — duplicated profile copies `layer_states` from the source profile

**Checkpoint**: Layer states are fully profile-scoped. Switching, creating, deleting, and duplicating profiles all handle layer states correctly.

---

## Phase 5: User Story 4 — Multi-Layer Mod Visual Indicator (Priority: P3)

**Goal**: Mod cards in the library display a visual indicator (layer count badge) for mods with multiple layers, helping users identify configurable mods at a glance.

**Independent Test**: View a library with both single-layer and multi-layer mods; verify multi-layer mods show a badge and single-layer mods do not.

- [x] T028 [P] [US4] Update `ModCard` grid view in `src/modules/library/components/ModCard.tsx` — add a layer count badge (e.g., "3 layers" or "2/4 layers" when some disabled) for mods with `layers.length > 1`. Position near the thumbnail or content area. Use `surface-*` and `brand-*` color tokens. Do not show for single-layer mods.
- [x] T029 [P] [US4] Update `ModCard` list view in `src/modules/library/components/ModCard.tsx` — add the same layer count indicator inline in the info row for mods with `layers.length > 1`.

**Checkpoint**: Multi-layer mods are visually distinguishable in both grid and list views. Single-layer mods unchanged.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Edge case handling, cleanup, and verification across all user stories.

- [x] T030 Implement layer state cleanup on mod re-install in `src-tauri/src/mods/library.rs` — in `install_single_mod_to_index()`, after extracting new metadata, reconcile `layer_states` across all profiles: remove entries for layers no longer in the mod, preserve entries for layers that still exist, let new layers default to enabled via absence
- [x] T031 Add layer state cleanup to `reconcile_library_index()` in `src-tauri/src/mods/mod.rs` — when removing orphaned mod entries, also clean up their `layer_states` entries from all profiles
- [x] T032 Clean up `layer_states` on mod uninstall in `src-tauri/src/mods/library.rs` — when `uninstall_mod()` removes a mod, also remove its entry from `layer_states` in all profiles
- [x] T033 Run `cargo clippy -p ltk-manager` and `cargo fmt -p ltk-manager` to verify zero warnings and no formatting diffs
- [x] T034 Run `pnpm check` (typecheck + lint + format:check) to verify zero errors
- [x] T035 Regenerate TypeScript bindings by running `cargo test -p ltk-manager` (ts-rs export) and verify `src/lib/bindings/` files are up to date (Profile type may need re-export if it gains the `layer_states` field in TS)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundational)**: No dependencies — start immediately
- **Phase 2 (US1 — View & Toggle)**: Depends on Phase 1 completion
- **Phase 3 (US2 — Layer Picker on Enable)**: Depends on Phase 1 completion. Can run in parallel with US1 (different files) but US1 provides the `LayerToggleList` component reused by US2's `LayerPickerPopover`
- **Phase 4 (US3 — Profile Persistence)**: Depends on Phase 1 completion. Can run in parallel with US1/US2 (different files — profile operations)
- **Phase 5 (US4 — Visual Indicator)**: Depends on Phase 1 completion. Can run in parallel with all other user stories (only touches ModCard display, no backend dependency beyond foundational)
- **Phase 6 (Polish)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 1 only. **This is the MVP.**
- **US2 (P2)**: Depends on Phase 1. Reuses `LayerToggleList` from US1 (T013), so ideally US1 completes first.
- **US3 (P2)**: Depends on Phase 1 only. Independent of US1/US2.
- **US4 (P3)**: Depends on Phase 1 only. Independent of all other stories.

### Within Each User Story

- Backend tasks before frontend bindings
- Frontend bindings before hooks
- Hooks before UI components
- Command registration (main.rs) after command wrapper

### Parallel Opportunities

Within Phase 1:

- T004 + T005 (ltk_overlay changes) can run in parallel with T001 (Profile struct change)

Within Phase 2 (US1):

- T010 + T011 (frontend bindings/hooks) can start after T009 (backend complete)
- T013 + T014 (LayerToggleList + LayerPopover) can run in parallel
- T015 (ModDetailsDialog) depends on T013 (LayerToggleList)

Within Phase 3 (US2):

- T019 + T020 (frontend bindings/hooks) can start after T018 (backend complete)
- T022 (LayerPickerPopover) depends on T013 (LayerToggleList from US1)

Within Phase 5 (US4):

- T028 + T029 (grid + list badge) can run in parallel

---

## Parallel Example: Phase 1 (Foundational)

```
# These can run in parallel (different repos/files):
Task T001: Add layer_states to Profile in src-tauri/src/mods/mod.rs
Task T004: Add enabled_layers to EnabledMod in league-mod/crates/ltk_overlay/src/builder/mod.rs
Task T005: Filter layers in league-mod/crates/ltk_overlay/src/builder/metadata.rs

# These depend on T001:
Task T002: Update read_installed_mod in src-tauri/src/mods/library.rs
Task T003: Update get_installed_mods in src-tauri/src/mods/library.rs

# This depends on T001 + T004:
Task T006: Update get_enabled_mods_for_overlay in src-tauri/src/mods/library.rs
```

## Parallel Example: Phase 2 (US1 — View & Toggle)

```
# Backend (sequential):
Task T007 → T008 → T009

# Frontend bindings (after backend):
Task T010 + T011 (parallel, different files)

# UI components (after hooks):
Task T013 + T014 (parallel, different files)
Task T015 (depends on T013)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Foundational (T001-T006)
2. Complete Phase 2: User Story 1 (T007-T015)
3. **STOP and VALIDATE**: Install a multi-layer modpkg, verify layers display, toggle a layer, run patcher, verify disabled layer excluded
4. Deploy/demo if ready — core layer selection is functional

### Incremental Delivery

1. Phase 1 (Foundational) → Backend ready
2. Phase 2 (US1: View & Toggle) → MVP! Users can see and control layers
3. Phase 3 (US2: Layer Picker on Enable) → Smoother first-time experience
4. Phase 4 (US3: Profile Persistence) → Verification of profile integration
5. Phase 5 (US4: Visual Indicator) → Polish for discoverability
6. Phase 6 (Polish) → Edge cases, cleanup, final checks

### Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Commit after each task or logical group
- Stop at any checkpoint to validate the story independently
- The `league-mod` workspace changes (T004, T005) must be committed and the dependency updated in `src-tauri/Cargo.toml` if using a path dependency
