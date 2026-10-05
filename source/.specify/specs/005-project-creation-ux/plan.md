# Implementation Plan: Improved Project Creation Dialog UX

**Branch**: `005-project-creation-ux` | **Date**: 2026-03-19 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-project-creation-ux/spec.md`

## Summary

Overhaul the "New Project" dialog to lead with the display name (auto-deriving the project slug), add optional thumbnail selection, persist the author name across sessions, and refresh the visual layout. All changes are frontend-only — no new Tauri commands or backend modifications are required.

## Technical Context

**Language/Version**: TypeScript (strict), React 19+, Rust (stable) — no Rust changes
**Primary Dependencies**: TanStack Form + Zod, Zustand (with persist), `@tauri-apps/plugin-dialog`, `@base-ui-components/react` (via `@/components` wrappers)
**Storage**: localStorage (author name persistence via Zustand persist)
**Testing**: Manual verification through UI (per constitution III), `pnpm check` for type/lint/format
**Target Platform**: Tauri v2 desktop app (Windows, macOS, Linux)
**Project Type**: Desktop app (Tauri)
**Performance Goals**: Dialog interaction must remain instant; no perceptible delay on slug derivation or thumbnail preview
**Constraints**: No new backend commands; reuse existing `create_workshop_project` + `set_project_thumbnail`
**Scale/Scope**: 2 files modified (`NewProjectDialog.tsx`, `workshopDialogs.ts`)

## Constitution Check

_GATE: Must pass before Phase 0 research. Re-check after Phase 1 design._

| Principle              | Status | Notes                                                                                                        |
| ---------------------- | ------ | ------------------------------------------------------------------------------------------------------------ |
| I. Code Quality        | PASS   | Imports from barrel exports, no ternaries in JSX, no trivial comments, no base-ui direct imports             |
| II. Type Safety        | PASS   | No new Tauri commands; existing `CreateProjectArgs` and `WorkshopProject` types unchanged                    |
| III. Testing           | PASS   | Test scenarios documented in spec; manual verification covers success + error paths                          |
| IV. UX Consistency     | PASS   | Uses `@/components` Dialog, Field, Button; toast for errors; loading states for async ops                    |
| V. Performance         | PASS   | Slug derivation is synchronous string transform; thumbnail preview uses Tauri asset protocol (no file reads) |
| Technology Constraints | PASS   | TanStack Form + Zod, Zustand persist, Tauri dialog — all within approved stack                               |
| Development Workflow   | PASS   | `pnpm check` + `cargo clippy` before PR; conventional commits                                                |

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/005-project-creation-ux/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── ipc-commands.md  # Phase 1 output
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── modules/workshop/components/
│   └── NewProjectDialog.tsx       # MODIFY: Major rewrite (layout, slug derivation, thumbnail, form reorder)
├── stores/
│   └── workshopDialogs.ts         # MODIFY: Add lastAuthorName with persist middleware
└── (no new files)
```

**Structure Decision**: No new files needed. The dialog component is rewritten in-place, and the Zustand store gains one persisted field. This keeps the change surface minimal.

## Design Decisions

### D1: Slug Derivation — Frontend-Only Utility

A `toSlug()` function converts the display name to a valid project slug in real-time. It normalizes Unicode (NFKD decomposition), strips diacritics and non-ASCII, lowercases, replaces non-alphanumeric runs with hyphens, and trims. This runs synchronously on every keystroke — no debouncing needed for a simple string transform.

### D2: Manual Override Lock — useRef Boolean

A `useRef<boolean>(false)` tracks whether the user has manually edited the slug. The display name's `onChange` only updates the slug when this ref is `false`. The slug field's `onChange` sets it to `true` if the new value differs from `toSlug(currentDisplayName)`. The ref resets when the dialog opens.

### D3: Thumbnail Preview — Tauri Asset Protocol

The file picker returns a native path. `convertFileSrc()` converts it to a `asset://` URL that the WebView can render as an `<img>`. No file reading, no blob URLs, no base64. After project creation, the path is passed to `set_project_thumbnail` which handles WebP conversion.

### D4: Author Persistence — Zustand Persist to localStorage

The `workshopDialogs` store gains `lastAuthorName: string` with `persist` middleware. Only `lastAuthorName` is persisted (not `newProjectOpen`). The dialog reads it as the default author value. On successful creation, it's updated.

### D5: Two-Step Submission — Create Then Thumbnail

The form submits in two steps: (1) create project, (2) if thumbnail selected, apply it. If step 2 fails, the project still exists — a toast notifies the user that thumbnail setting failed but the project was created. This avoids transactional complexity and reuses existing backend flows.

### D6: Dialog Layout — Thumbnail Left, Form Right

The dialog uses a wider layout (`lg` size instead of default `md`) with the thumbnail area on the left and form fields on the right. Display name is the first field with larger/bolder styling. The slug appears below it in a compact, muted style. Author and description follow. This gives the dialog visual personality while keeping the form scannable.
