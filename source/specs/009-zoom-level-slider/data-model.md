# Data Model: Zoom Level Slider

**Feature**: 009-zoom-level-slider
**Date**: 2026-03-29

## Entities

### ZoomLevel

Represents the UI zoom percentage selected by the user.

| Field | Type   | Constraints                            | Description         |
| ----- | ------ | -------------------------------------- | ------------------- |
| value | number | One of: 70, 80, 90, 100, 110, 120, 130 | The zoom percentage |

**Default value**: 100

**Derived property**: `scale = value / 100` (e.g., 70 → 0.7, 130 → 1.3)

**Storage**: Persisted in localStorage under `ltk-display-prefs` key via Zustand persist middleware. Stored as `zoomLevel: number` (replaces old `density: string` field).

### DisplayPreferences (updated)

The persisted store shape changes from:

```
v0 (current):
{
  density: "compact" | "normal" | "spacious",
  reduceMotion: "system" | "on" | "off"
}
```

to:

```
v1 (new):
{
  zoomLevel: 70 | 80 | 90 | 100 | 110 | 120 | 130,
  reduceMotion: "system" | "on" | "off"
}
```

### Migration Map

| Old Value  | New Value |
| ---------- | --------- |
| "compact"  | 70        |
| "normal"   | 80        |
| "spacious" | 100       |

## State Transitions

```
User clicks zoom step → store.setZoomLevel(value)
  → Zustand updates state
  → useEffect in __root.tsx syncs to document.documentElement.dataset.zoom
  → CSS responds via [data-zoom="..."] or --zoom-scale variable
  → All spacing/icon tokens recalculate
  → UI reflows instantly
```

## CSS Token Scaling

All spacing and icon tokens are computed from their base value multiplied by `--zoom-scale`:

| Token Pattern  | Base Formula    | Example at 70%        | Example at 130%       |
| -------------- | --------------- | --------------------- | --------------------- |
| `--space-NNN`  | N × 6px × scale | 1 × 6px × 0.7 = 4.2px | 1 × 6px × 1.3 = 7.8px |
| `--icon-NNN`   | base × scale    | 16px × 0.7 = 11.2px   | 16px × 1.3 = 20.8px   |
| `--card-min-w` | 240px × scale   | 168px                 | 312px                 |
| `--card-max-w` | 320px × scale   | 224px                 | 416px                 |

Tokens NOT affected: `--radius-*`, `--shadow-*`, `--z-*`, colors, font sizes, durations, easings.
