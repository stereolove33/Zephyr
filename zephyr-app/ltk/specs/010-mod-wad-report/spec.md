# Feature Specification: Per-Mod WAD Footprint Visibility

**Feature Branch**: `010-mod-wad-report`
**Created**: 2026-04-08
**Status**: Draft
**Input**: User description: "Show which WADs each mod affects, with cached per-mod reports surfaced on the mod card via a count badge and a popover detail view, computed during real patches and on-demand for never-patched mods, invalidated when the mod content or game changes."

## User Scenarios & Testing

### User Story 1 — See at a glance which mods touch the most WADs (Priority: P1)

A user with dozens of installed mods wants to identify which mods are responsible for a bloated patch. Each mod card shows a small badge with the number of game WADs that mod affects, color-tiered so unusually heavy mods stand out immediately. The user can scan the library and decide which mods to disable or uninstall to bring patch size down.

**Why this priority**: This is the core problem in the originating request — without the badge there is no way to make this decision without trial-and-error patching. Delivers value even with no detail view.

**Independent Test**: Install a mix of small (single-champion) and large (overhaul) mods, run a patch, then visually confirm each card displays a count and that overhaul mods show a visibly elevated tier.

**Acceptance Scenarios**:

1. **Given** a mod has been included in at least one successful patch, **When** the user views the library, **Then** that mod's card shows a badge with the exact number of game WADs the mod affects.
2. **Given** a mod affects a number of WADs in the "high" tier, **When** the user views its card, **Then** the badge is visually distinguished (warning color) from low- and medium-tier mods.
3. **Given** a mod has never been patched and has not been analyzed, **When** the user views its card, **Then** the badge shows an unknown/placeholder state instead of a number.

---

### User Story 2 — Inspect exactly which WADs a mod touches (Priority: P2)

When a count looks suspicious, the user wants to drill down and see the specific WAD names, ideally grouped by category (champions, maps, UI, other) so they can confirm whether the mod is staying within its declared scope or sneakily reaching into unrelated areas.

**Why this priority**: Builds directly on P1 and addresses the secondary "abuse detection" angle from the request, but the badge alone is already useful without it.

**Independent Test**: Hover or click the badge on a known mod and verify the popover lists the expected WAD names grouped by category, with totals matching the badge count.

**Acceptance Scenarios**:

1. **Given** a mod has a computed report, **When** the user opens its WAD detail view, **Then** the user sees the full list of affected WADs grouped by category and the timestamp the report was last computed.
2. **Given** the affected WAD list is long, **When** the user opens the detail view, **Then** the list is scrollable and remains readable.
3. **Given** the user is viewing the detail view, **When** the user requests a re-analysis, **Then** the report is recomputed and the view updates without requiring a full patch.

---

### User Story 3 — Get a report for a freshly installed mod without patching the whole library (Priority: P2)

A user has just installed a new mod and wants to know its WAD footprint before committing to a full patch run. They click an "Analyze" affordance on the card and the system computes the report for that single mod on demand.

**Why this priority**: Removes the "must patch everything first" friction. Important for users with large libraries where a full patch is expensive.

**Independent Test**: Install a new mod, confirm the badge shows the unknown state, trigger analyze, and confirm the badge populates without any patch being run.

**Acceptance Scenarios**:

1. **Given** a mod has no cached report, **When** the user triggers analyze on that mod, **Then** the mod's report is computed and the badge updates to show the count.
2. **Given** the user triggers analyze, **When** the patcher is currently running, **Then** the analyze action still succeeds without disturbing the patcher.
3. **Given** analyze fails (e.g., mod archive is corrupted), **When** the failure occurs, **Then** the user sees a clear error message and the badge returns to its prior state.

---

### User Story 4 — Trust that reports stay accurate (Priority: P3)

When a mod's archive changes externally, or the user updates League and the set of game WADs shifts, the cached report for affected mods becomes stale. The system detects this and either refreshes the report automatically on the next patch or marks the badge as stale so the user knows not to trust the displayed number.

**Why this priority**: Correctness safeguard. Without it the feature degrades silently over time, but the core value is delivered before this lands.

**Independent Test**: Modify a mod's archive on disk (or simulate a game update), reopen the library, and verify the affected badges are marked stale until recomputed.

**Acceptance Scenarios**:

1. **Given** a mod's content has changed since its last report, **When** the user views the library, **Then** the badge displays a stale indicator.
2. **Given** the game's WAD set has changed since a report was computed, **When** the user views the library, **Then** affected badges display a stale indicator.
3. **Given** a stale report exists, **When** the next successful patch runs that includes the mod, **Then** the report is refreshed automatically and the stale indicator clears.

---

### Edge Cases

- A mod affects zero WADs (e.g., empty or invalid layers): badge shows `0` and the detail view explains the empty result.
- A mod is enabled but disabled in every layer: report still reflects the mod's full potential footprint, not the currently-enabled subset, so the badge does not flicker as layer toggles change.
- Two mods with identical content: each gets its own cached entry; no cross-mod sharing required.
- Analyze is triggered repeatedly in quick succession on the same mod: the second invocation reuses the in-flight result rather than duplicating work.
- Cache file is missing or corrupted on startup: system treats all reports as absent and rebuilds them as patches run; no crash, no data loss.
- A patch run fails partway: only mods whose reports were fully computed are persisted; partial reports are discarded.

## Requirements

### Functional Requirements

- **FR-001**: System MUST compute, for each enabled mod participating in a successful patch, the set of game WADs that mod's overrides affect, independent of load order with other mods.
- **FR-002**: System MUST persist each mod's WAD report (count, full WAD list, computed-at timestamp, content fingerprint, game-index fingerprint) so it survives application restarts.
- **FR-003**: System MUST display, on each mod card in the library, a badge showing the affected-WAD count when a report is available.
- **FR-004**: System MUST visually distinguish mods whose WAD count falls into a "high" tier from low and medium tiers, using a tiering scheme that the user can recognize at a glance.
- **FR-005**: System MUST display a clear placeholder state on the badge when no report is available for a mod.
- **FR-006**: Users MUST be able to open a detail view from each mod card showing the full list of affected WADs grouped into recognizable categories (champions, maps, UI, other), the total count, and the last-computed timestamp.
- **FR-007**: Users MUST be able to trigger an on-demand analysis of a single mod that computes and persists its report without running a full patch and without interfering with an in-progress patch.
- **FR-008**: System MUST detect when a cached report is stale because the mod's content has changed since the report was computed, and mark the badge accordingly until refreshed.
- **FR-009**: System MUST detect when a cached report is stale because the game's WAD set has changed since the report was computed, and mark the badge accordingly until refreshed.
- **FR-010**: System MUST automatically refresh a stale report the next time the mod participates in a successful patch.
- **FR-011**: System MUST surface analysis failures (corrupt archive, missing game data) to the user with a clear message and leave any prior cached report untouched.
- **FR-012**: System MUST tolerate a missing or corrupted report cache on startup by treating all reports as absent rather than failing to launch the library.
- **FR-013**: System MUST keep WAD report computation off the user-interface critical path so that opening the library remains responsive regardless of cache size.

### Key Entities

- **Mod WAD Report**: A record describing one mod's WAD footprint. Attributes: mod identifier, affected-WAD count, ordered list of affected WAD names, total override count, timestamp the report was computed, fingerprint of the mod content the report was computed against, fingerprint of the game's WAD index the report was computed against.
- **WAD Report Cache**: The collection of all Mod WAD Reports, persisted alongside the existing mod library so it can be loaded at startup and updated as patches run or analyses are triggered.

## Success Criteria

### Measurable Outcomes

- **SC-001**: After a single successful full-library patch, 100% of enabled mods show a numeric WAD-count badge on their card.
- **SC-002**: A user with 30+ installed mods can identify the top 3 WAD-heaviest mods in under 30 seconds of scanning the library.
- **SC-003**: On-demand analysis of a single mod completes and updates the badge in under 2 seconds for a typical mod on typical hardware.
- **SC-004**: After a mod's archive is modified externally, the next time the user opens the library the corresponding badge displays a stale indicator with no manual user action required.
- **SC-005**: Opening the library with 100+ cached reports adds no perceptible delay (under 100 ms) compared to opening it with the cache empty.
- **SC-006**: Zero crashes or library-load failures attributable to a missing, partial, or corrupted report cache file.

## Assumptions

- The existing mod library, profile, and patcher flows remain the source of truth for which mods exist and which are enabled; this feature only annotates them with WAD footprint data.
- A mod's WAD footprint is reported as the union of WADs its overrides could touch, independent of which other mods are loaded alongside it. Cross-mod conflict resolution is out of scope for this feature.
- "Affected WADs" are reported using the game's WAD names/paths as they appear in the installed game index, not the mod's internal layer directory names.
- Grouping categories in the detail view (champions, maps, UI, other) are derived from WAD path conventions; mods that touch unrecognized paths fall into "other".
- The existing reconciliation pass that detects externally modified mod archives is the trigger point for content-fingerprint invalidation.
- Sorting and filtering the library by WAD count, and any "suspicious mod" heuristic that compares affected WAD categories against a mod's declared champions/tags, are out of scope for this initial release and may follow as separate features.
- The feature is delivered for the existing desktop library view only; no changes to mobile, web, or external integrations are implied.
