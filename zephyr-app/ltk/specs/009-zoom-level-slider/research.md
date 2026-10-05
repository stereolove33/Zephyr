# Research: Zoom Level Slider

**Feature**: 009-zoom-level-slider
**Date**: 2026-03-29

## R1: Zoom Level Scale Mapping

**Decision**: Use a percentage-to-decimal mapping where the percentage directly represents the scale factor applied to spacing and icon tokens. 100% = 1.0 scale (current "spacious" baseline).

**Rationale**: The current system uses three hardcoded scale factors (0.6, 0.75, 1.0). Moving to percentage-based values (0.7, 0.8, 0.9, 1.0, 1.1, 1.2, 1.3) provides finer granularity while keeping the math simple: `percentage / 100 = scale factor`. The base token values in `:root` represent the 100% (unscaled) values.

**Alternatives considered**:

- Continuous slider (rejected: Spotify uses discrete steps, harder to land on exact values)
- Keeping three named presets with sub-steps (rejected: adds complexity, percentage is self-explanatory)

## R2: CSS Implementation Strategy

**Decision**: Replace `[data-density="..."]` CSS blocks with a single `[data-zoom]` block that uses `calc()` with a `--zoom-scale` custom property, eliminating per-step token repetition.

**Rationale**: The current approach duplicates all 19 spacing/icon token overrides for each density level. With 7 zoom steps, this would mean 133 lines of redundant CSS. Instead, setting a single `--zoom-scale` variable and using `calc(N * 6px * var(--zoom-scale))` in `:root` definitions eliminates duplication entirely. The `data-zoom` attribute value directly maps to the scale (e.g., `data-zoom="100"` → `--zoom-scale: 1.0`).

**Alternatives considered**:

- Keep per-step CSS blocks (rejected: 7 blocks × 19 tokens = excessive repetition)
- JavaScript-based scaling (rejected: CSS-only is more performant, no reflow overhead)

## R3: Migration from Old Density Format

**Decision**: Handle migration in the Zustand persist `migrate` function. Map `"compact"` → `70`, `"normal"` → `80`, `"spacious"` → `100`.

**Rationale**: Zustand's persist middleware supports a `version` + `migrate` callback. Bumping the store version from unversioned (implicitly 0) to 1 and providing a migration function is the idiomatic approach. The migration runs once on first load and the store is re-persisted with the new format.

**Alternatives considered**:

- Runtime type checking on every read (rejected: migration is cleaner and runs once)
- Ignoring old values and resetting to default (rejected: spec requires preserving user preference)

## R4: Card Width Tokens

**Decision**: Scale `--card-min-w` and `--card-max-w` proportionally with the zoom level, using the same `--zoom-scale` factor.

**Rationale**: The current system adjusts card widths per density. The library and workshop grids use these tokens via CSS `minmax()`. Scaling them with the zoom factor maintains the proportional relationship between card size and spacing.

**Alternatives considered**:

- Fixed card widths regardless of zoom (rejected: would look disproportionate at extreme zoom levels)
- Separate card width control (rejected: over-engineering for this scope)

## R5: UI Component Design

**Decision**: Build a custom `ZoomLevelPicker` component using radio-style selectable dots in a horizontal layout, with three landmark labels above ("Dense", "Default", "Spacious").

**Rationale**: The Spotify reference uses a horizontal row of radio circles with percentage labels below each. Landmark labels above provide context. A "Reset" button at the bottom-right returns to 100%. This matches the existing app design language using `@/components` primitives and Tailwind utility classes.

**Alternatives considered**:

- Reusing RadioGroup.Card (rejected: too large for 7 options; dot-style radio is more compact and matches Spotify)
- Using a range/slider input (rejected: spec explicitly requires discrete steps, not continuous)
