# Feature Specification: Mod Layer Selection

**Feature Branch**: `004-mod-layer-selection`
**Created**: 2026-03-18
**Status**: Draft
**Input**: User description: "Implement layer selection UI for enabling/disabling mod layers in the library. Currently users can define mod layers in the workshop and when packing their mods, however, there is no way to select which layers should be enabled or not when enabling/installing."

## User Scenarios & Testing _(mandatory)_

### User Story 1 - View and Toggle Mod Layers (Priority: P1)

A user installs a multi-layer mod (e.g., a skin mod with a base layer and multiple chroma variants). When viewing the mod in their library, they can see all available layers and toggle individual layers on or off. Disabled layers are excluded from the overlay when the patcher runs.

**Why this priority**: This is the core value of the feature — giving users granular control over which parts of a mod are active. Without this, all layers are always enabled and users have no way to customize their experience with multi-layer mods.

**Independent Test**: Install a multi-layer mod, toggle a layer off, run the patcher, and verify the disabled layer's content is not applied to the game.

**Acceptance Scenarios**:

1. **Given** a mod with 3 layers (base, chroma-red, chroma-blue) is installed, **When** the user views the mod's layers, **Then** all 3 layers are listed with their names, descriptions (if available), and individual toggle controls.
2. **Given** a mod with layers displayed, **When** the user disables "chroma-blue", **Then** the toggle reflects the new state immediately and the overlay is marked for rebuild.
3. **Given** a mod with only 1 layer (base), **When** the user views the mod, **Then** no layer configuration UI is shown — the mod behaves identically to today.
4. **Given** a mod with layers of varying priorities, **When** the layers are displayed, **Then** they appear sorted by priority (lowest first), matching the overlay apply order.
5. **Given** all layers of a mod are disabled, **When** the overlay is built, **Then** the mod contributes no content (equivalent to being fully disabled, but the mod remains in the enabled list).

---

### User Story 2 - Layer Picker on Mod Enable (Priority: P2)

When a user enables a multi-layer mod (via the toggle switch on the mod card), they are presented with a compact layer picker that lets them choose which layers to activate before the mod is fully enabled. This prevents the scenario where all layers are blindly enabled and the user must retroactively discover and disable unwanted layers.

**Why this priority**: Improves the first-time experience with multi-layer mods. Users can still configure layers after enabling (via P1), so this is an enhancement, not a prerequisite.

**Independent Test**: Disable a multi-layer mod, toggle it on, verify a layer picker appears, select a subset of layers, confirm, and verify only the selected layers are active.

**Acceptance Scenarios**:

1. **Given** a multi-layer mod is disabled, **When** the user toggles it on, **Then** a compact layer picker appears (popover or inline panel) showing all layers with toggles, all defaulting to enabled.
2. **Given** the layer picker is shown, **When** the user unchecks some layers and confirms, **Then** the mod is enabled with only the selected layers active.
3. **Given** a single-layer mod is disabled, **When** the user toggles it on, **Then** the mod is enabled immediately with no layer picker (zero extra friction for simple mods).
4. **Given** the layer picker is shown, **When** the user dismisses it without confirming (e.g., clicks outside or presses Escape), **Then** the mod remains disabled (the toggle reverts).

---

### User Story 3 - Layer State Persistence Per Profile (Priority: P2)

Each profile maintains its own independent layer configuration for every mod. When a user switches profiles, the layer states reflect that profile's saved configuration. This integrates naturally with the existing profile system.

**Why this priority**: Profiles are the existing mechanism for managing different mod configurations. Layer states must be profile-scoped to be useful — a user may want different chroma variants active in different profiles.

**Independent Test**: Configure different layer states for the same mod in two profiles, switch between them, and verify each profile shows the correct layer states.

**Acceptance Scenarios**:

1. **Given** Profile A has mod X with [base: on, chroma: off] and Profile B has mod X with [base: on, chroma: on], **When** the user switches from Profile A to Profile B, **Then** mod X shows chroma as enabled.
2. **Given** a mod is installed while Profile A is active, **When** the user switches to Profile B (where the mod is also enabled but has no saved layer config), **Then** the mod's layers default to all enabled.
3. **Given** a user changes layer states in Profile A, **When** they switch to Profile B and back to Profile A, **Then** Profile A's layer states are exactly as they left them.

---

### User Story 4 - Multi-Layer Mod Visual Indicator (Priority: P3)

Users with many installed mods can quickly identify which mods have configurable layers from the library view, without having to open each mod's details.

**Why this priority**: Quality-of-life improvement for power users with large mod libraries. Not required for core functionality.

**Independent Test**: View a library containing both single-layer and multi-layer mods and verify that multi-layer mods are visually distinguishable.

**Acceptance Scenarios**:

1. **Given** a library with a mix of single-layer and multi-layer mods, **When** the user views the library in grid or list mode, **Then** multi-layer mods display a visual indicator (e.g., layer count badge or icon) on their cards.
2. **Given** a multi-layer mod where some layers are disabled, **When** the user views the library, **Then** the indicator reflects the partial state (e.g., "2/4 layers" or similar).

---

### Edge Cases

- What happens when a mod is re-installed with a different set of layers? Layer states for removed layers are discarded; new layers default to enabled; existing layers retain their state.
- What happens when a mod has layers but the base layer is disabled? The system allows it — users may want only variant layers active (no enforcement of base-always-on).
- How does the system handle a mod with many layers (10+)? The layer list is scrollable with a reasonable max height.
- What happens when a user imports/exports a profile? Layer configurations are included in the profile data.
- What happens if a layer has no description? The layer is shown with its name only — descriptions are displayed when available but not required.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST display all layers for a mod when the mod has more than one layer, including name, description (when available), and enabled state.
- **FR-002**: System MUST allow users to toggle individual layers on or off for each enabled mod.
- **FR-003**: System MUST persist layer enabled/disabled states per mod per profile.
- **FR-004**: System MUST respect layer enabled/disabled states when building the overlay — disabled layers are excluded from the overlay build.
- **FR-005**: System MUST default all layers to enabled when a mod is first installed or first enabled in a profile with no existing layer configuration.
- **FR-006**: System MUST NOT show layer configuration UI for mods with only one layer.
- **FR-007**: System MUST invalidate the overlay when any layer's enabled state changes.
- **FR-008**: System MUST display layers sorted by priority (lowest first), matching the overlay apply order.
- **FR-009**: System MUST present a layer picker when enabling a multi-layer mod, allowing the user to choose layers before activation.
- **FR-010**: System MUST clean up stale layer states when a mod is re-installed with a different layer set — remove states for deleted layers, default new layers to enabled, preserve states for unchanged layers.
- **FR-011**: System MUST show a visual indicator on mod cards for mods with multiple layers.

### Key Entities

- **Mod Layer**: A named, prioritized content container within a mod. Attributes: name, priority, description (optional), enabled state (per profile).
- **Layer Configuration**: A per-profile, per-mod mapping of layer names to their enabled/disabled state. Stored within the profile data structure.
- **Profile** (extended): Augmented to include layer configuration data alongside the existing enabled_mods and mod_order lists.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Users can view and toggle mod layers within 2 interactions from the library view (no deep navigation required).
- **SC-002**: Toggling a layer provides immediate visual feedback (under 1 second); overlay invalidation occurs in the background.
- **SC-003**: 100% of multi-layer mods display their available layers and allow per-layer configuration after this feature ships.
- **SC-004**: Layer states are correctly preserved across profile switches with zero data loss.
- **SC-005**: Single-layer mods have zero additional UI complexity compared to the current experience.
- **SC-006**: Users enabling a multi-layer mod for the first time can configure layers before activation, reducing retroactive adjustments.

## Assumptions

- The existing `ModLayer` structure (name, priority, enabled) is sufficient for layer representation — no additional metadata (e.g., string overrides) needs to be exposed in the layer selection UI.
- Layer descriptions from the mod's metadata will be displayed when available but are not required (many mods may not include descriptions).
- The overlay builder in `ltk_overlay` already supports filtering layers — this feature focuses on passing the correct enabled/disabled state from the frontend through to the overlay build.
- Layer configuration is profile-scoped (not global) because different profiles may need different layer setups for the same mod.
- The "base" layer is treated the same as any other layer — users may disable it if desired.
- The layer picker on enable (P2) defaults all layers to enabled, requiring users to opt-out rather than opt-in, since most users will want all layers active.
