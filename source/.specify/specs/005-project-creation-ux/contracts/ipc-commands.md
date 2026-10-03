# IPC Contracts: Project Creation Dialog

**Feature Branch**: `005-project-creation-ux`
**Date**: 2026-03-19

## Overview

No new IPC commands are introduced. This feature reuses two existing Tauri commands in sequence.

## Command Flow

### Step 1: Create Project (existing)

**Command**: `create_workshop_project`
**Direction**: Frontend → Backend

**Request**:

```json
{
  "args": {
    "name": "my-awesome-mod",
    "displayName": "My Awesome Mod",
    "description": "A brief description",
    "authors": ["AuthorName"]
  }
}
```

**Response** (success):

```json
{
  "status": "ok",
  "data": {
    "path": "/path/to/workshop/my-awesome-mod",
    "name": "my-awesome-mod",
    "displayName": "My Awesome Mod",
    "version": "1.0.0",
    "description": "A brief description",
    "authors": [{ "name": "AuthorName", "role": null }],
    "tags": [],
    "champions": [],
    "maps": [],
    "layers": [
      {
        "name": "base",
        "priority": 0,
        "description": "Base layer of the mod",
        "stringOverrides": {}
      }
    ],
    "thumbnailPath": null,
    "lastModified": "2026-03-19T12:00:00Z"
  }
}
```

**Error codes**: `ProjectAlreadyExists`, `ValidationFailed`, `Io`

### Step 2: Set Thumbnail (existing, optional)

**Command**: `set_project_thumbnail`
**Direction**: Frontend → Backend
**Condition**: Only called if user selected a thumbnail in the dialog

**Request**:

```json
{
  "projectPath": "/path/to/workshop/my-awesome-mod",
  "imagePath": "/path/to/selected/image.png"
}
```

**Response** (success): Updated `WorkshopProject` with `thumbnailPath` set to `"thumbnail.webp"`.

**Error codes**: `Io`, `InvalidPath`

## Orchestration

The frontend is responsible for:

1. Calling `create_workshop_project` first
2. On success, if a thumbnail was selected, calling `set_project_thumbnail` with the new project's path
3. Handling partial failure: if project creation succeeds but thumbnail fails, the project still exists (toast error for thumbnail, project is still usable)
