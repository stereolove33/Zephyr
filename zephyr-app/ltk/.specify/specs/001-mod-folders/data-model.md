# Data Model: Mod Library Folders

**Branch**: `001-mod-folders` | **Date**: 2026-03-26

## Entities

### LibraryFolder (NEW)

A named container for grouping mods at the library level.

| Field     | Type            | Description                                                                 |
| --------- | --------------- | --------------------------------------------------------------------------- |
| `id`      | `String` (UUID) | Unique identifier, generated on creation                                    |
| `name`    | `String`        | User-facing display name (not required to be unique)                        |
| `mod_ids` | `Vec<String>`   | Ordered list of mod IDs in this folder (determines internal priority order) |

**Constraints**:

- `id` is globally unique across all folders
- `mod_ids` contains only valid mod IDs that exist in `LibraryIndex.mods`
- A mod ID can appear in at most one folder's `mod_ids` (enforced by business logic)
- Empty `mod_ids` is valid (empty folder)

**Serialization**: `#[serde(rename_all = "camelCase")]`, `#[derive(TS)]` with `#[ts(export)]`

### LibraryIndex (MODIFIED)

Two new fields added to the existing struct.

| Field               | Type                   | Default          | Description                                                           |
| ------------------- | ---------------------- | ---------------- | --------------------------------------------------------------------- |
| `mods`              | `Vec<LibraryModEntry>` | —                | (existing) All installed mods                                         |
| `profiles`          | `Vec<Profile>`         | —                | (existing) All profiles                                               |
| `active_profile_id` | `String`               | —                | (existing) Active profile UUID                                        |
| **`folders`**       | `Vec<LibraryFolder>`   | Root folder only | **(NEW)** All folders (always includes root folder with ID `"root"`)  |
| **`folder_order`**  | `Vec<String>`          | `vec!["root"]`   | **(NEW)** Ordered list of folder IDs defining top-level display order |

Both new fields use `#[serde(default)]` for backward compatibility.

**Root folder**: A special folder with sentinel ID `"root"` always exists. It holds ungrouped mods, cannot be renamed or deleted, and is hidden from the user-facing folder list.

**Migration invariant**: On load, if no root folder exists, one is created with all existing mods from the active profile's `mod_order`.

### InstalledMod (MODIFIED)

One new optional field added to the frontend response model.

| Field           | Type             | Description                                                   |
| --------------- | ---------------- | ------------------------------------------------------------- |
| (all existing)  | —                | Unchanged                                                     |
| **`folder_id`** | `Option<String>` | **(NEW)** ID of the containing folder, or `None` if ungrouped |

Folder name is NOT denormalized into `InstalledMod` — the frontend resolves it at runtime from the folders query by ID.

### Profile (UNCHANGED)

No structural changes. `mod_order` and `enabled_mods` continue to represent the flat priority order and enabled subset. They are derived from `folder_order` + folder contents:

**Derivation rule**:

```
Profile.mod_order = flatten(folder_order):
  for each folder_id in folder_order:
    append all mod_ids from that folder in order
Profile.enabled_mods = Profile.mod_order filtered to enabled set (order preserved)
```

## Relationships

```
LibraryIndex
├── mods: Vec<LibraryModEntry>        (all mods, flat)
├── folders: Vec<LibraryFolder>       (all folders, including root "root")
│   └── mod_ids: Vec<String>          ──references──> LibraryModEntry.id
├── folder_order: Vec<String>         ──references──> LibraryFolder.id
└── profiles: Vec<Profile>
    ├── mod_order: Vec<String>        (derived: flattened folder_order)
    └── enabled_mods: Vec<String>     (subset of mod_order)
```

**Key invariants**:

1. Every mod ID in any `folder.mod_ids` must exist in `LibraryIndex.mods`
2. Every mod ID appears in exactly one folder's `mod_ids` — never in multiple folders
3. Every folder ID in `folder_order` must reference a valid `LibraryFolder`
4. The root folder (ID `"root"`) always exists and cannot be deleted
5. `Profile.mod_order` = flattened `folder_order` (derived, kept in sync)
6. `Profile.enabled_mods` ⊆ `Profile.mod_order`

## State Transitions

### Folder Lifecycle

```
[Not Exists] ──create_folder(name)──> [Empty Folder]
[Empty Folder] ──move_mod_to_folder(mod_id)──> [Folder with Mods]
[Folder with Mods] ──move_mod_from_folder(mod_id)──> [Folder with Mods] or [Empty Folder]
[Any Folder] ──rename_folder(new_name)──> [Same Folder, new name]
[Any Folder] ──delete_folder()──> [Not Exists] (mods moved to top-level)
```

### Mod-Folder Assignment

```
[In Root Folder] ──move_to_folder(folder_id)──> [In User Folder]
  - Remove from root folder's mod_ids
  - Append to target folder's mod_ids

[In User Folder] ──move_to_folder("root")──> [In Root Folder]
  - Remove from user folder's mod_ids
  - Append to root folder's mod_ids

[In Folder A] ──move_to_folder(folder_b_id)──> [In Folder B]
  - Remove from folder A's mod_ids
  - Append to folder B's mod_ids
```

### Batch Toggle

```
[Folder with mixed enabled states] ──toggle_folder(enabled=true)──>
  - All mod_ids in folder added to Profile.enabled_mods

[Folder with mods enabled] ──toggle_folder(enabled=false)──>
  - All mod_ids in folder removed from Profile.enabled_mods
```

## Frontend-Only State (Zustand)

### LibraryViewStore (NEW)

| Field             | Type             | Default | Persisted          | Description                                  |
| ----------------- | ---------------- | ------- | ------------------ | -------------------------------------------- |
| `expandedFolders` | `Set<string>`    | `Set()` | Yes (localStorage) | Folder IDs expanded in list view             |
| `currentFolderId` | `string \| null` | `null`  | No                 | Folder currently navigated into in grid view |

**Behavior**:

- `expandedFolders` persists via Zustand `persist` middleware (localStorage key: `ltk-library-view`)
- `currentFolderId` resets to `null` on app launch and on profile switch
- When `currentFolderId` is set, grid view shows only that folder's mods + a FolderHeader
- When search/filter is active, `currentFolderId` is ignored (flat mode)
