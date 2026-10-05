# Data Model: Library Index Schema Versioning

## Entity: LibraryIndex

The `library.json` file stored in the mod storage directory.

### Current Schema (v0 — legacy, pre-versioning)

```json
{
  "mods": [...],
  "profiles": [...],
  "activeProfileId": "uuid",
  "folders": [...],
  "folderOrder": [...]
}
```

Note: `folders` and `folderOrder` may be absent in older v0 files (pre-folder feature). The existing ad-hoc migration in `load_library_index` handles this.

### Target Schema (v1 — post-versioning)

```json
{
  "version": 1,
  "mods": [...],
  "profiles": [...],
  "activeProfileId": "uuid",
  "folders": [...],
  "folderOrder": [...]
}
```

Only change: addition of the `version` integer field at the top level.

### Fields

| Field             | Type                   | Required              | Default                | Description                                                  |
| ----------------- | ---------------------- | --------------------- | ---------------------- | ------------------------------------------------------------ |
| `version`         | integer                | Yes (after migration) | `0` via serde default  | Schema version identifier. `0` = legacy pre-versioning file. |
| `mods`            | `Vec<LibraryModEntry>` | Yes                   | `[]`                   | Installed mod entries                                        |
| `profiles`        | `Vec<Profile>`         | Yes                   | `[default]`            | User profiles with mod ordering and enabled states           |
| `activeProfileId` | `String`               | Yes                   | default profile UUID   | Currently active profile                                     |
| `folders`         | `Vec<LibraryFolder>`   | Yes (after v1)        | `[]` via serde default | Mod folders for organization                                 |
| `folderOrder`     | `Vec<String>`          | Yes (after v1)        | `[]` via serde default | Display order of folder IDs                                  |

### State Transitions

```
File does not exist ──→ LibraryIndex::default() (version = CURRENT_VERSION)
                           │
                           ▼
                     save_library_index()
                           │
                           ▼
                    library.json on disk (version = CURRENT_VERSION)


File exists (no version field, v0) ──→ load raw JSON
                                          │
                                          ▼
                                    detect version = 0
                                          │
                                          ▼
                                    backup library.v0.json.bak
                                          │
                                          ▼
                                    run v0→v1 migration
                                    (add root folder, populate folder_order, set version=1)
                                          │
                                          ▼
                                    deserialize into LibraryIndex
                                          │
                                          ▼
                                    save (persists version=1)


File exists (version > CURRENT_VERSION) ──→ AppError::SchemaVersionTooNew
                                              (no modification, no backup)


File exists (version = CURRENT_VERSION) ──→ deserialize directly
                                              (no migration, no backup)
```

## Entity: ErrorCode (extended)

New variant added to the `ErrorCode` enum:

| Variant               | Serialized               | Context                                   | Description                                      |
| --------------------- | ------------------------ | ----------------------------------------- | ------------------------------------------------ |
| `SchemaVersionTooNew` | `SCHEMA_VERSION_TOO_NEW` | `{ "fileVersion": N, "maxSupported": M }` | Library index was written by a newer app version |

## Migration Registry

| From | To  | Function   | Description                                                                                                                           |
| ---- | --- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| 0    | 1   | `v0_to_v1` | Absorbs existing ad-hoc folder migration + sets version field. Ensures root folder exists, populates folder_order, sets version to 1. |

Future migrations (v1→v2, v2→v3, etc.) are added as new entries in this registry.
