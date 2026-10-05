# Data Model: Mod Layer Selection

**Feature Branch**: `004-mod-layer-selection` | **Date**: 2026-03-18

## Entities

### Profile (Extended)

The existing `Profile` entity gains a new field for per-mod layer configuration.

| Field            | Type                                           | Description                                              |
| ---------------- | ---------------------------------------------- | -------------------------------------------------------- |
| id               | String (UUID)                                  | Unique identifier                                        |
| name             | String                                         | User-friendly name                                       |
| slug             | ProfileSlug                                    | Slugified filesystem-safe name                           |
| enabled_mods     | Vec\<String\>                                  | Ordered list of enabled mod IDs (overlay priority)       |
| mod_order        | Vec\<String\>                                  | Display order of all mods (enabled + disabled)           |
| **layer_states** | **HashMap\<String, HashMap\<String, bool\>\>** | **NEW: mod_id → (layer_name → enabled). Missing = true** |
| created_at       | DateTime\<Utc\>                                | Creation timestamp                                       |
| last_used        | DateTime\<Utc\>                                | Last switched-to timestamp                               |

**Serialized key**: `layerStates` (camelCase, per `#[serde(rename_all = "camelCase")]`)

**Default behavior**: `#[serde(default)]` ensures backward compatibility — existing `library.json` files without `layerStates` deserialize to an empty map, meaning all layers default to enabled.

### ModLayer (Unchanged Structure, Changed Semantics)

The existing `ModLayer` struct is unchanged. The `enabled` field is currently hardcoded to `true` — it will now be populated from the active profile's `layer_states`.

| Field    | Type   | Description                                         |
| -------- | ------ | --------------------------------------------------- |
| name     | String | Layer name (matches mod archive layer names)        |
| priority | i32    | Layer priority (lower = applied first in overlay)   |
| enabled  | bool   | Whether this layer is active in the current profile |

### InstalledMod (Unchanged)

No structural changes. The `layers` field already contains `Vec<ModLayer>` — its `enabled` values will now reflect the active profile's configuration instead of being hardcoded.

### EnabledMod (Extended — in ltk_overlay crate)

The `EnabledMod` struct in the `ltk_overlay` crate gains an optional layer filter.

| Field              | Type                            | Description                                        |
| ------------------ | ------------------------------- | -------------------------------------------------- |
| id                 | String                          | Mod identifier                                     |
| content            | Box\<dyn ModContentProvider\>   | Content provider for reading mod data              |
| **enabled_layers** | **Option\<HashSet\<String\>\>** | **NEW: layer names to include. None = all layers** |

## Relationships

```
Profile 1──* LayerStates (per mod)
  │
  └── layerStates[mod_id] 1──* LayerState (per layer)
        │
        └── layer_name → enabled (bool)

InstalledMod 1──* ModLayer
  │
  └── ModLayer.enabled ← derived from Profile.layerStates
```

## State Transitions

### Layer Toggle

```
User toggles layer → set_mod_layers command
  → Profile.layer_states[mod_id][layer_name] = new_value
  → save library.json
  → invalidate overlay
  → frontend updates ModLayer.enabled optimistically
```

### Mod Enable with Layer Picker

```
User enables multi-layer mod → layer picker shown
  → User selects layers → confirm
  → toggle_mod(mod_id, true) + set_mod_layers(mod_id, states)
    OR: enable_mod_with_layers(mod_id, layer_states) (combined command)
  → Profile.enabled_mods += mod_id
  → Profile.layer_states[mod_id] = user_selections
  → save library.json
  → invalidate overlay
```

### Mod Re-install (Layer Cleanup)

```
Mod re-installed with new layer set
  → For each profile:
    → Remove layer_states entries for layers no longer in mod
    → Preserve entries for layers that still exist
    → New layers: absence from map → defaults to enabled
  → save library.json
```

### Profile Switch

```
User switches from Profile A → Profile B
  → Active profile changes
  → InstalledMod.layers re-derived from Profile B's layer_states
  → Frontend refetches mod list (cache invalidation)
  → UI reflects Profile B's layer configuration
```

## Validation Rules

- Layer names in `layer_states` MUST match layer names from the mod's `mod.config.json`.
- A mod ID in `layer_states` MUST correspond to an installed mod. Orphaned entries are cleaned during reconciliation.
- `layer_states` values are optional per mod — absence means all layers enabled.
- Individual layer entries are optional — absence means that layer is enabled.
- The `set_mod_layers` command MUST validate that the mod exists and is in the active profile's scope.

## Persistence Format

### library.json (Extended)

```json
{
  "mods": [
    {
      "id": "abc-123",
      "installedAt": "2026-03-18T10:00:00Z",
      "format": "modpkg"
    }
  ],
  "profiles": [
    {
      "id": "profile-001",
      "name": "Default",
      "slug": "default",
      "enabledMods": ["abc-123"],
      "modOrder": ["abc-123"],
      "layerStates": {
        "abc-123": {
          "base": true,
          "chroma-red": false,
          "chroma-blue": true
        }
      },
      "createdAt": "2026-03-18T10:00:00Z",
      "lastUsed": "2026-03-18T12:00:00Z"
    }
  ],
  "activeProfileId": "profile-001"
}
```

**Notes**:

- Only layers with explicitly set states need entries. Missing layers default to enabled.
- In practice, the map will only contain entries for layers the user has explicitly toggled off (and then back on). A fully-default mod has no entry in `layerStates`.
