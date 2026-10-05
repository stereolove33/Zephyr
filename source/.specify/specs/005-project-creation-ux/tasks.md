# Tasks: Improved Project Creation Dialog UX

**Input**: Design documents from `/specs/005-project-creation-ux/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not requested — manual UI verification per constitution III.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Store changes and utility function that all user stories depend on

- [x] T001 [P] Add `lastAuthorName` field with `persist` middleware to the Zustand store in `src/stores/workshopDialogs.ts`. Add persist middleware wrapping the existing `create` call, using storage key `"workshop-dialogs"` and `partialize` to only persist `lastAuthorName` (not `newProjectOpen`). Default `lastAuthorName` to `""`. Add a `setLastAuthorName` action.
- [x] T002 [P] Add a `toSlug` utility function to `src/modules/workshop/components/NewProjectDialog.tsx` (defined above the component or in a local scope). Implementation: normalize NFKD, strip diacritics (`/[\u0300-\u036f]/g`), strip non-ASCII (`/[^\x00-\x7F]/g`), lowercase, replace non-alphanumeric runs with hyphens (`/[^a-z0-9]+/g`), trim leading/trailing hyphens (`/^-+|-+$/g`). See `quickstart.md` for exact implementation.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Restructure the form schema and field order — MUST complete before user story implementation

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Restructure the Zod schema and form field order in `src/modules/workshop/components/NewProjectDialog.tsx`. Move `displayName` to the first field position in the schema. Keep `name` (slug) as the second field with existing validation (`^[a-z0-9-]+$`, no leading/trailing hyphens). Remove the existing auto-generation of `displayName` from `name` (the current `titleCase` logic). Update the form's `defaultValues` to read `lastAuthorName` from the Zustand store for the `authorName` field. Ensure the form still submits the same `CreateProjectArgs` shape to the mutation.

**Checkpoint**: Form schema restructured — display name is first, slug is second, author defaults to last-used value

---

## Phase 3: User Story 1 - Display Name Drives Slug Generation (Priority: P1) MVP

**Goal**: Typing a display name auto-generates the project slug in real-time. Manual edits to the slug lock it from further auto-updates.

**Independent Test**: Open dialog → type "Cool Champion Skin" → verify slug shows `cool-champion-skin`. Manually edit slug → change display name → verify slug stays manually set.

### Implementation for User Story 1

- [x] T004 [US1] Add a `slugManuallyEdited` ref (`useRef<boolean>(false)`) in `src/modules/workshop/components/NewProjectDialog.tsx`. Reset it to `false` when the dialog opens (in the effect or callback that handles dialog open state). This ref tracks whether the user has manually typed in the slug field.
- [x] T005 [US1] Wire the display name field's `onChange` (via TanStack Form's `validators.onChange` or a `listeners.onChange` callback) to call `form.setFieldValue("name", toSlug(value))` when `slugManuallyEdited.current` is `false` in `src/modules/workshop/components/NewProjectDialog.tsx`. This makes the slug update in real-time as the user types the display name.
- [x] T006 [US1] Wire the slug (`name`) field's `onChange` to detect manual edits in `src/modules/workshop/components/NewProjectDialog.tsx`. When the user changes the slug value and it differs from `toSlug(currentDisplayName)`, set `slugManuallyEdited.current = true`. This locks the slug from further auto-updates.
- [x] T007 [US1] Update the `onSubmit` handler in `src/modules/workshop/components/NewProjectDialog.tsx` to call `setLastAuthorName(values.authorName)` from the Zustand store after successful project creation. This persists the author name for the next dialog session.

**Checkpoint**: Display name drives slug generation with manual override lock. Author name persists across sessions. Core UX inversion is complete.

---

## Phase 4: User Story 2 - Thumbnail Selection During Creation (Priority: P2)

**Goal**: Users can optionally select a thumbnail image directly in the creation dialog, with a live preview. The thumbnail is applied after project creation using the existing backend flow.

**Independent Test**: Open dialog → click thumbnail area → select image → verify preview appears at 16:9. Submit → verify project has thumbnail set. Also test: submit without thumbnail → project created without error.

### Implementation for User Story 2

- [x] T008 [US2] Add `selectedThumbnailPath` state (`useState<string | null>(null)`) to `src/modules/workshop/components/NewProjectDialog.tsx`. Reset to `null` when dialog closes. Import `open` from `@tauri-apps/plugin-dialog` and `convertFileSrc` from `@tauri-apps/api/core`.
- [x] T009 [US2] Add a thumbnail picker area to the dialog JSX in `src/modules/workshop/components/NewProjectDialog.tsx`. When no thumbnail is selected, show a clickable placeholder (dashed border, camera/image icon, "Add Thumbnail" text) with 16:9 aspect ratio. On click, call `open()` with image filters (`["webp", "png", "jpg", "jpeg", "gif", "bmp", "tiff", "ico"]`). On file selection, set `selectedThumbnailPath`.
- [x] T010 [US2] Add thumbnail preview rendering in `src/modules/workshop/components/NewProjectDialog.tsx`. When `selectedThumbnailPath` is set, render `<img src={convertFileSrc(selectedThumbnailPath)} />` with `object-fit: cover` at 16:9 aspect ratio. Add a remove button (small X or "Remove" action) that sets `selectedThumbnailPath` back to `null`.
- [x] T011 [US2] Update the `onSubmit` handler in `src/modules/workshop/components/NewProjectDialog.tsx` to call `api.setProjectThumbnail(project.path, selectedThumbnailPath)` after successful `createWorkshopProject` when `selectedThumbnailPath` is not null. Import the existing `useSetProjectThumbnail` mutation hook. Handle partial failure: if thumbnail setting fails, show a toast error but don't fail the overall creation (the project still exists).

**Checkpoint**: Thumbnail can be selected, previewed, and applied during project creation. Projects without thumbnails still work.

---

## Phase 5: User Story 3 - Refreshed Dialog Layout and Feel (Priority: P3)

**Goal**: The dialog layout is visually refreshed — display name is prominent, slug is secondary, thumbnail area is visually integrated. The overall feel is polished and guides the creator naturally.

**Independent Test**: Open dialog → verify display name is the first and most prominent field. Verify slug appears smaller/muted below it. Verify thumbnail area is visually integrated (not an afterthought).

### Implementation for User Story 3

- [x] T012 [US3] Change the Dialog size from default (`md`) to `lg` in `src/modules/workshop/components/NewProjectDialog.tsx` to accommodate the two-column layout. Update the dialog body to use a flex/grid layout: thumbnail area on the left (~40% width), form fields on the right (~60% width).
- [x] T013 [US3] Style the display name field as the primary input in `src/modules/workshop/components/NewProjectDialog.tsx`. Use larger text (`text-lg` or `text-xl`), make it the first field in the form, and set `autoFocus` so it receives focus when the dialog opens.
- [x] T014 [US3] Style the slug field as secondary/derived information in `src/modules/workshop/components/NewProjectDialog.tsx`. Use smaller, muted text (`text-sm text-surface-400`). Show a label like "project-slug" or the folder path context. Keep it editable but visually de-emphasized compared to the display name.
- [x] T015 [US3] Polish the thumbnail placeholder and preview styling in `src/modules/workshop/components/NewProjectDialog.tsx`. Ensure the placeholder has a subtle border (`border-dashed border-surface-700`), centered icon and text, and smooth hover state. Ensure the preview image fills the area with rounded corners matching the dialog's design language.

**Checkpoint**: Dialog looks polished with clear visual hierarchy. Display name is prominent, slug is secondary, thumbnail is integrated.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, validation, and final quality checks

- [x] T016 Verify edge case handling in `src/modules/workshop/components/NewProjectDialog.tsx`: (1) empty display name produces empty slug and blocks submission, (2) display name with only special characters (e.g., "!!!") produces empty slug and blocks submission, (3) clearing display name after auto-generation clears the slug (when not manually edited), (4) very long display names produce valid truncated slugs.
- [x] T017 Run `pnpm check` (typecheck + lint + format) and fix any issues across `src/modules/workshop/components/NewProjectDialog.tsx` and `src/stores/workshopDialogs.ts`.
- [x] T018 Manual UI verification: test both dark and light themes per constitution IV. Verify all dialog states (empty, filled, with thumbnail, without thumbnail, error states) render correctly in both themes.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — T001 and T002 can start immediately and in parallel
- **Foundational (Phase 2)**: Depends on T001 (store changes) and T002 (toSlug function)
- **US1 (Phase 3)**: Depends on Phase 2 (restructured form schema)
- **US2 (Phase 4)**: Depends on Phase 2 (restructured form). Can run in parallel with US1 if tasks are carefully coordinated, but since both modify the same file, sequential is safer.
- **US3 (Phase 5)**: Depends on US1 + US2 completion (layout depends on all elements being present)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 only. No dependency on other stories. **MVP — can be shipped alone.**
- **User Story 2 (P2)**: Depends on Phase 2 only. Independent of US1 in logic, but modifies the same file — execute after US1 to avoid merge conflicts.
- **User Story 3 (P3)**: Depends on US1 + US2. Layout polish requires all functional elements to be in place.

### Within Each User Story

- Tasks within each story are sequential (same file: `NewProjectDialog.tsx`)
- Exception: T001 and T002 (Phase 1) are in different files and can be parallel

### Parallel Opportunities

- **T001 + T002**: Different files (`workshopDialogs.ts` vs `NewProjectDialog.tsx`), can run in parallel
- **US1 + US2 could theoretically parallel** but both modify `NewProjectDialog.tsx` — sequential recommended
- **T017 + T018**: Lint check and manual testing can run in parallel

---

## Parallel Example: Phase 1 Setup

```text
# These two tasks modify different files and can run simultaneously:
Task T001: "Add lastAuthorName with persist middleware in src/stores/workshopDialogs.ts"
Task T002: "Add toSlug utility function in src/modules/workshop/components/NewProjectDialog.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001 + T002 in parallel)
2. Complete Phase 2: Foundational (T003)
3. Complete Phase 3: User Story 1 (T004–T007)
4. **STOP and VALIDATE**: Test slug derivation, manual override, author persistence
5. Can ship as-is — core UX improvement delivered

### Incremental Delivery

1. Phase 1 + Phase 2 → Foundation ready
2. Add User Story 1 (T004–T007) → Test → **MVP shipped**
3. Add User Story 2 (T008–T011) → Test thumbnail flow → Ship
4. Add User Story 3 (T012–T015) → Test layout → Ship
5. Phase 6 Polish (T016–T018) → Final validation → Ship

### Single Developer Path (Recommended)

Since all changes touch 2 files (primarily one), a single developer executing sequentially T001→T018 is the most efficient path. Estimated: 18 focused tasks, each small and well-scoped.

---

## Notes

- Almost all tasks modify `src/modules/workshop/components/NewProjectDialog.tsx` — parallelism within stories is limited
- T001 (`workshopDialogs.ts`) is the only task in a different file and can always run independently
- No backend changes required — `cargo clippy` check in T017 is a formality
- Each user story checkpoint is a valid ship point
- Commit after each phase completion for clean git history
