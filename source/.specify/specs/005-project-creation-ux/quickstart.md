# Quickstart: Improved Project Creation Dialog UX

**Feature Branch**: `005-project-creation-ux`
**Date**: 2026-03-19

## Overview

This feature improves the "New Project" dialog UX by:

1. Making display name the primary input with auto-derived slug
2. Adding optional thumbnail selection directly in the dialog
3. Persisting the author name across sessions
4. Refreshing the dialog layout for a more polished feel

## Files to Modify

### Frontend (all changes)

| File                                                   | Change                                                                           |
| ------------------------------------------------------ | -------------------------------------------------------------------------------- |
| `src/modules/workshop/components/NewProjectDialog.tsx` | Major rewrite: new layout, slug derivation, thumbnail picker, form field reorder |
| `src/stores/workshopDialogs.ts`                        | Add `lastAuthorName` with persist middleware                                     |

### No Backend Changes

The feature reuses existing Tauri commands:

- `create_workshop_project` — project creation
- `set_project_thumbnail` — thumbnail application after creation

## Key Patterns to Follow

### Slug Derivation

```ts
function toSlug(value: string): string {
  return value
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "") // strip diacritics
    .replace(/[^\x00-\x7F]/g, "") // strip non-ASCII
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-") // non-alphanumeric → hyphen
    .replace(/^-+|-+$/g, ""); // trim hyphens
}
```

### Thumbnail File Picker (existing pattern from ThumbnailSection)

```ts
import { open } from "@tauri-apps/plugin-dialog";
import { convertFileSrc } from "@tauri-apps/api/core";

const file = await open({
  multiple: false,
  filters: [
    { name: "Images", extensions: ["webp", "png", "jpg", "jpeg", "gif", "bmp", "tiff", "ico"] },
  ],
});
if (file) {
  setSelectedThumbnailPath(file);
  // Preview: <img src={convertFileSrc(file)} />
}
```

### Author Persistence (Zustand persist)

```ts
import { persist } from "zustand/middleware";

// Add persist middleware to workshopDialogs store
// Key: "workshop-dialogs"
// Only persist: lastAuthorName (not dialog open state)
```

### Form Submission Flow

```ts
// 1. Create project
const project = await createProject({
  name: values.name,
  displayName: values.displayName,
  description: values.description,
  authors: values.authorName ? [values.authorName] : [],
});

// 2. Set thumbnail if selected
if (selectedThumbnailPath) {
  await setProjectThumbnail({
    projectPath: project.path,
    imagePath: selectedThumbnailPath,
  });
}

// 3. Persist author name
setLastAuthorName(values.authorName);
```

## Dev Commands

```bash
# Run with hot reload (frontend only — no backend changes needed)
pnpm dev

# Full dev mode if needed
pnpm tauri dev

# Validate before PR
pnpm check
```
