# Research: Improved Project Creation Dialog UX

**Feature Branch**: `005-project-creation-ux`
**Date**: 2026-03-19

## R1: Slug Derivation Strategy

**Decision**: Implement slug derivation as a pure frontend utility function in the dialog component. No backend changes needed — the existing `is_valid_project_name()` Rust validation remains the source of truth.

**Rationale**: The backend already validates `[a-z0-9-]+` with no leading/trailing hyphens. The frontend just needs a `toSlug(displayName)` function that:

1. Converts to lowercase
2. Replaces spaces and non-alphanumeric characters with hyphens
3. Collapses consecutive hyphens
4. Strips leading/trailing hyphens
5. Strips non-ASCII characters (Unicode → empty, since backend rejects them)

**Alternatives considered**:

- Backend slug generation endpoint: Unnecessary network round-trip for a deterministic string transform.
- Shared Rust+TS slug logic via WASM: Over-engineered for a simple string function.

## R2: Slug Auto-Sync Lock Mechanism

**Decision**: Use a `slugManuallyEdited` boolean ref (not form state) to track whether the user has manually typed in the slug field. Once set to `true`, the display name `onChange` no longer updates the slug.

**Rationale**: TanStack Form's `onChange` on the display name field can call `form.setFieldValue("name", toSlug(value))` when `slugManuallyEdited` is false. The slug field's `onFocus` or first `onChange` (when value differs from auto-generated) sets the lock. A ref avoids unnecessary re-renders.

**Alternatives considered**:

- Derived/computed field: TanStack Form doesn't natively support derived fields that can be overridden.
- Two separate state systems: Would fragment form state and make validation harder.

## R3: Thumbnail Preview in Dialog (Before Project Exists)

**Decision**: Use Tauri's `open()` file dialog to get the file path, then use `convertFileSrc()` to create an asset URL for the `<img>` preview. Store the selected file path in component state (not form state). After project creation, call `api.setProjectThumbnail(projectPath, imagePath)` as a follow-up mutation.

**Rationale**: This reuses the exact pattern from `ThumbnailSection.tsx`. The file path from `open()` is a native path that `convertFileSrc()` can render. No need to read the file into memory or create blob URLs. The two-step flow (create project → set thumbnail) keeps the backend simple and reuses existing infrastructure.

**Alternatives considered**:

- Adding thumbnail to `CreateProjectArgs`: Would require backend changes and couples creation with thumbnail. The existing `set_thumbnail` handles WebP conversion, atomic writes, and config updates — duplicating this in `create_project` is wasteful.
- Base64 preview: Unnecessary when Tauri asset protocol can serve local files.

## R4: Author Name Persistence

**Decision**: Add a `lastAuthorName` field to the `workshopDialogs` Zustand store with `persist` middleware using `localStorage`. The dialog reads this as the default value for the author field and updates it on successful submission.

**Rationale**: Author name is client-side convenience state, not app configuration. Using Zustand persist is consistent with the codebase's state management boundaries (Zustand for client-only state, TanStack Query for server state). Adding it to the Rust `Settings` struct would require backend changes, settings migration, and a Tauri command round-trip for something that's purely a UI convenience.

**Alternatives considered**:

- Add `default_author` to Rust `Settings`: Over-engineered; requires backend changes for a frontend convenience feature.
- Raw `localStorage.getItem/setItem`: Works but bypasses the established Zustand pattern.

## R5: Dialog Layout Approach

**Decision**: Restructure the dialog to a two-column layout at larger sizes (or stacked on small dialogs): thumbnail area on the left/top, form fields on the right/bottom. Display name is the first and most prominent field with larger text. Slug is shown below it in a smaller, muted style with an edit toggle or expandable section.

**Rationale**: The thumbnail area gives the dialog visual personality and makes the creation experience feel more like "designing" a mod rather than filling out a form. The display name prominence matches the UX inversion (human name first, machine name derived).

**Alternatives considered**:

- Keep single-column with thumbnail at bottom: Thumbnail feels like an afterthought.
- Full-width thumbnail banner at top: Takes too much vertical space and pushes form fields down.

## R6: Existing Infrastructure Reuse

**Decision**: No new Tauri commands, no new Rust types, no backend changes. The entire feature is frontend-only, leveraging:

- `api.createWorkshopProject()` — existing project creation
- `api.setProjectThumbnail()` — existing thumbnail application
- `open()` from `@tauri-apps/plugin-dialog` — existing file picker
- `convertFileSrc()` — existing asset URL conversion
- `@/components` Dialog, Field, Button — existing UI components

**Rationale**: The backend already supports everything needed. The UX improvements are purely about how the frontend presents and orchestrates these existing capabilities.
