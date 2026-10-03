# Plan: Lightweight Mod Content Browser

Status: Draft
Created: 2026-04-18
Scope: Workshop project view

## 1. Goal

Give creators at-a-glance visibility into what a workshop project actually contains, without the Manager taking on any file editing or preview responsibilities. Editing and deep inspection is handed off to [LTK Forge](https://github.com/LeagueToolkit/ltk-forge).

## 2. Non-Goals

Explicitly out of scope — the Manager is a **conductor**, not an editor:

- File preview (images, models, particles, audio) — Forge's job.
- File editing, conversion, or extraction — Forge's job.
- WAD/bin inspection beyond file listing — already covered by `wad_reports.rs` for installed mods; not duplicated here.
- A new persistence layer (sidecar state, workflow checklist, mod-type hints).
- Any changes to `mod.config.json` schema or the on-disk project layout.
- Live file watching via `notify`. Poll-on-focus is sufficient for v1.

## 3. User-Facing Shape

A single collapsible panel on the project overview page titled **"Content"**. Default: expanded.

For each layer folder under `content/`:

- Layer header row (`base`, `red-theme`, etc.) with total file count and total size.
- Collapsible file tree beneath the header. Directories expand/collapse on click, show a cumulative file count on the right, and the first-level directories start expanded so creators see their top-level layout without needing to click. Deeper directories stay collapsed until opened.
- Per-file row: kind icon (with kind name on hover), filename, size. Full relative path isn't shown because the tree structure already conveys it.
- Per-layer actions: **Open folder** (shell reveal), **Open in LTK Forge** (see §6).
- Empty state per layer: one line of text + link to the LTK wiki extraction guide. No wizard, no banners.

**Tree vs. flat list**: the original plan argued for a flat list "grouped by top-level subdirectory" to avoid tree-widget complexity. We reversed that decision during implementation — LoL mod content routinely nests 4–6 levels deep (`assets/characters/<champion>/skins/skin##/...`), which made the flat presentation with inlined path segments visually noisy and hard to scan. A standard collapsible tree is both more familiar and cheaper to read at a glance. The tree is built frontend-side from the same flat `ContentEntry[]` the backend returns, so no wire-format change.

Drag-and-drop onto the panel copies files into the currently active layer's content directory. This is the only write operation the browser performs.

## 4. Backend

New file: `src-tauri/src/workshop/content.rs`.

One new command, registered in `main.rs`:

```rust
#[tauri::command]
get_project_content_tree(project_name: String) -> IpcResult<ContentTree>
```

Types:

```rust
pub struct ContentTree {
    pub layers: Vec<LayerContent>,
}

pub struct LayerContent {
    pub name: String,           // "base", "red-theme", ...
    pub file_count: usize,
    pub total_size_bytes: u64,
    pub entries: Vec<ContentEntry>,
}

pub struct ContentEntry {
    pub relative_path: String,  // POSIX-style, relative to content/{layer}/
    pub size_bytes: u64,
    pub kind: LeagueFileKind,   // from ltk_file, serialized as snake_case
}
```

Implementation notes:

- Walk `content/{layer}/` with `walkdir`.
- Skip hidden files (leading `.`) and symlinks.
- Return every file — **no entry cap**. Real mods can contain tens of thousands of files; frontend virtualization handles render cost.
- Return sorted: layers alphabetical with `base` first; entries by `relative_path`.
- Follows the `AppResult<T>` → `IpcResult<T>` pattern in `commands/workshop.rs`.

No mutation commands. File drops use the existing file-copy path (whichever dialog/dnd path is wired today — reuse, don't duplicate).

### 4.1 File Type Identification

Each entry is tagged with a `LeagueFileKind` from the [`ltk_file`](https://crates.io/crates/ltk_file) crate (already a dependency at `0.2.8`).

- **Detection strategy: extension-only via `LeagueFileKind::from_extension()`.** Cheap, no I/O, runs inline during the walk. Handles all League-native formats the crate knows: `bin`, `dds`, `tex`, `skn`, `skl`, `anm`, `scb`, `sco`, `mapgeo`, `wgeo`, `bnk`, `wpk`, `stringtable`, `preload`, `luaobj`, `png`, `jpg`, `tga`, `svg`.
- **No magic-byte sniffing in v1.** `identify_from_bytes()` requires reading `MAX_MAGIC_SIZE` bytes per file — meaningful I/O cost across thousands of files, and mod content dirs almost always have correct extensions. Deferred to a future enhancement if a real need surfaces (e.g., files with wrong/missing extensions).
- **Enable the `serde` feature on `ltk_file`** in `src-tauri/Cargo.toml`:
  ```toml
  ltk_file = { version = "0.2.8", features = ["serde"] }
  ```
  This gives us `snake_case` JSON serialization for the enum (`"property_bin"`, `"texture_dds"`, `"wwise_bank"`, etc.) — stable and unambiguous to match on the frontend.
- Files whose extension isn't recognized serialize as `"unknown"`. That's the fallback bucket on the frontend too.

## 5. Frontend

New files under `src/modules/workshop/`:

- `api/useProjectContentTree.ts` — `useQuery`, keyed by project name. `staleTime: 0`, refetch on window focus (TanStack Query already supports this, no custom listener needed).
- `components/ContentBrowser.tsx` — the panel component.
- `components/ContentBrowserLayerSection.tsx` — per-layer rendering.
- Export both through `components/index.ts` → `modules/workshop/index.ts`.

Rendering rules:

- Use only wrapped primitives from `@/components` where one exists. The tree rows use native `<button>`/`<div>` with `role="tree"` / `role="treeitem"` since there's no wrapped tree primitive and building one isn't justified for a single consumer.
- Tree structure is built in `utils/contentTree.ts` (pure, unit-tested) from the backend's flat `ContentEntry[]`. A companion `flattenTree(tree, expanded)` turns the tree into a linear list of currently-visible rows, which is fed to [`@tanstack/react-virtual`](https://tanstack.com/virtual) — only rows in the scroll viewport mount. This lets the UI handle projects with tens of thousands of files without the backend or frontend needing an entry cap.
- Row height is fixed at 22px so the virtualizer doesn't need measurement. Indent rails per ancestor level render inside each row; because every row draws at the same left offsets, the vertical guide lines appear continuous across the virtual window.
- Directories are sorted first, then files, each group alphabetized, to match standard file-tree expectations.
- Row icons come from the file-kind mapping in §5.1. Panel chrome icons (`Folder`, `ChevronRight`, `ExternalLink`, `FolderOpen`, `RefreshCw`) from `lucide-react`.
- Size formatting: share a helper with existing size display code if one exists; otherwise add `utils/formatBytes.ts`. Check before duplicating.

Mount point: `src/modules/workshop/components/overview/` — added as a new section in the overview composition, after existing sections. No route changes.

### 5.1 File Kind → Icon Mapping

Single mapping table lives in `src/modules/workshop/utils/fileKindIcon.ts`. The TypeScript `LeagueFileKind` union is hand-mirrored from the Rust enum (small, stable surface — 21 variants).

Group by what the kind _is_, not by extension, so visually-similar formats share an icon:

| Category        | Kinds                                                                                      | Icon (lucide)    | Tint token      |
| --------------- | ------------------------------------------------------------------------------------------ | ---------------- | --------------- |
| Texture / image | `png`, `jpeg`, `tga`, `svg`, `texture`, `texture_dds`                                      | `Image`          | `--accent-400`  |
| Mesh            | `simple_skin`, `static_mesh_ascii`, `static_mesh_binary`, `map_geometry`, `world_geometry` | `Box`            | `--surface-300` |
| Animation / rig | `animation`, `skeleton`                                                                    | `PersonStanding` | `--surface-300` |
| Property data   | `property_bin`, `property_bin_override`, `preload`                                         | `FileCode2`      | `--surface-400` |
| Text / strings  | `riot_string_table`, `lua_obj`                                                             | `FileText`       | `--surface-400` |
| Audio           | `wwise_bank`, `wwise_package`                                                              | `Volume2`        | `--surface-300` |
| Light data      | `light_grid`                                                                               | `Sun`            | `--surface-300` |
| Unknown         | `unknown`                                                                                  | `File`           | `--surface-500` |

A small `Tooltip` on the row icon shows the human-readable kind name (`"Property Bin"`, `"Wwise Bank"`, `"DDS Texture"`, …) — derived from the same mapping table. Cheap way to teach creators what they're looking at without running a full docs page.

The mapping is exhaustive (`ts-pattern` or a `satisfies Record<LeagueFileKind, ...>`) so adding a new kind upstream triggers a TypeScript error until the table is updated.

## 6. LTK Forge Handoff

Forge is a separate desktop app. The Manager never embeds Forge UI.

Minimum viable handoff (v1): **"Open in LTK Forge"** button per layer that invokes a deep link / shell command. Two options:

1. **Protocol handler** (`ltk-forge://open?path=...`) — requires Forge to register a protocol on install. Cleanest. Confirm Forge registers one before committing to this path.
2. **Shell launch with path argument** — fall back to `shell::open` with the layer folder if no protocol is registered.

Decision deferred until we confirm Forge's CLI / protocol surface. Track as open question in §9.

Either way, the button is best-effort: if Forge isn't installed, show a one-line hint linking to the Forge releases page. No tool-detection heuristics, no background probes.

## 7. Refresh Behavior

- Automatic refetch when the project route gains window focus (`refetchOnWindowFocus: true`).
- Automatic refetch after a successful drag-drop add.
- Manual **Refresh** icon button in the panel header for the 1% case where focus-based refresh misses a change.

No file watcher. If this proves insufficient in practice we revisit, but the `notify` + debouncer surface is meaningful complexity (buffer overflows on Windows, cross-platform quirks) and not worth paying for until we know we need it.

## 8. Delivery

Single PR. Suggested commit breakdown:

1. Backend: `content.rs` + command registration + unit tests for the walker (empty project, nested project, truncation cap).
2. Frontend: `useProjectContentTree` hook + `ContentBrowser` components.
3. Wire into overview; mark this plan's status as Shipped.

Rough size: ~400–600 LOC total. Fits the existing Workshop module without introducing new patterns.

## 9. Open Questions

- Forge handoff mechanism — protocol URL vs. CLI arg. Requires a conversation with Forge's maintainer before implementation.
- Whether `walkdir` is already in the tree or needs adding to `Cargo.toml`.
- Whether an existing `formatBytes` helper exists in `src/utils/`.
- Whether to bump `ltk_file` to `0.2.9` while we're touching the dep (changelog check before bumping).

## 10. Deliberately Not Doing

Captured here so future contributors don't relitigate:

- Workflow checklist / auto-completing steps.
- "Mod type" concept or sidecar state files.
- Terminology rename (`layers` → `variants`, `string overrides` → `text changes`). Orthogonal; if wanted, that's a separate, purely-cosmetic PR.
- Guidance configuration JSON framework.
- First-mod wizard.
- Tool-detection heuristics for external editors.
- In-app publish button placeholder.
- Magic-byte file identification via `ltk_file::identify_from_bytes()`. Extension-based is enough for v1; revisit only if content dirs with mislabeled files become a real problem.
