# Implementation Plan: Zoom Level Slider

**Branch**: `009-zoom-level-slider` | **Date**: 2026-03-29 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/009-zoom-level-slider/spec.md`

## Summary

Replace the current three-option density picker (compact/normal/spacious) with a Spotify-style zoom level control offering seven percentage steps (70%–130% in 10% increments). The zoom level scales spacing and icon size CSS tokens via a numeric scale factor derived from the percentage. Includes landmark labels, a reset button, and migration from the old density format.

## Technical Context

**Language/Version**: TypeScript (strict) + React 19, CSS (Tailwind v4)
**Primary Dependencies**: Zustand (client state + persistence), Tailwind CSS v4
**Storage**: localStorage via Zustand persist middleware (key: `ltk-display-prefs`)
**Testing**: Vitest (unit), manual UI verification
**Target Platform**: Tauri v2 desktop app (Windows, macOS, Linux)
**Project Type**: Desktop app (Tauri)
**Performance Goals**: Immediate visual feedback on zoom change (<16ms reflow)
**Constraints**: No new dependencies; CSS-only scaling via custom properties
**Scale/Scope**: 5 files modified, 1 new component, ~200 LOC delta

## Constitution Check

_GATE: Must pass before Phase 0 research. Re-check after Phase 1 design._

| Principle                         | Status | Notes                                                                                |
| --------------------------------- | ------ | ------------------------------------------------------------------------------------ |
| I. Code Quality & Maintainability | PASS   | Imports from barrel exports, no ternaries in JSX, state consumed via hooks not props |
| II. Type Safety & Error Handling  | PASS   | New `ZoomLevel` type replaces `Density` type; no Tauri commands involved             |
| III. Testing Standards            | PASS   | Existing displayStore tests will be updated; `pnpm check` required before merge      |
| IV. User Experience Consistency   | PASS   | Uses `@/components` primitives; tested in both themes; uses design tokens            |
| V. Performance Requirements       | PASS   | CSS-only scaling, no extra renders; Zustand selectors used                           |
| Technology Constraints            | PASS   | No new dependencies added                                                            |
| Development Workflow              | PASS   | Feature branch targets main; conventional commits                                    |

## Project Structure

### Documentation (this feature)

```text
specs/009-zoom-level-slider/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (affected files)

```text
src/
├── stores/
│   └── displayStore.ts          # MODIFY: Replace Density type with ZoomLevel
├── styles/
│   └── global.css               # MODIFY: Replace data-density blocks with data-zoom
├── modules/settings/components/AppearanceSection/
│   ├── DensityPicker.tsx         # RENAME/REPLACE → ZoomLevelPicker.tsx
│   ├── AppearanceSection.tsx     # MODIFY: Import ZoomLevelPicker
│   └── index.ts                 # MODIFY: Export ZoomLevelPicker
├── routes/
│   └── __root.tsx               # MODIFY: Apply data-zoom instead of data-density
└── __tests__/stores/
    └── displayStore.test.ts     # MODIFY: Update tests for zoom level
```

## Complexity Tracking

No constitution violations. No complexity justifications needed.
