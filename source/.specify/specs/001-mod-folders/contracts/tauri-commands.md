# Tauri Command Contracts: Mod Library Folders

**Branch**: `001-mod-folders` | **Date**: 2026-03-26

## New Commands

### `create_folder`

Creates a new empty folder in the library.

**Input**:

```json
{ "name": "string" }
```

**Output** (`IpcResult<LibraryFolder>`):

```json
{
  "ok": true,
  "value": {
    "id": "uuid-string",
    "name": "Ranked Skins",
    "modIds": []
  }
}
```

**Errors**:

- `VALIDATION_FAILED` — name is empty or whitespace-only

**Side effects**: Appends `OrderItem::Folder(id)` to end of `item_order`. Persists `library.json`. Does NOT invalidate overlay (organizational change only).

**Patcher guard**: None (allowed while patcher running).

---

### `rename_folder`

Renames an existing folder.

**Input**:

```json
{ "folderId": "string", "newName": "string" }
```

**Output** (`IpcResult<()>`): void on success.

**Errors**:

- `VALIDATION_FAILED` — folder not found, or `newName` is empty/whitespace
- Specific error code TBD if folder not found (may use new `FolderNotFound` variant or reuse `VALIDATION_FAILED`)

**Patcher guard**: None.

---

### `delete_folder`

Deletes a folder, moving its mods back to the top level.

**Input**:

```json
{ "folderId": "string" }
```

**Output** (`IpcResult<()>`): void on success.

**Errors**:

- `VALIDATION_FAILED` — folder not found

**Side effects**: Removes `OrderItem::Folder(id)` from `item_order`. Inserts `OrderItem::Mod(id)` for each mod that was in the folder (in their original order, at the position where the folder was). Removes folder from `folders` vec. Persists. Does NOT invalidate overlay.

**Patcher guard**: None.

---

### `move_mod_to_folder`

Moves a mod into a folder (from top level or another folder).

**Input**:

```json
{ "modId": "string", "folderId": "string" }
```

**Output** (`IpcResult<()>`): void on success.

**Errors**:

- `MOD_NOT_FOUND` — mod doesn't exist
- `VALIDATION_FAILED` — folder not found, or mod is already in the target folder

**Side effects**: Removes mod from its current location (top-level `item_order` or another folder's `mod_ids`). Appends to target folder's `mod_ids`. Re-derives `Profile.mod_order` for all profiles. Persists. Does NOT invalidate overlay (mod priority may change, but overlay uses `enabled_mods` which is re-derived).

**Patcher guard**: None (organizational change).

---

### `move_mod_to_top_level`

Moves a mod out of its folder to the top level.

**Input**:

```json
{ "modId": "string", "position": "number | null" }
```

`position`: Index in `item_order` to insert at. `null` = append to end.

**Output** (`IpcResult<()>`): void on success.

**Errors**:

- `MOD_NOT_FOUND` — mod doesn't exist
- `VALIDATION_FAILED` — mod is not in any folder

**Side effects**: Removes mod from folder's `mod_ids`. Inserts `OrderItem::Mod(id)` into `item_order` at the specified position. Re-derives `Profile.mod_order`. Persists.

**Patcher guard**: None.

---

### `toggle_folder`

Enables or disables all mods in a folder for the active profile.

**Input**:

```json
{ "folderId": "string", "enabled": "boolean" }
```

**Output** (`IpcResult<()>`): void on success.

**Errors**:

- `VALIDATION_FAILED` — folder not found or folder is empty
- `PATCHER_RUNNING` — patcher is active

**Side effects**: For each mod in the folder: if `enabled=true`, add to `Profile.enabled_mods` (if not already present); if `enabled=false`, remove from `Profile.enabled_mods`. Persists. Invalidates overlay.

**Patcher guard**: YES — blocked while patcher is running.

---

### `reorder_folder_mods`

Reorders mods within a folder.

**Input**:

```json
{ "folderId": "string", "modIds": ["string"] }
```

**Output** (`IpcResult<()>`): void on success.

**Errors**:

- `VALIDATION_FAILED` — folder not found, or `modIds` doesn't match the folder's current mod set (same validation as existing `reorder_mods`)

**Side effects**: Replaces folder's `mod_ids` with the new order. Re-derives `Profile.mod_order` and `Profile.enabled_mods` order for all profiles. Persists. Invalidates overlay (priority order changed).

**Patcher guard**: YES — blocked while patcher is running (changes overlay priority).

---

### `reorder_items` (replaces/extends `reorder_mods`)

Reorders top-level items (mix of mods and folders).

**Input**:

```json
{
  "itemOrder": [
    { "type": "mod", "id": "string" },
    { "type": "folder", "id": "string" }
  ]
}
```

**Output** (`IpcResult<()>`): void on success.

**Errors**:

- `VALIDATION_FAILED` — provided items don't match current `item_order` set

**Side effects**: Replaces `item_order`. Re-derives `Profile.mod_order` and `Profile.enabled_mods` for all profiles. Persists. Invalidates overlay.

**Patcher guard**: YES — blocked while patcher is running.

---

## Modified Commands

### `get_installed_mods` (MODIFIED)

**Change**: Each `InstalledMod` in the response now includes `folder_id: Option<String>`. Folder name is NOT included — the frontend resolves it at runtime from the `get_folders` response. The response order reflects the flattened `item_order` (folders expanded in-place).

**New response shape** (per mod):

```json
{
  "id": "...",
  "name": "...",
  "displayName": "...",
  "enabled": true,
  "folderId": "folder-uuid-or-null",
  ...existing fields...
}
```

### `get_folders`

Returns all folders with their metadata.

**Input**: none

**Output** (`IpcResult<Vec<LibraryFolder>>`):

```json
{
  "ok": true,
  "value": [
    { "id": "uuid", "name": "Ranked Skins", "modIds": ["mod1", "mod2"] },
    { "id": "uuid", "name": "ARAM Fun", "modIds": ["mod3"] }
  ]
}
```

### `get_item_order`

Returns the current top-level item ordering.

**Input**: none

**Output** (`IpcResult<Vec<OrderItem>>`):

```json
{
  "ok": true,
  "value": [
    { "type": "mod", "id": "mod-id-1" },
    { "type": "folder", "id": "folder-id-1" },
    { "type": "mod", "id": "mod-id-2" }
  ]
}
```

## Frontend API Additions

```typescript
// In api object (src/lib/tauri.ts)
createFolder: (name: string) => invokeResult<LibraryFolder>("create_folder", { name }),
renameFolder: (folderId: string, newName: string) => invokeResult<void>("rename_folder", { folderId, newName }),
deleteFolder: (folderId: string) => invokeResult<void>("delete_folder", { folderId }),
moveModToFolder: (modId: string, folderId: string) => invokeResult<void>("move_mod_to_folder", { modId, folderId }),
moveModToTopLevel: (modId: string, position?: number) => invokeResult<void>("move_mod_to_top_level", { modId, position: position ?? null }),
toggleFolder: (folderId: string, enabled: boolean) => invokeResult<void>("toggle_folder", { folderId, enabled }),
reorderFolderMods: (folderId: string, modIds: string[]) => invokeResult<void>("reorder_folder_mods", { folderId, modIds }),
reorderItems: (itemOrder: OrderItem[]) => invokeResult<void>("reorder_items", { itemOrder }),
getFolders: () => invokeResult<LibraryFolder[]>("get_folders"),
getItemOrder: () => invokeResult<OrderItem[]>("get_item_order"),
```
