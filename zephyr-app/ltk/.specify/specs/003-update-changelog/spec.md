# Feature Specification: Update Changelog Display

**Feature Branch**: `003-update-changelog`
**Created**: 2026-03-11
**Status**: Draft
**Input**: User description: "Show changelog/release notes in the update notification so users can see what changed before updating"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - View Changelog Before Updating (Priority: P1)

A user sees the "Update Available" banner and wants to know what changed before deciding to update. They click a "What's New" button, which opens a dialog showing the release notes (grouped by category: Added, Fixed, Performance, etc.). After reviewing, they can either update directly from the dialog or close it.

**Why this priority**: Core value — users need visibility into what an update contains to make an informed decision. Without this, users are updating blindly.

**Independent Test**: Can be verified by triggering the update notification and confirming the changelog dialog renders markdown content with proper formatting.

**Acceptance Scenarios**:

1. **Given** an update is available with release notes, **When** the user clicks "What's New", **Then** a dialog opens showing the changelog rendered as formatted markdown with section headings and bullet lists.
2. **Given** an update is available without release notes (body is empty/undefined), **When** the user clicks "What's New", **Then** the dialog shows a "No release notes available" fallback message.
3. **Given** the changelog dialog is open, **When** the user clicks "Update Now", **Then** the update begins downloading and installing (same behavior as the banner's Update Now button).
4. **Given** the changelog dialog is open, **When** the user clicks "Close" or the X button, **Then** the dialog closes and the update banner remains visible.

---

## Functional Requirements

### FR-1: Changelog Rendering

- The changelog body (markdown from GitHub release notes) must be rendered with proper formatting for headings, bullet lists, and paragraphs.
- Styling must match the application's dark theme using surface color tokens.

### FR-2: Changelog Dialog

- A modal dialog displays the changelog with a "What's New in v{version}" header.
- The dialog must be scrollable for long changelogs (max height 60vh).
- The dialog includes "Update Now" and "Close" action buttons.

### FR-3: Update Notification Integration

- A "What's New" button is added to the existing update notification banner.
- The button opens the changelog dialog without disrupting the existing update flow.

## Non-Functional Requirements

### NFR-1: Bundle Size

- The markdown rendering dependency should be lightweight (under 15kb gzipped).

## Out of Scope

- Backend/Rust changes
- Modifying the update check logic or `useUpdateCheck` hook
- Changelog fetching from any source other than `Update.body`
- Rich media (images, videos) in changelog rendering

## Success Criteria

1. Users can view formatted release notes before updating.
2. The changelog dialog is accessible from the update notification banner.
3. The existing update flow (Update Now, dismiss) continues to work unchanged.
