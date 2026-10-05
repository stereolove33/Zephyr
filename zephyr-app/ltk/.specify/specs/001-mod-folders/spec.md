# Feature Specification: Mod Library Folders

**Feature Branch**: `001-mod-folders`
**Created**: 2026-03-26
**Status**: Draft
**Input**: User description: "We need to implement a way in the mod library for users to group mods into folders that can be enabled. This would allow users to group their mods so they can be enabled much quicker and easier."

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Create a Folder and Add Mods (Priority: P1)

A user with many installed mods wants to organize them into logical groups. They create a folder (e.g., "Ranked Skins" or "ARAM Fun"), then move existing mods into it. The folder appears in the mod library alongside ungrouped mods, and mods inside it are visually nested under the folder.

**Why this priority**: Without the ability to create folders and assign mods to them, no other folder functionality is possible. This is the foundational capability.

**Independent Test**: Can be fully tested by creating a folder, dragging/moving mods into it, and verifying mods appear grouped under the folder in the library view.

**Acceptance Scenarios**:

1. **Given** a library with installed mods, **When** the user creates a new folder with a name, **Then** the folder appears in the mod list and is empty.
2. **Given** a folder exists, **When** the user moves one or more mods into the folder, **Then** those mods appear nested under the folder and are no longer shown at the top level.
3. **Given** a folder contains mods in list view, **When** the user expands/collapses the folder row, **Then** the contained mods are shown or hidden inline.
4. **Given** a folder in grid view, **When** the user clicks the folder card, **Then** the view navigates into the folder showing only its contained mods, with a way to navigate back to the top level.
5. **Given** a folder exists, **When** the user renames the folder, **Then** the new name is displayed and persisted.
6. **Given** a folder exists, **When** the user deletes the folder, **Then** the mods inside it are moved back to the top level (ungrouped) and the folder is removed.

---

### User Story 2 - Enable/Disable All Mods in a Folder (Priority: P1)

A user wants to quickly enable or disable an entire group of mods at once. They toggle the folder's enabled state, which enables or disables all mods contained within it. This is the core value proposition — batch toggling for faster workflow.

**Why this priority**: This is the primary motivation for the feature. Without batch enable/disable, folders are just visual grouping with limited value.

**Independent Test**: Can be tested by creating a folder with multiple mods, toggling the folder on/off, and verifying all contained mods change state accordingly.

**Acceptance Scenarios**:

1. **Given** a folder with 5 disabled mods, **When** the user enables the folder, **Then** all 5 mods become enabled.
2. **Given** a folder with 5 enabled mods, **When** the user disables the folder, **Then** all 5 mods become disabled.
3. **Given** a folder with a mix of enabled and disabled mods, **When** the user views the folder, **Then** the folder shows a mixed/indeterminate state indicator.
4. **Given** a folder in mixed state, **When** the user toggles the folder on, **Then** all mods become enabled.
5. **Given** a folder with all mods enabled, **When** the user individually disables one mod inside it, **Then** the folder state updates to mixed/indeterminate.

---

### User Story 3 - Drag and Drop Mod Organization (Priority: P2)

A user wants to reorganize their mods intuitively by dragging mods into and out of folders, and reordering mods within folders. The drag-and-drop interaction should feel natural and consistent with the existing mod reordering behavior.

**Why this priority**: Drag-and-drop is the most intuitive way to organize mods into folders, but the feature is still usable with context menu or button-based assignment as a fallback.

**Independent Test**: Can be tested by dragging a mod onto a folder and verifying it moves into that folder, and by dragging a mod out of a folder to the top level.

**Acceptance Scenarios**:

1. **Given** a mod at the top level and a folder, **When** the user drags the mod onto the folder, **Then** the mod is moved into the folder.
2. **Given** a mod inside a folder, **When** the user drags it out to the top level, **Then** the mod is removed from the folder and appears ungrouped.
3. **Given** multiple mods inside a folder, **When** the user reorders them via drag, **Then** the new order is persisted and reflects mod priority.
4. **Given** a folder, **When** the user drags it to a new position in the list, **Then** the folder (with all its mods) moves to the new position.

---

### User Story 4 - Folder State Persists Across Profiles (Priority: P3)

Folders are a library-level organizational concept — they persist regardless of which profile is active. However, the enabled/disabled state of mods within folders is profile-specific (as it is today). When switching profiles, the folder structure remains the same but mod enabled states reflect the new profile.

**Why this priority**: Important for consistency but not blocking the core folder workflow. Users expect their organizational structure to remain stable across profile switches.

**Independent Test**: Can be tested by creating folders, switching profiles, and verifying folders remain while mod enabled states change per profile.

**Acceptance Scenarios**:

1. **Given** folders with mods on Profile A, **When** the user switches to Profile B, **Then** the same folder structure is visible but mod enabled/disabled states reflect Profile B's configuration.
2. **Given** a folder exists, **When** a new profile is created, **Then** the folder structure is visible in the new profile.

---

### Edge Cases

- What happens when a mod is uninstalled that belongs to a folder? The mod is removed from the folder. If the folder becomes empty, it remains (user may want to add mods later).
- What happens when a mod is installed? It is added to the root folder, appearing at the top level until the user moves it into a user-created folder.
- What happens when folders are reordered alongside ungrouped mods? Folders and ungrouped mods share the same ordering space — a folder occupies a single position in the list.
- What happens when the user has no folders? The library behaves exactly as it does today — fully backward compatible.
- What happens when the patcher is running? Folder creation, renaming, deletion, and moving mods between folders are organizational actions and should be allowed while the patcher is running. However, enabling/disabling mods (including via folder toggle) is blocked while the patcher is running, consistent with current behavior.
- Can folders be nested inside other folders? No. Folders are single-level only to keep the experience simple and avoid complexity.
- In list view, folder expanded/collapsed state persists between app sessions. In grid view, navigating into a folder is a transient navigation action — the app always opens at the top-level folder view on launch.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST allow users to create named folders in the mod library via a toolbar button and a right-click context menu on empty library space.
- **FR-002**: System MUST allow users to rename and delete folders.
- **FR-003**: System MUST allow users to move mods into and out of folders.
- **FR-004**: System MUST allow users to enable or disable all mods in a folder with a single action (batch toggle). In list view, the toggle appears on the folder row. In grid view, the toggle is accessible via right-click context menu on the folder card and as a header control when navigated inside the folder.
- **FR-005**: System MUST display a tri-state indicator on folders: all enabled, all disabled, or mixed.
- **FR-006**: System MUST persist folder structure and mod-to-folder assignments at the library level (not per-profile).
- **FR-007**: System MUST maintain per-profile mod enabled/disabled state within folders (consistent with existing profile behavior).
- **FR-008**: System MUST support drag-and-drop for moving mods into/out of folders and reordering within folders.
- **FR-009**: In list view, system MUST support expanding and collapsing folder rows to show/hide contained mods inline. In grid view, system MUST allow navigating into a folder to view its contents, with a way to navigate back to the top level.
- **FR-010**: System MUST allow folders to be reordered alongside ungrouped mods in the priority list.
- **FR-011**: System MUST NOT allow nested folders (single level only).
- **FR-012**: System MUST preserve existing mods as ungrouped when no folders exist (full backward compatibility).
- **FR-013**: System MUST allow folder management (create, rename, delete, move mods) while the patcher is running, but MUST block batch enable/disable while the patcher is running.
- **FR-014**: When a folder is deleted, system MUST move contained mods to the root folder rather than uninstalling them.
- **FR-015**: System MUST allow individual mod toggling within a folder (overriding the folder-level state).

### Key Entities

- **Folder**: A named container for grouping mods. Has a unique identifier, a display name, and an ordered list of mod references. Exists at the library level (shared across profiles). A special root folder (sentinel ID `"root"`) holds ungrouped mods and cannot be renamed or deleted.
- **Mod-Folder Assignment**: Every mod always belongs to exactly one folder — there are no ungrouped mods at the data level. Mods not assigned to a user-created folder live in the root folder. Only the folder ID is stored on the mod; the folder name is resolved at runtime from the folders list.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Users can enable or disable all mods in a folder with a single click, reducing the number of interactions needed from N (one per mod) to 1.
- **SC-002**: Users can create a folder and organize mods into it in under 30 seconds.
- **SC-003**: Folder structure remains intact when switching between profiles, with no data loss or corruption.
- **SC-004**: The library is fully functional and visually unchanged for users who do not create any folders (zero regression).
- **SC-005**: Drag-and-drop for folder organization feels responsive, with visual feedback appearing within 100ms of initiating the drag.

## Clarifications

### Session 2026-03-26

- Q: How do folders behave differently in grid vs list view? → A: Grid view treats folders as navigable containers (click to enter); list view uses inline row expansion.
- Q: What information does a folder card show in grid view? → A: Folder name, mod count, and thumbnail previews of the first few contained mods.
- Q: Where does the batch enable/disable toggle appear in grid view? → A: Both via right-click context menu on the folder card and as a toggle in the folder header when navigated inside.
- Q: How do search/filter results interact with folders? → A: Results are flat — folders disappear, matching mods shown individually with a folder label/badge indicating their parent folder.
- Q: How does the user create a new folder? → A: Both a dedicated "New Folder" button in the library toolbar and a right-click context menu option on empty library space.
- Q: Should folder name be denormalized into the installed mod response? → A: No. Only `folderId` is stored on each mod. Folder name is resolved at runtime by looking up the folder ID from the folders query. This avoids redundant data and keeps the mod struct lean.
- Q: How should ungrouped mods be represented on the backend? → A: All mods always belong to a folder. Ungrouped mods live in an implicit root folder with sentinel ID `"root"`. The root folder cannot be renamed or deleted, and is hidden from the user-facing folder list. `item_order` becomes a simple `Vec<String>` of folder IDs (no more `OrderItem` enum). This unifies all mod-folder operations and eliminates special-case handling for ungrouped mods.

## Assumptions

- Folder names do not need to be unique — users may have duplicate-named folders if they wish, distinguished by position.
- Folders are purely an organizational UI concept and do not affect the overlay build process — the overlay still processes mods in their priority order regardless of folder grouping.
- The folder feature does not interact with the Workshop or mod installation flow — it is a post-install organizational tool.
- Empty folders are allowed and persist until explicitly deleted by the user.
- Filtering and searching mods operates on individual mods, not folders. When a search or filter is active, the folder structure is hidden and results are displayed as a flat list/grid with a folder badge on each mod indicating its parent folder (if any).
