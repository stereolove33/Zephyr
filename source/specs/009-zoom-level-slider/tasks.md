# Tasks: Zoom Level Slider

**Input**: Design documents from `/specs/009-zoom-level-slider/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Foundational (Store + CSS Token System)

**Purpose**: Replace the density data model and CSS token scaling — required before any UI work

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T001 [P] Replace `Density` type with `ZoomLevel` type, rename `density` to `zoomLevel` (default: 100), rename hooks to `useZoomLevel`/`useSetZoomLevel`, add Zustand persist `version: 1` with migration function (compact→70, normal→80, spacious→100) in `src/stores/displayStore.ts`
- [x] T002 [P] Remove all three `[data-density="..."]` CSS blocks, replace `:root` spacing/icon token definitions with `calc()`-based formulas using `--zoom-scale` variable (e.g., `--space-001: calc(1 * 6px * var(--zoom-scale, 1))`), scale `--card-min-w`/`--card-max-w` similarly, remove `--density-scale` in `src/styles/global.css`
- [x] T003 Update `__root.tsx` to read `zoomLevel` from store via `useZoomLevel()`, set `document.documentElement.style.setProperty('--zoom-scale', String(zoomLevel / 100))` in useEffect, remove old `data-density` attribute sync in `src/routes/__root.tsx`

**Checkpoint**: Zoom scaling works end-to-end with the old default. App renders correctly at 100% zoom.

---

## Phase 2: User Story 1 - Select Zoom Level via Percentage Steps (Priority: P1) 🎯 MVP

**Goal**: Replace the DensityPicker with a horizontal zoom level control showing 7 percentage steps (70%–130%)

**Independent Test**: Open settings, click each percentage step, verify spacing/icons scale across the app. Restart app, verify persistence.

### Implementation for User Story 1

- [x] T004 [US1] Create `ZoomLevelPicker` component with horizontal radio-dot layout for 7 zoom steps (70%–130%), highlight selected step, call `useSetZoomLevel()` on click in `src/modules/settings/components/AppearanceSection/ZoomLevelPicker.tsx`
- [x] T005 [US1] Update `AppearanceSection.tsx` to import and render `ZoomLevelPicker` instead of `DensityPicker` in `src/modules/settings/components/AppearanceSection/AppearanceSection.tsx`
- [x] T006 [US1] Delete old `DensityPicker.tsx` file at `src/modules/settings/components/AppearanceSection/DensityPicker.tsx`
- [x] T007 [US1] Update barrel export in `src/modules/settings/components/AppearanceSection/index.ts` if DensityPicker was exported
- [x] T008 [US1] Update store tests: replace density assertions with zoomLevel, add migration test (old format → new), update persistence test in `src/__tests__/stores/displayStore.test.ts`

**Checkpoint**: User Story 1 fully functional — zoom level selectable via percentage steps, persists across restart, old density values migrate correctly.

---

## Phase 3: User Story 2 - Visual Landmarks (Priority: P2)

**Goal**: Add "Dense", "Default", "Spacious" landmark labels above the zoom steps

**Independent Test**: Visually verify labels appear at correct positions — "Dense" at left, "Default" at 100%, "Spacious" at right.

### Implementation for User Story 2

- [x] T009 [US2] Add landmark labels ("Dense", "Default", "Spacious") positioned above the zoom step dots at left/center/right in `src/modules/settings/components/AppearanceSection/ZoomLevelPicker.tsx`

**Checkpoint**: Zoom control is self-explanatory with visual landmarks.

---

## Phase 4: User Story 3 - Reset to Default (Priority: P3)

**Goal**: Add a "Reset" button that returns zoom to 100%

**Independent Test**: Set non-default zoom, click Reset, verify zoom returns to 100%. Verify Reset is hidden/de-emphasized at 100%.

### Implementation for User Story 3

- [x] T010 [US3] Add "Reset" button below the zoom control that calls `setZoomLevel(100)`, conditionally hidden or disabled when already at 100% in `src/modules/settings/components/AppearanceSection/ZoomLevelPicker.tsx`

**Checkpoint**: All user stories complete and independently functional.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Verification and cleanup across all stories

- [x] T011 Run `pnpm check` (typecheck + lint + format) and fix any issues
- [x] T012 Manually verify all 7 zoom levels in both dark and light themes
- [x] T013 Verify library and workshop card grids respond correctly to zoom changes at extreme values (70%, 130%)
- [x] T014 Test migration: manually set localStorage to old density format, reload, verify correct zoom level applied

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: No dependencies — start immediately
  - T001 and T002 can run in parallel (different files)
  - T003 depends on T001 (needs new hook names)
- **User Story 1 (Phase 2)**: Depends on Phase 1 completion
  - T004 through T008 are mostly sequential (same file for T004, then integration)
  - T008 (tests) can run in parallel with T005–T007
- **User Story 2 (Phase 3)**: Depends on T004 (modifies same component)
- **User Story 3 (Phase 4)**: Depends on T004 (modifies same component)
- **Polish (Phase 5)**: Depends on all user stories complete

### Parallel Opportunities

```
Phase 1 parallel:
  T001 (displayStore.ts) || T002 (global.css)
  then T003 (depends on T001)

Phase 2 parallel:
  T008 (tests) || T005 + T006 + T007 (integration cleanup)
  (T004 must complete first)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Foundational (T001–T003)
2. Complete Phase 2: User Story 1 (T004–T008)
3. **STOP and VALIDATE**: Test zoom selection, persistence, and migration
4. App is fully usable at this point

### Incremental Delivery

1. Foundation → Zoom works internally
2. Add User Story 1 → Zoom selectable via UI (MVP!)
3. Add User Story 2 → Landmark labels for orientation
4. Add User Story 3 → Reset button for convenience
5. Polish → Cross-theme verification, edge case testing

---

## Notes

- All changes are frontend-only — no Rust/backend modifications needed
- US2 and US3 both modify `ZoomLevelPicker.tsx` so they must be sequential
- The old `DensityPicker.tsx` is deleted in T006, not renamed, since the component is fully rewritten
- Migration handles the breaking localStorage schema change transparently
