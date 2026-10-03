# Quickstart: Zoom Level Slider

**Feature**: 009-zoom-level-slider
**Date**: 2026-03-29

## Overview

Replace the 3-option density picker with a 7-step zoom level control (70%–130%). This is a frontend-only change — no Rust/backend modifications needed.

## Files to Modify

### 1. `src/stores/displayStore.ts`

- Replace `Density` type with `ZoomLevel` type (union of literal numbers: 70 | 80 | 90 | 100 | 110 | 120 | 130)
- Rename `density` → `zoomLevel`, `setDensity` → `setZoomLevel` in store interface and implementation
- Change default from `"normal"` to `100`
- Add Zustand persist `version: 1` and `migrate` function to convert old density strings to zoom numbers
- Update exported hooks: `useZoomLevel()`, `useSetZoomLevel()`

### 2. `src/styles/global.css`

- Remove all three `[data-density="..."]` blocks (lines 182–234)
- Replace `:root` default density tokens with `calc()`-based definitions using `--zoom-scale: 1`
- Add a minimal set of `[data-zoom]` rules that set `--zoom-scale` per step, OR use a single approach where `__root.tsx` sets `--zoom-scale` directly as an inline style

### 3. `src/modules/settings/components/AppearanceSection/DensityPicker.tsx`

- Rename file to `ZoomLevelPicker.tsx`
- Replace RadioGroup.Card layout with horizontal dot-radio layout
- Add zoom steps: 70%, 80%, 90%, 100%, 110%, 120%, 130%
- Add landmark labels: "Dense" (left), "Default" (center at 100%), "Spacious" (right)
- Add "Reset" button (sets zoom to 100%)

### 4. `src/modules/settings/components/AppearanceSection/AppearanceSection.tsx`

- Update import from `DensityPicker` to `ZoomLevelPicker`
- Update JSX to render `<ZoomLevelPicker />`

### 5. `src/modules/settings/components/AppearanceSection/index.ts`

- Update export if DensityPicker was exported (it's internal, so likely just the file rename)

### 6. `src/routes/__root.tsx`

- Change `document.documentElement.dataset.density = density` to set `data-zoom` attribute
- Optionally set `--zoom-scale` as inline CSS variable: `document.documentElement.style.setProperty('--zoom-scale', String(zoomLevel / 100))`
- Update store hook from `useDensity` to `useZoomLevel`

### 7. `src/__tests__/stores/displayStore.test.ts`

- Update all tests to use `zoomLevel` instead of `density`
- Add migration test: verify old format `{ density: "compact" }` migrates to `{ zoomLevel: 70 }`
- Update persistence test assertions

## Implementation Order

1. **Store** (`displayStore.ts`) — foundation for everything else
2. **CSS** (`global.css`) — token scaling mechanism
3. **Root layout** (`__root.tsx`) — connect store to DOM
4. **Component** (`ZoomLevelPicker.tsx`) — new UI control
5. **Integration** (`AppearanceSection.tsx`, barrel exports) — wire it up
6. **Tests** (`displayStore.test.ts`) — verify behavior

## Verification

```bash
pnpm check          # typecheck + lint + format
pnpm tauri dev      # manual UI testing
```

Manual checks:

- Select each zoom level, verify spacing/icons scale
- Verify persistence across app restart
- Verify old density values migrate correctly (edit localStorage manually to test)
- Test in both dark and light themes
- Verify library/workshop card grids respond to zoom changes
