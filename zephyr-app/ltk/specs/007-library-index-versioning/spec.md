# Feature Specification: Library Index Schema Versioning

**Feature Branch**: `007-library-index-versioning`
**Created**: 2026-03-28
**Status**: Draft
**Input**: User description: "Should we version the library index json file/schema for better backwards compatibility?"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Seamless Upgrade Across App Versions (Priority: P1)

A user upgrades the application to a new version that changes the library index structure (e.g., adds new fields, restructures data). On first launch after the upgrade, the application detects the older schema version, migrates the index in place, and the user's mod library appears exactly as before with no data loss.

**Why this priority**: This is the core purpose of versioning — ensuring users never lose their mod library when the app evolves.

**Independent Test**: Can be tested by writing a v1 index file, launching the app with v2 schema expectations, and verifying all mods, profiles, folders, and ordering are preserved.

**Acceptance Scenarios**:

1. **Given** a library.json written by an older app version, **When** the user launches a newer version, **Then** the index is silently migrated to the current schema version and all data is preserved.
2. **Given** a library.json at the current schema version, **When** the user launches the same app version, **Then** no migration runs and the index loads normally.
3. **Given** a library.json with an unrecognized future version, **When** the user launches an older app version, **Then** the app displays a clear error message explaining the index was created by a newer version and cannot be loaded.

---

### User Story 2 - Downgrade Protection (Priority: P2)

A user rolls back to an older app version (e.g., a newer release introduced a bug). The older version detects that the index was written by a newer version and refuses to silently corrupt data, instead showing a clear message.

**Why this priority**: Prevents silent data corruption when users downgrade, which is harder to recover from than a clear error.

**Independent Test**: Can be tested by writing an index with a version number higher than the app understands and verifying the app rejects it with a user-friendly message.

**Acceptance Scenarios**:

1. **Given** a library.json with version 3, **When** the app only understands up to version 2, **Then** the app shows an error indicating the user needs a newer version.
2. **Given** the downgrade scenario, **When** the error is shown, **Then** the user's library.json is not modified or overwritten.

---

### User Story 3 - Backup Before Migration (Priority: P3)

Before migrating a library index to a new schema version, the application creates a backup of the original file so users can recover if something goes wrong.

**Why this priority**: Provides a safety net for migrations, reducing the risk of irreversible data loss.

**Independent Test**: Can be tested by triggering a migration and verifying a backup file exists alongside the migrated index.

**Acceptance Scenarios**:

1. **Given** a library.json that needs migration, **When** migration runs, **Then** a backup copy of the original file is saved before any modifications.
2. **Given** a migration has been backed up, **When** migration completes successfully, **Then** the backup file remains available for manual recovery.

---

### Edge Cases

- What happens when library.json is malformed or truncated (not valid JSON)? The existing behavior (serde error) continues — versioning does not change error handling for corrupt files.
- What happens when a migration step fails partway through? The backup file preserves the original state; the failed migration leaves the index in an error state that the user can recover from by restoring the backup.
- What happens when the version field is missing entirely (pre-versioning index)? Treated as version 0 (legacy) and migrated forward to the current version.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The library index file MUST contain a schema version identifier.
- **FR-002**: The application MUST detect the schema version on load and run appropriate migrations to bring the index up to the current version.
- **FR-003**: Migrations MUST be sequential — each version step has a dedicated migration function (v1->v2, v2->v3, etc.), and the system chains them as needed.
- **FR-004**: The application MUST reject indexes with a schema version higher than the app understands, displaying a clear error to the user without modifying the file.
- **FR-005**: The application MUST create a backup of the library index file before performing any schema migration.
- **FR-006**: Existing library.json files without a version field MUST be treated as version 0 (legacy) and migrated forward to the current version.
- **FR-007**: The version field MUST be persisted on every save so the schema version is always present in the file.

### Key Entities

- **Library Index**: The `library.json` file stored in the mod storage directory. Contains mods, profiles, folders, folder ordering, and the active profile reference. Will gain a `version` field.
- **Schema Version**: An integer identifier embedded in the index that declares which schema shape the data conforms to.
- **Migration**: A function that transforms an index from version N to version N+1, preserving all user data.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Users upgrading from any prior app version retain 100% of their mod library data (mods, profiles, folders, ordering, enabled states) after migration.
- **SC-002**: Opening an index from a newer app version in an older version always produces a user-visible error rather than silent data corruption.
- **SC-003**: A backup file exists for every migration performed, enabling manual recovery.

## Assumptions

- The library index is a local, single-user file — there is no multi-device sync or concurrent access beyond the existing `index_lock` mutex.
- The current ad-hoc migration code in `load_library_index` (root folder migration, folder_order population) represents the existing v1 schema shape and will be absorbed into the versioned migration system.
- Version numbers are monotonically increasing integers starting at 1.
- The initial rollout will set the current schema as version 1, consolidate existing migrations under a "pre-v1 to v1" upgrade path, and establish the framework for future v1->v2+ migrations.
