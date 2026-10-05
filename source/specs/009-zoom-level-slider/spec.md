# Feature Specification: Zoom Level Slider

**Feature Branch**: `009-zoom-level-slider`
**Created**: 2026-03-29
**Status**: Draft
**Input**: User description: "Rework UI density into Spotify-style zoom level slider with percentage steps"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Select Zoom Level via Percentage Steps (Priority: P1)

A user opens the settings page and sees a "Zoom level" control that presents a horizontal row of selectable percentage steps (70%, 80%, 90%, 100%, 110%, 120%, 130%). The user clicks on a percentage to immediately apply that zoom level to the entire UI. The currently selected step is visually highlighted. The zoom level persists across app restarts.

**Why this priority**: This is the core interaction — replacing the three-option density picker with a fine-grained percentage-based zoom selector. Without this, no other story delivers value.

**Independent Test**: Can be fully tested by opening settings, selecting different zoom percentages, and observing that spacing and icon sizes change across the app. Persists after restart.

**Acceptance Scenarios**:

1. **Given** the user is on the settings page, **When** they view the zoom level control, **Then** they see a horizontal row of percentage options (70% through 130% in 10% increments) with the current selection highlighted.
2. **Given** a zoom level is selected, **When** the user clicks a different percentage, **Then** the UI immediately updates all spacing and icon sizes to reflect the new scale factor.
3. **Given** the user selects a zoom level, **When** they close and reopen the app, **Then** the previously selected zoom level is still active.
4. **Given** the app is freshly installed, **When** the user first views the zoom control, **Then** 100% is selected as the default.

---

### User Story 2 - Visual Landmarks for Dense, Default, and Spacious (Priority: P2)

The zoom level control displays three visual landmark labels — "Dense" at the low end, "Default" at the center (100%), and "Spacious" at the high end — similar to Spotify's zoom level UI. These labels help users understand the effect of each region without needing to try every step.

**Why this priority**: Landmarks provide orientation and make the control self-explanatory, but the core zoom functionality works without them.

**Independent Test**: Can be tested by visually verifying the labels appear at the correct positions above the percentage steps.

**Acceptance Scenarios**:

1. **Given** the zoom control is rendered, **When** the user views it, **Then** they see "Dense" aligned near the lowest percentage, "Default" aligned at 100%, and "Spacious" aligned near the highest percentage.

---

### User Story 3 - Reset to Default (Priority: P3)

A "Reset" action is available within the zoom level control that returns the zoom to the default value (100%) with a single click.

**Why this priority**: Convenience feature — users can always manually click 100%, but a reset button adds polish.

**Independent Test**: Can be tested by selecting a non-default zoom level, clicking Reset, and verifying the zoom returns to 100%.

**Acceptance Scenarios**:

1. **Given** the zoom level is set to a non-default value, **When** the user clicks the Reset action, **Then** the zoom level returns to 100% and the UI updates immediately.
2. **Given** the zoom level is already at 100%, **When** the user views the Reset action, **Then** the Reset action is either hidden or visually de-emphasized.

---

### Edge Cases

- What happens when the persisted zoom value from a previous version uses the old density format ("compact"/"normal"/"spacious")? The system should migrate gracefully to the nearest percentage equivalent.
- What happens if a persisted zoom percentage is outside the supported range (e.g., from a future or past version)? The system should clamp to the nearest valid step or fall back to 100%.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST present zoom levels as discrete percentage steps from 70% through 130% in 10% increments (70%, 80%, 90%, 100%, 110%, 120%, 130%).
- **FR-002**: System MUST apply the selected zoom level immediately to all spacing tokens and icon size tokens across the entire UI.
- **FR-003**: System MUST persist the selected zoom level across app sessions using the existing display preferences storage.
- **FR-004**: System MUST default to 100% zoom on fresh installations.
- **FR-005**: System MUST display visual landmark labels ("Dense", "Default", "Spacious") at the low end, center (100%), and high end of the zoom control.
- **FR-006**: System MUST provide a "Reset" action that returns the zoom to 100%.
- **FR-007**: System MUST migrate previously saved density values ("compact", "normal", "spacious") to their nearest percentage equivalents upon first load after the update.
- **FR-008**: The zoom control MUST use a horizontal radio-style layout with individual selectable steps (not a continuous slider), matching the Spotify reference design.

### Key Entities

- **Zoom Level**: A percentage value representing the UI scale factor. Valid values: 70, 80, 90, 100, 110, 120, 130. Stored as a number. Replaces the previous "density" concept (compact/normal/spacious).

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Users can change the zoom level in a single click from the settings page.
- **SC-002**: All spacing and icon sizes across the UI adjust proportionally when zoom level changes, with no elements clipping or overlapping at any supported zoom level.
- **SC-003**: Previously saved density preferences are automatically migrated so users do not experience a reset to default after updating.
- **SC-004**: The zoom control is visually self-explanatory — landmark labels ("Dense", "Default", "Spacious") allow users to understand the effect without trial-and-error.

## Assumptions

- The zoom level affects spacing and icon size tokens only, consistent with the current density system. It does not affect font sizes, border radii, shadows, z-index, or color tokens.
- The percentage steps (70%–130% in 10% increments) provide sufficient granularity. 100% maps to the current "spacious" (scale 1.0) baseline since that represents the unscaled token values.
- Keyboard shortcuts for zoom (Ctrl+/Ctrl-) mentioned in the Spotify reference are out of scope for this feature unless explicitly requested.
- The visual design of the zoom control (radio dots, landmark icons/illustrations) follows Spotify's general pattern but will be adapted to match the existing app design language.
- Migration mapping from old density values: "compact" → 70%, "normal" → 80%, "spacious" → 100%.
