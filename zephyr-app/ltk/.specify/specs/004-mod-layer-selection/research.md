# Research: Mod Layer Selection

**Feature Branch**: `004-mod-layer-selection` | **Date**: 2026-03-18

## R1: Overlay Builder Layer Filtering Support

**Decision**: The `ltk_overlay` crate's `EnabledMod` struct must be extended with an optional layer filter, and the metadata collection phase must respect it.

**Rationale**: Currently, `EnabledMod` only carries `id` and `content` (a `Box<dyn ModContentProvider>`). The `collect_single_mod_metadata` function in `builder/metadata.rs` iterates **all** layers unconditionally — there is no mechanism to skip disabled layers. The `ModLayer.enabled` field exists in ltk-manager but is hardcoded to `true` and never consulted during overlay building.

**Alternatives considered**:

- **Filter at content provider level**: Wrap each `ModContentProvider` in a filtering adapter that returns empty for disabled layers. Rejected — adds complexity to every provider and breaks the separation between data access and policy.
- **Post-collection filtering**: Collect all layers, then discard overrides from disabled layers. Rejected — wastes I/O reading content that will be thrown away.
- **Pre-collection filtering (chosen)**: Add `enabled_layers: Option<HashSet<String>>` to `EnabledMod`. When `Some`, the builder skips layers not in the set during `collect_single_mod_metadata`. When `None`, all layers are processed (backward-compatible default).

**Key files**:

- `league-mod/crates/ltk_overlay/src/builder/mod.rs` — `EnabledMod` struct (lines 73-88)
- `league-mod/crates/ltk_overlay/src/builder/metadata.rs` — `collect_single_mod_metadata` (lines 24-102)

---

## R2: Layer State Persistence Strategy

**Decision**: Add a `layer_states: HashMap<String, HashMap<String, bool>>` field to the `Profile` struct, stored in `library.json`.

**Rationale**: Layer configuration is profile-scoped (different profiles may want different layers active for the same mod). The `Profile` struct already owns `enabled_mods` and `mod_order`, making it the natural location for layer state. A `HashMap<String, HashMap<String, bool>>` maps `mod_id → (layer_name → enabled)`. Missing entries default to `true` (all enabled), providing backward compatibility with existing `library.json` files.

**Alternatives considered**:

- **Separate `layer_config.json` per profile**: Rejected — adds file I/O complexity for no benefit; `library.json` is already loaded/saved atomically.
- **Store in `LibraryModEntry`**: Rejected — layer states are profile-scoped, not global. Different profiles need different states for the same mod.
- **Store as a flat `Vec<(String, String, bool)>`**: Rejected — less ergonomic for lookup and update operations.

**Key files**:

- `src-tauri/src/mods/mod.rs` — `Profile` struct (lines 178-198)
- `src-tauri/src/mods/mod.rs` — `load_library_index` / `save_library_index` (lines 356-382)

---

## R3: Backend Command Design

**Decision**: Add a new dedicated Tauri command `set_mod_layers` that accepts a mod ID and a map of layer name → enabled state, rather than extending the existing `toggle_mod` command.

**Rationale**: The existing `toggle_mod` command has a simple `(mod_id, enabled)` signature that maps cleanly to a single boolean toggle. Overloading it with optional layer state would complicate its semantics. A dedicated command makes the API surface explicit and follows the existing pattern of one command per domain action.

**Alternatives considered**:

- **Extend `toggle_mod` with optional `layer_states` parameter**: Rejected — muddies the semantic meaning of "toggle mod" and complicates optimistic update logic on the frontend.
- **Per-layer `toggle_layer` command**: Rejected — toggling multiple layers requires multiple IPC calls, adding latency and complicating atomicity.
- **Batch `set_mod_layers` (chosen)**: Accepts the full layer state map for a mod in one call. Atomic (single index mutation), efficient (one IPC round-trip), and clear in intent.

**Command signature**:

```rust
pub fn set_mod_layers(
    &self,
    settings: &Settings,
    mod_id: &str,
    layer_states: HashMap<String, bool>,
) -> AppResult<()>
```

---

## R4: Frontend Layer UI Pattern

**Decision**: Use a combination of inline layer expansion on mod cards (for quick access) and enhanced layer controls in the existing ModDetailsDialog (for full management). The layer picker on enable uses a Popover anchored to the toggle switch.

**Rationale**: The existing ModDetailsDialog already displays layers read-only (lines 161-185 of `ModDetailsDialog.tsx`). Adding toggle controls there is low-effort. For the library view, a lightweight inline expansion or popover avoids forcing users into a dialog just to toggle a layer — satisfying the "2 interactions" success criterion. The Popover component is already available and used in the codebase.

**Alternatives considered**:

- **Dialog-only approach**: Rejected — requires opening a dialog to toggle any layer, adding friction.
- **Inline card expansion**: Considered for the primary layer toggle surface. Shows layers directly below the mod card when expanded. Works well for list view but may be awkward in grid view.
- **Popover on mod card (chosen for grid view)**: A small popover triggered by a layers button/icon on the card. Compact, doesn't disrupt layout, leverages existing Popover component.
- **Layer picker on enable**: Popover anchored near the toggle switch, shown when enabling a multi-layer mod. Dismissed by confirm/cancel or clicking outside.

**Key components**:

- Existing: `Popover`, `Switch`, `Dialog` from `@/components`
- New: `LayerPopover` (reusable layer toggle list), `LayerPickerPopover` (on-enable variant)

---

## R5: Overlay Integration — Passing Layer States

**Decision**: Modify `get_enabled_mods_for_overlay()` in `library.rs` to read layer states from the active profile and pass them as `enabled_layers` on each `EnabledMod`.

**Rationale**: This is the single integration point between the profile's layer configuration and the overlay builder. Currently, `get_enabled_mods_for_overlay()` creates `EnabledMod` structs with no layer information. After the change, it will read the profile's `layer_states` for each mod and convert them to `Option<HashSet<String>>` (layer names where `enabled == true`). If no layer states exist for a mod, `None` is passed (all layers active — backward-compatible).

**Key files**:

- `src-tauri/src/mods/library.rs` — `get_enabled_mods_for_overlay()` (lines 285-350)
- `src-tauri/src/overlay/mod.rs` — `ensure_overlay()` (lines 36-132)

---

## R6: Layer State Cleanup on Re-install

**Decision**: During mod installation (`install_single_mod_to_index`), after extracting new metadata, reconcile layer states in all profiles that reference the mod: remove entries for layers that no longer exist, preserve entries for layers that still exist, and let new layers default to enabled (via absence from the map).

**Rationale**: When a mod is updated (uninstall + reinstall or overwrite), its layer set may change. Stale layer state entries for removed layers would be harmless (ignored) but could cause confusion if inspected. Proactive cleanup keeps the data clean. New layers automatically default to enabled because missing keys default to `true`.

**Key files**:

- `src-tauri/src/mods/library.rs` — `install_single_mod_to_index()` (handles installation)
- `src-tauri/src/mods/mod.rs` — `reconcile_library_index()` (existing reconciliation logic)
