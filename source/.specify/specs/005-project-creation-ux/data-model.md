# Data Model: Improved Project Creation Dialog UX

**Feature Branch**: `005-project-creation-ux`
**Date**: 2026-03-19

## Entities

### No New Backend Entities

This feature introduces no new backend data structures. All existing Rust types remain unchanged:

- `CreateProjectArgs { name, display_name, description, authors }` — unchanged
- `WorkshopProject` — unchanged
- `Settings` — unchanged

### Frontend State Changes

#### Modified: Workshop Dialogs Store (`workshopDialogs.ts`)

Current state:

```
WorkshopDialogsState {
  newProjectOpen: boolean
}
```

New state (with persist middleware):

```
WorkshopDialogsState {
  newProjectOpen: boolean
  lastAuthorName: string         // Persisted to localStorage
}
```

- `lastAuthorName` defaults to `""` on first use
- Updated on successful project creation with the submitted author name
- Read as the default value for the author field when dialog opens

#### New: Dialog Component State (local to NewProjectDialog)

```
DialogLocalState {
  slugManuallyEdited: boolean    // Ref, not reactive state
  selectedThumbnailPath: string | null  // Local state for file path from picker
}
```

- `slugManuallyEdited` is a `useRef<boolean>(false)`, reset when dialog opens
- `selectedThumbnailPath` is `useState<string | null>(null)`, reset when dialog closes

### Form Schema Changes

Current Zod schema:

```
{
  name: string           // Project slug (primary input)
  displayName: string    // Auto-generated from name
  description: string
  authorName: string
}
```

New Zod schema:

```
{
  displayName: string    // Primary input (moved to first position)
  name: string           // Project slug (auto-derived from displayName)
  description: string
  authorName: string
}
```

Field ordering change only — the same four fields, same validation rules. The `name` field validation remains `^[a-z0-9-]+$` with no leading/trailing hyphens.

### Slug Derivation Function

```
toSlug(displayName: string) → string
```

Transform rules:

1. Input: any Unicode string
2. Strip non-ASCII characters
3. Convert to lowercase
4. Replace whitespace and non-alphanumeric characters with `-`
5. Collapse consecutive `-` into single `-`
6. Strip leading and trailing `-`
7. Output: string matching `^[a-z0-9-]*$`

Examples:
| Display Name | Slug |
|---|---|
| "My Awesome Mod" | `my-awesome-mod` |
| "Kai'Sa's New Look!" | `kaisas-new-look` |
| " --Extra Spaces-- " | `extra-spaces` |
| "!!!" | ``(empty — form validation blocks submission) |
| "Mod v2.0" |`mod-v20` |

## Relationships

```
Dialog opens
  → reads lastAuthorName from store
  → user types displayName
  → toSlug() derives name (unless slugManuallyEdited)
  → user optionally selects thumbnail (stored as selectedThumbnailPath)
  → on submit:
      1. createWorkshopProject({ name, displayName, description, authors })
      2. if selectedThumbnailPath: setProjectThumbnail(project.path, selectedThumbnailPath)
      3. update lastAuthorName in store
```
