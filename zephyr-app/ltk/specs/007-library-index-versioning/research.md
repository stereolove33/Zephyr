# Research: Library Index Schema Versioning

## R1: Version Field Strategy

**Decision**: Use a top-level integer `version` field in the JSON, defaulting to `1` via `serde(default)`.

**Rationale**: An integer is simple, unambiguous, and sufficient for a local single-file schema. Semver is overkill — there's no concept of backwards-compatible minor changes when the app controls both reader and writer. Missing field (pre-versioning files) deserializes as `0` via `serde(default)`, which signals "legacy, needs full migration to v1."

**Alternatives considered**:

- String semver (`"1.0.0"`) — unnecessarily complex for a local file with a single consumer.
- Separate `schema_version` field name — `version` is shorter and unambiguous since there's no other version in the index.

## R2: Migration Architecture

**Decision**: A `migrate_index` function takes `serde_json::Value` and a version integer, applies sequential migration functions (v0→v1, v1→v2, etc.) until reaching `CURRENT_VERSION`, then deserializes into the typed `LibraryIndex`.

**Rationale**: Working on `serde_json::Value` during migrations avoids needing old struct versions. Each migration is a pure function `fn(Value) -> AppResult<Value>` that transforms the JSON shape. This is the standard pattern for local-file schema evolution (SQLite `user_version` + migrations, VS Code settings, etc.).

**Alternatives considered**:

- Versioned struct types (`LibraryIndexV1`, `LibraryIndexV2`) — combinatorial explosion, unnecessary when `Value` manipulation suffices.
- serde `#[serde(alias)]` / `#[serde(flatten)]` — only handles additive changes, not restructuring.

## R3: Backup Strategy

**Decision**: Copy `library.json` to `library.v{N}.json.bak` before migrating from version N.

**Rationale**: The filename includes the source version so users (or support) can identify which app version created it. A single backup per source version is sufficient — if migration runs again, overwriting the backup is fine since the source data is the same.

**Alternatives considered**:

- Timestamped backups (`library.json.2026-03-28T12-00-00.bak`) — accumulates clutter; version-keyed is more useful for recovery.
- No backup (rely on OS-level backup/git) — too risky for end users who don't have these.

## R4: Downgrade Detection

**Decision**: After extracting the `version` field from raw JSON, if `version > CURRENT_VERSION`, return a new `AppError::SchemaVersionTooNew` error. The frontend handles this via `hasErrorCode()` and shows a toast.

**Rationale**: Clean error propagation through the existing `AppError` → `AppErrorResponse` → `IpcResult` pipeline. The error carries context with the file version and the max supported version so the frontend can display a helpful message.

**Alternatives considered**:

- Silently ignore unknown fields (serde `deny_unknown_fields` disabled) — doesn't protect against structural changes that remove or rename fields.
- Show a blocking dialog instead of toast — overkill; the app can still function for settings changes, it just can't load the library.

## R5: Existing Migration Absorption

**Decision**: The current ad-hoc code in `load_library_index` (root folder creation, folder_order population) becomes the `v0_to_v1` migration. Files without a `version` field are treated as v0.

**Rationale**: This formalizes the existing migration into the new framework with no behavioral change. After migration, `version` is set to `1` and saved, so the migration runs exactly once.

**Alternatives considered**:

- Start at v1 and call pre-versioning files v1 — loses the ability to distinguish "already has folders" from "needs folder migration." Using v0 for legacy files is cleaner.
