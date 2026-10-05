# Tauri Command Contracts: Mod Layer Selection

**Feature Branch**: `004-mod-layer-selection` | **Date**: 2026-03-18

## New Commands

### `set_mod_layers`

Set the enabled/disabled state of individual layers for a mod in the active profile.

**Rust signature**:

```rust
#[tauri::command]
pub async fn set_mod_layers(
    library: State<'_, ModLibraryState>,
    settings: State<'_, SettingsState>,
    mod_id: String,
    layer_states: HashMap<String, bool>,
) -> IpcResult<()>
```

**TypeScript binding**:

```typescript
setModLayers: (modId: string, layerStates: Record<string, boolean>) =>
  invokeResult<void>("set_mod_layers", { modId, layerStates });
```

**Request**:
| Parameter | Type | Required | Description |
| ------------- | ----------------------------- | -------- | ---------------------------------------- |
| mod_id | String | Yes | UUID of the installed mod |
| layer_states | HashMap\<String, bool\> | Yes | Layer name → enabled state |

**Response**: `IpcResult<()>` — success or error.

**Error codes**:
| Code | Condition |
| -------------- | -------------------------------------------- |
| ModNotFound | Mod ID does not exist in the library |
| InternalState | Active profile not found |
| MutexLockFailed | Failed to acquire index lock |

**Side effects**:

- Updates `Profile.layer_states[mod_id]` in `library.json`
- Invalidates overlay for active profile
- Emits cache invalidation (frontend refetches mod list)

**Idempotency**: Yes. Calling with the same states is a no-op (overlay still invalidated).

---

### `enable_mod_with_layers`

Enable a mod and set its initial layer configuration in a single atomic operation. Used by the layer picker on enable flow.

**Rust signature**:

```rust
#[tauri::command]
pub async fn enable_mod_with_layers(
    library: State<'_, ModLibraryState>,
    settings: State<'_, SettingsState>,
    mod_id: String,
    layer_states: HashMap<String, bool>,
) -> IpcResult<()>
```

**TypeScript binding**:

```typescript
enableModWithLayers: (modId: string, layerStates: Record<string, boolean>) =>
  invokeResult<void>("enable_mod_with_layers", { modId, layerStates });
```

**Request**:
| Parameter | Type | Required | Description |
| ------------- | ----------------------------- | -------- | ---------------------------------------- |
| mod_id | String | Yes | UUID of the mod to enable |
| layer_states | HashMap\<String, bool\> | Yes | Initial layer configuration |

**Response**: `IpcResult<()>` — success or error.

**Error codes**:
| Code | Condition |
| -------------- | -------------------------------------------- |
| ModNotFound | Mod ID does not exist in the library |
| InternalState | Active profile not found |

**Side effects**:

- Adds mod to `Profile.enabled_mods` (same insertion logic as `toggle_mod`)
- Sets `Profile.layer_states[mod_id]` to provided states
- Invalidates overlay
- Single index mutation (atomic)

**Rationale**: Combining enable + layer config in one command avoids a race condition between two separate mutations and ensures the overlay is only invalidated once.

## Modified Commands

### `toggle_mod` (Unchanged Signature)

No signature changes. When a mod is disabled via `toggle_mod(mod_id, false)`, its `layer_states` entry is **preserved** (not removed). This allows the user to re-enable the mod later and retain their layer configuration.

When a mod is enabled via `toggle_mod(mod_id, true)` for a **single-layer mod**, no layer state is set (defaults to all enabled). For multi-layer mods, the frontend should use `enable_mod_with_layers` instead.

## Modified Queries

### `get_installed_mods` (Changed Behavior)

**Existing signature** (unchanged):

```rust
pub fn get_installed_mods(&self, settings: &Settings) -> AppResult<Vec<InstalledMod>>
```

**Changed behavior**: The `ModLayer.enabled` field in each `InstalledMod` is now populated from the active profile's `layer_states` instead of being hardcoded to `true`. Missing entries default to `true`.

## Frontend Hooks

### `useSetModLayers` (New)

TanStack Query mutation hook for `set_mod_layers` command.

**Optimistic update pattern**:

```typescript
onMutate: ({ modId, layerStates }) => {
  // Snapshot and optimistically update InstalledMod.layers[].enabled
};
onError: (_, _, context) => {
  // Rollback to snapshot
};
onSettled: () => {
  // Invalidate mod list query
};
```

### `useEnableModWithLayers` (New)

TanStack Query mutation hook for `enable_mod_with_layers` command.

**Optimistic update pattern**:

```typescript
onMutate: ({ modId, layerStates }) => {
  // Snapshot and optimistically update mod.enabled + mod.layers[].enabled
};
```
