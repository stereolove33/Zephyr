# Feature Specification: Improved Project Creation Dialog UX

**Feature Branch**: `005-project-creation-ux`
**Created**: 2026-03-19
**Status**: Draft
**Input**: User description: "We need to make the UX for mod creators much better. The new project creation dialog is a bit stale and needs more life to it. We should derive the project name/slug from the display name etc... also maybe give them the ability to set a thumbnail directly in the dialog."

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Display Name Drives Slug Generation (Priority: P1)

A mod creator opens the "New Project" dialog and types a human-readable display name first (e.g., "My Awesome Skin"). As they type, the project slug auto-generates below (e.g., `my-awesome-skin`). The creator can see exactly what the folder name will be, and can manually override the slug if desired. Once overridden, the slug stops auto-updating from the display name.

**Why this priority**: This is the core UX inversion — currently users must think in slugs first, which is unintuitive. Leading with the display name matches how creators naturally think about their mods and removes friction from the most common path.

**Independent Test**: Can be fully tested by opening the dialog, typing a display name, and verifying the slug auto-populates correctly. Delivers value by making project creation feel natural and reducing naming errors.

**Acceptance Scenarios**:

1. **Given** the dialog is open and the slug has not been manually edited, **When** the user types "Cool Champion Skin" in the Display Name field, **Then** the slug field shows `cool-champion-skin` in real-time.
2. **Given** the dialog is open, **When** the user types a display name containing special characters (e.g., "Kai'Sa's New Look!"), **Then** the slug strips invalid characters and shows `kaisas-new-look`.
3. **Given** the auto-generated slug is visible, **When** the user manually edits the slug field, **Then** the slug stops auto-updating from the display name for the remainder of the session.
4. **Given** the user has manually edited the slug, **When** the user changes the display name again, **Then** the slug retains its manually-set value and does not auto-update.

---

### User Story 2 - Thumbnail Selection During Creation (Priority: P2)

A mod creator wants to set a thumbnail image for their project right from the creation dialog, rather than having to create the project first and then navigate to settings to add one. The dialog provides a visual thumbnail area that the creator can click to open a file picker, or they see a placeholder encouraging them to add one. This is optional — projects can still be created without a thumbnail.

**Why this priority**: Setting a thumbnail during creation reduces the number of steps to get a polished project set up. The existing thumbnail infrastructure (WebP conversion, atomic writes) already handles the heavy lifting; this story surfaces it earlier in the workflow.

**Independent Test**: Can be tested by opening the dialog, clicking the thumbnail area, selecting an image, and verifying it appears as a preview. The project should be created with the thumbnail already set.

**Acceptance Scenarios**:

1. **Given** the dialog is open, **When** the user clicks the thumbnail placeholder area, **Then** a file picker opens filtered to supported image formats (webp, png, jpg, jpeg, gif, bmp, tiff, ico).
2. **Given** an image has been selected, **When** the dialog displays, **Then** a preview of the selected image is shown in the thumbnail area at 16:9 aspect ratio.
3. **Given** an image preview is shown, **When** the user clicks a remove/change action, **Then** the image is cleared and the placeholder returns.
4. **Given** no thumbnail is selected, **When** the user submits the form, **Then** the project is created without a thumbnail (no error, fully optional).
5. **Given** a thumbnail is selected and the form is valid, **When** the user submits, **Then** the project is created and the thumbnail is applied via the existing `set_thumbnail` flow.

---

### User Story 3 - Refreshed Dialog Layout and Feel (Priority: P3)

The dialog feels more polished and engaging compared to the current plain form layout. The display name field is prominent and inviting, the slug is presented as secondary/derived information, and the thumbnail area gives the dialog visual personality. The overall layout guides the creator through the process naturally.

**Why this priority**: While functional improvements (P1, P2) deliver the most value, a refreshed visual layout makes the creation experience feel modern and encourages creators to fill out more metadata upfront.

**Independent Test**: Can be tested by opening the dialog and evaluating the visual hierarchy — display name should be the most prominent input, slug should feel secondary/derived, and the thumbnail area should be visually integrated.

**Acceptance Scenarios**:

1. **Given** the dialog is open, **When** the user views the form, **Then** the Display Name field is the first and most prominent input.
2. **Given** the dialog is open, **When** the user views the slug field, **Then** it appears as secondary/derived information (smaller, less prominent than display name).
3. **Given** the dialog is open, **When** the user views the thumbnail area, **Then** it is visually integrated into the dialog (not tucked away or afterthought-like).

---

### Edge Cases

- What happens when the display name is empty? The slug should also be empty (no creation allowed without a valid slug).
- What happens when the display name produces an empty slug after sanitization (e.g., only special characters like "!!!")? The slug field should be empty and validation should prevent submission.
- What happens when the display name produces a slug that conflicts with an existing project? Existing duplicate-name validation should apply and show an appropriate error.
- What happens when the user selects a very large thumbnail image? The existing backend handles conversion to WebP; the dialog should not block on this during preview (preview uses the original file locally).
- What happens when the user clears the display name after having auto-generated a slug? The slug should clear as well (since it was never manually edited).

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The Display Name field MUST be the primary input — placed first in the form and visually emphasized.
- **FR-002**: The system MUST auto-generate a project slug from the display name by converting to lowercase, replacing spaces and special characters with hyphens, removing consecutive hyphens, and stripping leading/trailing hyphens.
- **FR-003**: The slug field MUST update in real-time as the user types in the Display Name field, as long as the user has not manually edited the slug.
- **FR-004**: Once the user manually edits the slug field, the system MUST stop auto-generating it from the display name for the remainder of that dialog session.
- **FR-005**: The slug field MUST still enforce its existing validation rules (lowercase alphanumeric and hyphens only, no leading/trailing hyphens).
- **FR-006**: The dialog MUST include an optional thumbnail selection area that opens a file picker for supported image formats.
- **FR-007**: When an image is selected, the dialog MUST display a preview at 16:9 aspect ratio.
- **FR-008**: The user MUST be able to remove a selected thumbnail before submitting the form.
- **FR-009**: When the form is submitted with a thumbnail selected, the system MUST create the project first, then apply the thumbnail using the existing `set_thumbnail` backend flow.
- **FR-010**: The Author field MUST persist the last-used author name so creators don't have to re-enter it for each new project.

### Key Entities

- **Project Slug**: The machine-readable identifier derived from the display name. Used as the project folder name. Constrained to lowercase letters, numbers, and hyphens.
- **Display Name**: The human-readable project name. Primary input from which the slug is derived.
- **Thumbnail**: An optional image file that represents the project visually. Stored as WebP after backend conversion.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Mod creators can complete project creation (including optional thumbnail) in a single dialog interaction without navigating elsewhere.
- **SC-002**: 90%+ of creators never need to manually edit the auto-generated slug — it should produce sensible results from natural display names.
- **SC-003**: The display name field is the first focusable element when the dialog opens, guiding creators to start with the human-readable name.
- **SC-004**: Thumbnail selection and preview add no more than one additional click to the creation flow compared to skipping it.

## Assumptions

- The existing `set_thumbnail` backend (WebP conversion, atomic writes) is reused as-is for applying thumbnails after project creation.
- The file picker for thumbnails uses Tauri's native file dialog, consistent with the existing thumbnail selection in the project overview.
- The author name persistence uses the existing settings or local storage mechanism — a reasonable default for single-creator workflows.
- The slug derivation logic handles Unicode by stripping non-ASCII characters (standard slug behavior), since the backend validates `[a-z0-9-]+`.
