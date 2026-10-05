# Feature Specification: Lightweight Mod Content Browser

**Feature Branch**: `012-mod-content-browser`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "Lightweight mod content browser surfacing per-layer file listings with League file-kind icons, drag-drop adds, and a handoff to LTK Forge for editing."

## User Scenarios & Testing _(mandatory)_

### User Story 1 - See what's in the mod project at a glance (Priority: P1)

A mod creator opens a workshop project in the Manager and wants to know what the mod actually contains right now: which layers exist, how many files each layer has, how large it is, what kinds of game assets are present, and where those files sit in the project structure. Today the Manager creates the project folder but hides the contents; creators have to open a file explorer to answer basic questions about their own project.

**Why this priority**: This is the entire feature's reason for existing. Without visibility, the remaining stories have nothing to attach to. A browser that only shows contents — nothing else — already delivers the core value.

**Independent Test**: Open an existing project that has files in several layer folders. The content panel shows each layer with a summary (file count, total size) and a readable listing of the files inside, each tagged with an icon that reflects what type of League asset it is. No other interaction required.

**Acceptance Scenarios**:

1. **Given** a project with files in `base/` and one additional layer, **When** the creator opens the project overview, **Then** a "Content" panel displays both layers, each showing its file count, total size, and a per-file listing with relative paths.
2. **Given** a project containing recognized League asset files (textures, meshes, animations, audio banks, property bins), **When** the creator views the content panel, **Then** each file row shows an icon that groups it into the appropriate asset category and a tooltip that names the exact kind in plain language.
3. **Given** a project with a file whose extension isn't a recognized League asset type, **When** the creator views the content panel, **Then** the file is still listed, with a generic "unknown" icon and its filename and size visible.
4. **Given** a project with an empty layer folder, **When** the creator views the content panel, **Then** that layer shows an empty-state message directing them to extract game files, with a link to the LTK extraction guide.
5. **Given** a project containing tens of thousands of files, **When** the creator views the content panel, **Then** the tree renders responsively by virtualizing rows — scrolling through the full listing stays smooth regardless of total file count.

---

### User Story 2 - Add files to a layer via drag-and-drop (Priority: P2)

The creator has extracted or edited files outside the Manager and wants to pull them into the active layer without navigating the OS file manager, copying paths manually, or running a separate import wizard.

**Why this priority**: The browser is most useful when creators can both see and incrementally fill a project. Drag-drop turns passive visibility into a two-way workflow, but the feature still delivers value without it (creators can drop files directly into the folder via the OS and the browser will reflect the change on refresh).

**Independent Test**: With a project open, drag one or more files from the OS onto the content panel. The files appear in the currently active layer's listing with correct metadata, no manual refresh required.

**Acceptance Scenarios**:

1. **Given** a project is open to its active layer, **When** the creator drags a single file onto the content panel, **Then** the file is copied into the active layer's content directory and appears in the listing with its correct kind, size, and relative path.
2. **Given** a project is open, **When** the creator drags multiple files at once, **Then** all files are copied in a single operation and appear together in the listing.
3. **Given** a drag-drop copy fails partway (e.g., disk full, permission denied), **When** the operation completes, **Then** the creator sees a clear failure message naming which files failed and why, and the listing reflects only the files that succeeded.

---

### User Story 3 - Hand off to LTK Forge for editing (Priority: P3)

When the creator needs to actually inspect or modify a file — preview a texture, edit a property bin, repack a WAD — they expect the Manager to point them to LTK Forge (the dedicated editor) rather than trying to preview or edit inside the Manager itself.

**Why this priority**: Editing is out of scope for the Manager by design. A handoff button is a nice-to-have that reinforces the tool boundary, but the content browser delivers its primary value (visibility + drag-drop) without it. Forge's integration surface also isn't finalized, so shipping this as a later, polished handoff is acceptable.

**Independent Test**: With Forge installed and a project open, activate the "Open in LTK Forge" action on a layer. Forge launches with the correct layer folder loaded. With Forge not installed, the action shows a one-line hint linking to Forge's download page.

**Acceptance Scenarios**:

1. **Given** LTK Forge is installed and registered as the handler for its integration surface, **When** the creator activates "Open in LTK Forge" on a layer, **Then** Forge launches (or foregrounds) and opens that layer's content folder.
2. **Given** LTK Forge is not installed, **When** the creator activates the same action, **Then** they see a short, dismissible hint linking to Forge's releases page instead of a silent failure or an error dialog.
3. **Given** the creator activates "Open folder", **Then** the OS file manager reveals the layer's content directory. This action is always available regardless of Forge's installation state.

---

### User Story 4 - Keep the listing fresh when files change outside the Manager (Priority: P3)

The creator edits files in an external tool (Forge, Photoshop, Blender, etc.), returns to the Manager, and expects the content listing to reflect what's on disk now — not a snapshot from when the project was opened.

**Why this priority**: Stale data is annoying but rarely catastrophic — creators can always reopen the project or trigger a manual refresh. The primary value of the browser survives without automatic refresh; automatic refresh just removes friction.

**Independent Test**: Open a project, switch away from the Manager, add/remove/modify files in the layer directory via another tool, switch back to the Manager. The listing reflects the change without any explicit refresh action.

**Acceptance Scenarios**:

1. **Given** a project is open and the Manager loses window focus, **When** files are added, removed, or resized in the layer directory, and the Manager regains focus, **Then** the listing updates to reflect the new state within a couple of seconds.
2. **Given** a project is open, **When** the creator activates the manual "Refresh" control, **Then** the listing rescans and reflects current disk state.
3. **Given** a drag-drop add completes, **When** the copy finishes, **Then** the listing updates without requiring a manual refresh.

---

### Edge Cases

- **Very large projects**: What happens when a layer contains tens of thousands of files? The tree virtualizes its rows so only the visible window is mounted. Scrolling and expand/collapse stay responsive; no truncation, no UI hang.
- **Missing or corrupt layer directory**: A layer folder referenced by project config but missing on disk renders as an empty layer with an explanatory message; it does not crash the panel or block other layers from rendering.
- **Hidden files and symlinks**: Dotfiles and symlinks are skipped, so `.DS_Store`, `Thumbs.db`, and accidental link loops don't pollute the listing or cause traversal errors.
- **Files with unrecognized extensions**: Still listed, bucketed under an "unknown" category. Never dropped from the listing.
- **Files with deceptive extensions** (correct byte contents but wrong extension): Listed according to their extension; magic-byte verification is deliberately not part of v1. Acceptable trade-off for performance.
- **Drag-drop onto a project with no active layer selected**: The drop target is disabled with a clear hint ("select a layer first") rather than silently copying to `base/`.
- **Concurrent external edit during refresh**: If files change while a scan is in progress, the scan may return a snapshot from somewhere between the states; the next focus-triggered refresh reconciles.
- **Open in LTK Forge on an empty layer**: Still opens Forge at the layer folder — a creator may want to seed an empty layer from inside Forge.

## Requirements _(mandatory)_

### Functional Requirements

**Visibility**

- **FR-001**: The project overview MUST include a dedicated Content section that lists the project's layers and the files contained in each.
- **FR-002**: For each layer, the system MUST display the layer name, total file count, and total size.
- **FR-003**: For each file, the system MUST display its filename, path relative to the layer root, size, and an icon that reflects its League asset category.
- **FR-004**: The system MUST classify each file into one of a fixed set of asset categories (texture/image, mesh, animation/rig, property data, text/strings, audio, light data, or unknown) based on its file extension.
- **FR-005**: The system MUST display a human-readable name of the detected asset kind on hover, so creators can learn what they're looking at without consulting external documentation.
- **FR-006**: Files whose extension is not a recognized League asset type MUST still appear in the listing, classified as "unknown".
- **FR-007**: The system MUST skip hidden files (leading `.`) and symlinks when building the listing.
- **FR-008**: The system MUST render the file listing responsively at any project size by virtualizing the tree rows — only the rows within the scroll viewport are mounted at a time. No entries are omitted or capped.
- **FR-009**: Layers MUST be ordered with the `base` layer first, followed by other layers alphabetically. Files within a layer MUST be ordered by relative path.
- **FR-010**: When a layer directory is empty, the system MUST show a short empty-state message directing the creator to an extraction guide.

**File addition**

- **FR-011**: Creators MUST be able to add one or more files to the currently active layer by dragging them onto the Content section from the OS.
- **FR-012**: After a successful drag-drop, the listing MUST reflect the newly added files without requiring a manual refresh.
- **FR-013**: When a drag-drop operation partially fails, the system MUST report which files were not added and why, without discarding the files that did succeed.

**External editor handoff**

- **FR-014**: Each layer MUST expose an "Open folder" action that reveals the layer's content directory in the OS file manager.
- **FR-015**: Each layer MUST expose an "Open in LTK Forge" action that launches Forge at that layer's folder when Forge is available on the system.
- **FR-016**: When LTK Forge is not available, the "Open in LTK Forge" action MUST present the creator with a link to Forge's releases page rather than silently failing.
- **FR-017**: The system MUST NOT embed file preview, file editing, or format conversion within the Content section.

**Refresh**

- **FR-018**: The Content listing MUST refresh automatically when the application window regains focus.
- **FR-019**: The Content section MUST provide a manual refresh control that rescans the project on demand.
- **FR-020**: The system MUST NOT require the creator to navigate away and back to see changes made by external tools.

**Scope protection**

- **FR-021**: The feature MUST NOT modify the on-disk project format (project configuration schema or content directory layout).
- **FR-022**: The feature MUST NOT introduce new persistent state outside what already exists for a workshop project.

### Key Entities _(include if feature involves data)_

- **Project Content Tree**: The aggregate view of a single workshop project's layers and their files. Composed of one or more Layer Contents.
- **Layer Content**: One layer's files under the project's content directory. Carries the layer's display name, total file count, total size, and the ordered list of File Entries.
- **File Entry**: A single file within a layer. Carries its filename, path relative to the layer root, size, and an asset kind classification.
- **Asset Kind**: The classification applied to each file entry. Groups League-specific file formats (textures, meshes, animations, property data, strings, audio, light data) into visually distinct categories, with an "unknown" fallback for unclassified files.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: A creator who has never seen the Content section before can correctly identify how many files are in each layer of an existing project within 10 seconds of opening it.
- **SC-002**: A creator can identify whether a specific file in their project is a texture, a mesh, a property bin, or an audio asset purely from its row in the listing, without opening the file externally, in 90% of cases across the recognized League asset types.
- **SC-003**: A creator can add files to a layer by dragging them from the OS in under 5 seconds, and the new files appear in the listing within 2 seconds of the drop completing.
- **SC-004**: After editing files in an external tool, the creator sees the updated state reflected in the Content section within 3 seconds of returning focus to the Manager, without performing any manual action.
- **SC-005**: For a project containing up to 500 files across its layers, the Content section renders fully within 2 seconds of the project view opening.
- **SC-006**: For a project containing tens of thousands of files (up to at least 50,000), the Content section opens and scrolls smoothly — no dropped frames during scroll, no hang during expand/collapse.
- **SC-007**: Zero regressions are observed in existing workshop features (project creation, packing, layer management, imports, thumbnail management) after the Content section is added to the project overview.

## Assumptions

- The Manager intentionally delegates file inspection, preview, and editing to LTK Forge. The Content section's job ends at visibility, drag-drop adds, and handoff.
- Classification by file extension is sufficient for the first iteration. Byte-level verification (catching mislabeled files) is deliberately deferred; it is not required to deliver the listed success criteria.
- Real-world mods can legitimately contain tens of thousands of files. The Content section scales by virtualizing the tree rather than capping entries.
- External tool edits to layer directories happen while the Manager is not the foreground window. Refresh-on-focus is therefore sufficient; a live file watcher is not required to meet the success criteria.
- LTK Forge exposes — or will expose — a stable way to be launched at a specific folder (protocol handler or CLI argument). The handoff story is conditional on that surface existing.
- The `base` layer is always present in a valid workshop project. Additional layers are optional and sorted alphabetically after `base`.
- The LTK documentation wiki is the canonical destination for "how to extract game files" and similar guidance links.
