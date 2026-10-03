# Research: Mod Library Folders

**Branch**: `001-mod-folders` | **Date**: 2026-03-26

## R1: Folder Data Persistence Strategy

**Decision**: Extend `LibraryIndex` with a top-level `folders: Vec<LibraryFolder>` field.

**Rationale**: Folders are a library-level concept (shared across profiles), which aligns with `LibraryIndex` being the single source of truth for library-wide data. Adding `folders` alongside `mods` and `profiles` keeps the data co-located and atomically persisted via the existing `mutate_index()` pattern. Using `#[serde(default)]` ensures backward compatibility — existing `library.json` files without the `folders` field will deserialize with an empty vec.

**Alternatives considered**:

- Separate `folders.json` file — rejected because it introduces a second persistence file requiring cross-file consistency, and the existing `index_lock` mutex wouldn't cover it without refactoring.
- Folders as a profile-level concept — rejected per spec: folders persist across profile switches.

## R2: Mod Ordering with Folders

**Decision**: Introduce a root folder model where all mods always belong to a folder. A special root folder (sentinel ID `"root"`) holds ungrouped mods. `LibraryIndex` gets two new fields: `folders: Vec<LibraryFolder>` (all folders including root) and `folder_order: Vec<String>` (ordered folder IDs defining top-level display order). Each folder's `mod_ids: Vec<String>` tracks internal mod ordering. `Profile.mod_order` is derived by flattening `folder_order` (expanding each folder's `mod_ids` in sequence). `Profile.enabled_mods` remains the subset of enabled mod IDs.

**Rationale**: The root folder model eliminates special-case handling for ungrouped mods — every mod operation is a folder operation. `folder_order` is a simple `Vec<String>` instead of a heterogeneous enum, simplifying serialization and DX. Keeping `Profile.mod_order` as the flattened view ensures the overlay builder continues to work unchanged.

**Alternatives considered**:

- `OrderItem` enum (`Mod(id)` / `Folder(id)`) for `item_order` — rejected during implementation because it required special-case handling for ungrouped mods throughout the codebase. Root folder model is simpler.
- Removing `Profile.mod_order` entirely — breaks backward compatibility and requires overlay builder changes.

## R3: Folder-Aware Mod Order Reconciliation

**Decision**: Extend `sync_profile_mod_orders()` to call `sync_folders()` which reconciles all folders with valid mods (removes orphaned mod IDs from folders, appends new mods to the root folder). When a new mod is installed, it is appended to the root folder's `mod_ids`. When a mod is uninstalled, it is removed from its containing folder's `mod_ids`. The flattened `Profile.mod_order` is re-derived via `flatten_folder_order()` after any structural change.

**Rationale**: The existing reconciliation already handles appending new mods and removing orphaned IDs. Extending it to also scan folder contents maintains the same invariants. The root folder model means all mods are always in a folder, simplifying the reconciliation logic.

**Alternatives considered**: None significant — this is the natural extension of the existing pattern.

## R4: Batch Toggle Implementation

**Decision**: Implement batch toggle as a single `mutate_index()` call that iterates all mod IDs in the folder and adds/removes them from `Profile.enabled_mods`. For mods with multiple layers and no existing layer state, default all layers to enabled (consistent with current single-mod toggle behavior).

**Rationale**: A single `mutate_index()` call ensures atomicity — either all mods toggle or none do. The overlay is invalidated once. This keeps the operation fast (single disk write) and consistent.

**Alternatives considered**:

- Calling `toggle_mod_enabled()` in a loop — rejected because each call would load/save/invalidate separately, causing N disk writes instead of 1.

## R5: DnD Strategy for Folders

**Decision**: Extend the existing `@dnd-kit` setup in `SortableModList` to support two interaction modes:

1. **Drop onto folder**: When a mod is dragged over a folder item (not between items), the folder becomes a drop target and the mod is moved into it.
2. **Reorder between items**: The existing reorder behavior applies to top-level items (mods and folders). Within a folder view (list: expanded; grid: navigated in), reordering applies to the folder's internal mod list.

Use `@dnd-kit`'s `onDragOver` to detect folder hover and provide visual feedback (folder highlights). Use `onDragEnd` to determine whether the drop was a reorder or a move-into-folder.

**Rationale**: `@dnd-kit` already supports custom collision detection and multiple droppable areas. The existing `SortableModList` handles reorder via `onDragEnd`; adding folder-as-droppable extends this naturally.

**Alternatives considered**:

- Using a separate DnD library for folder interactions — rejected to avoid adding dependencies and maintaining consistency.
- Context menu only (no DnD into folders) — rejected per spec; drag-and-drop is P2 but explicitly required.

## R6: Grid View Folder Navigation

**Decision**: Use a Zustand store (`libraryView.ts`) to track the currently "open" folder ID in grid view. When a folder card is clicked, the store is set to that folder's ID, and `LibraryContent` renders only that folder's mods with a `FolderHeader` component (showing folder name, back button, batch toggle). The back button (or breadcrumb) clears the store, returning to the top-level view. This is client-only state (not persisted).

**Rationale**: Grid folder navigation is transient per the spec (resets on app launch). A Zustand store is the correct choice for ephemeral UI state per the constitution's state management rules. No TanStack Router integration needed since this is in-page navigation, not a route change.

**Alternatives considered**:

- TanStack Router with query params (`?folder=id`) — over-engineering for in-page navigation; would add to browser history unnecessarily.
- React state in Library.tsx — acceptable but a Zustand store is cleaner for cross-component access (toolbar needs to know if inside a folder).

## R7: List View Expansion State Persistence

**Decision**: Store expanded folder IDs in the same Zustand store (`libraryView.ts`) with `persist` middleware (localStorage). On app launch, list view folders restore their expanded/collapsed state.

**Rationale**: Per spec, list view expansion state persists between sessions. Zustand with `persist` is the established pattern in this codebase (see `useDisplayStore`).

**Alternatives considered**:

- Backend persistence in `library.json` — rejected; this is pure UI state, not domain data.

## R8: Search/Filter Flat Mode

**Decision**: When `searchQuery` is non-empty or any filter is active, the library renders in "flat mode" — all mods (including those in folders) are shown as a flat list/grid. Each mod in a folder gets a small badge/label showing its parent folder name. Folders themselves are hidden from the view. The existing `useFilteredMods()` hook is extended to flatten folder contents into the result set.

**Rationale**: Per clarification, search results are flat. This is simpler to implement (no partial folder expansion) and more useful for finding specific mods.

**Alternatives considered**: None — this was a direct clarification from the user.

## R9: Folder Card Thumbnail Previews

**Decision**: The grid-view folder card shows up to 4 thumbnail previews of contained mods in a 2x2 grid layout. Thumbnails are loaded via the existing `useModThumbnail(modId)` hook. If fewer than 4 mods exist, empty slots show a placeholder. The card also displays the folder name and mod count.

**Rationale**: Per clarification, the folder card shows name + count + thumbnail previews. A 2x2 grid is a common pattern (similar to playlist covers) that gives visual identity to folders without being overly complex.

**Alternatives considered**:

- Single large thumbnail — doesn't convey folder contents at a glance.
- Scrolling thumbnail strip — too complex for a card element.

## R10: Backward Compatibility

**Decision**: Use `#[serde(default)]` on the new `folders` and `folder_order` fields in `LibraryIndex`. On first load of an existing `library.json`:

1. `folders` defaults to `Vec::new()` (empty).
2. `folder_order` defaults to `Vec::new()` (empty).
3. A migration step in `load_library_index()` detects the absence of a root folder and creates one containing all existing mods. `folder_order` is populated with `["root"]`.

**Rationale**: This ensures existing users see no change — all their mods appear in the root folder (ungrouped at the top level) in their existing order. The migration is idempotent and runs only once.

**Alternatives considered**:

- Versioned migration system — over-engineering for a single additive change.
