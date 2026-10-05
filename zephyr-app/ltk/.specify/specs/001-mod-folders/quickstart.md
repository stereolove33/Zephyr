# Quickstart: Mod Library Folders

**Branch**: `001-mod-folders` | **Date**: 2026-03-26

## Prerequisites

- Rust (stable toolchain)
- Node.js + pnpm
- Existing dev environment for ltk-manager (`pnpm install` completed)

## Development

```bash
# Full dev mode (Rust + React hot reload)
pnpm tauri dev

# Frontend only (skip Rust rebuild for UI iteration)
pnpm dev

# Type check + lint + format
pnpm check

# Rust checks
cargo clippy -p ltk-manager
cargo fmt -p ltk-manager --check

# Verbose logging (useful for debugging folder operations)
RUST_LOG=ltk_manager=trace,tauri=info pnpm tauri dev
```

## Implementation Order

### Phase 1: Backend Data Model + CRUD

1. Add `LibraryFolder` and `OrderItem` structs to `src-tauri/src/mods/mod.rs`
2. Extend `LibraryIndex` with `folders` and `item_order` fields (`#[serde(default)]`)
3. Add migration logic in `load_library_index()` for existing users
4. Implement folder CRUD methods on `ModLibrary` in new `src-tauri/src/mods/folders.rs`
5. Add command wrappers in `src-tauri/src/commands/folders.rs`
6. Register commands in `main.rs`
7. Add TS types and API bindings in `src/lib/tauri.ts`

### Phase 2: Batch Toggle + Mod Movement

1. Implement `toggle_folder` (batch enable/disable)
2. Implement `move_mod_to_folder` / `move_mod_to_top_level`
3. Implement `reorder_folder_mods` / `reorder_items`
4. Update `sync_profile_mod_orders()` to handle folders
5. Create mutation hooks with optimistic updates

### Phase 3: Frontend — List View

1. Create `FolderRow` component (expandable row with tri-state toggle)
2. Create Zustand store for expanded folder state
3. Extend `SortableModList` to render folder rows with nested mods
4. Add folder context menu (rename, delete, toggle)
5. Add "New Folder" button to `LibraryToolbar`
6. Create `CreateFolderDialog`

### Phase 4: Frontend — Grid View

1. Create `FolderCard` component (name + count + 2x2 thumbnail grid)
2. Create `FolderHeader` component (back button + name + batch toggle)
3. Implement folder navigation in grid view (Zustand `currentFolderId`)
4. Add folder context menu on folder cards

### Phase 5: DnD + Polish

1. Extend DnD to support dropping mods onto folders
2. Add drag-out-of-folder support
3. Add folder badge on mod cards in flat/search mode
4. Test backward compatibility (no folders → no changes)
5. Test profile switching with folders

## Key Files to Modify

| File                                                 | Change                                                                      |
| ---------------------------------------------------- | --------------------------------------------------------------------------- |
| `src-tauri/src/mods/mod.rs`                          | Add `LibraryFolder`, `OrderItem`, extend `LibraryIndex`                     |
| `src-tauri/src/mods/folders.rs`                      | NEW — folder business logic                                                 |
| `src-tauri/src/mods/library.rs`                      | Update `get_installed_mods()` to include folder info; update reconciliation |
| `src-tauri/src/commands/folders.rs`                  | NEW — command wrappers                                                      |
| `src-tauri/src/commands/mod.rs`                      | Export folder commands                                                      |
| `src-tauri/src/main.rs`                              | Register folder commands in `generate_handler![]`                           |
| `src/lib/tauri.ts`                                   | Add `LibraryFolder`, `OrderItem` types; add API methods                     |
| `src/modules/library/api/`                           | New hooks for folder mutations + queries                                    |
| `src/modules/library/components/`                    | New components: FolderCard, FolderRow, FolderHeader, etc.                   |
| `src/stores/libraryView.ts`                          | NEW — expanded folders + current folder navigation state                    |
| `src/modules/library/components/SortableModList.tsx` | Extend for folder DnD                                                       |
| `src/modules/library/components/ModCard.tsx`         | Add folder badge                                                            |
| `src/modules/library/components/LibraryToolbar.tsx`  | Add "New Folder" button                                                     |

## Testing Checklist

- [ ] Create folder → appears in library
- [ ] Rename folder → name updates
- [ ] Delete folder → mods return to top level
- [ ] Move mod into folder → disappears from top level, appears in folder
- [ ] Move mod out of folder → returns to top level
- [ ] Toggle folder on → all mods enabled
- [ ] Toggle folder off → all mods disabled
- [ ] Mixed state indicator shows correctly
- [ ] Grid view: click folder → navigates in, back button works
- [ ] List view: expand/collapse folder row
- [ ] Search: folders hidden, mods shown flat with folder badge
- [ ] Profile switch: folders persist, enabled states change
- [ ] No folders: library behaves identically to before
- [ ] Patcher running: folder CRUD allowed, toggle blocked
- [ ] DnD: reorder within folder, move between folders, move to top level
- [ ] Existing `library.json` without folders field loads correctly
