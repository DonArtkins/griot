# Feature 07 — App Shell (Dashboard · Board · Task · Team · Notifications)

## Type

NEW FEATURE

## What This Delivers

The five core app-shell surfaces from the Week-1 Figma screens (the GRD's entity map): Dashboard (Workspace/Project cards + activity feed), Board (columns + task cards), Task detail modal, Team settings, Notifications page — wired to the backend through features 03/04 hooks.

## Dependencies

- Web features 03, 04, 05 (data + auth).
- Backend features 04–07 (routes).

## Context To Read First

- `web/project-kit/context/{architecture,state-and-data}.md`
- `project-kit/context/ui-registry.md` (components)

## Files Owned

- `web/src/features/{dashboard,board,taskDetail,settings,notifications}/**`

## Implementation Notes

- Dashboard: `WorkspaceCard`, `ProjectCard`, `ActivityFeedItem` from registry.
- Board: `BoardColumn` + `TaskCard` (open modal), count badges, WIP limit display.
- Task detail: `TaskModal` with description, assignee, priority selector, due date, comments, attachments.
- Team settings: member list + roles + `InviteForm`.
- Notifications: list + read/unread + mark-all-read.
- Every surface ships EmptyState/LoadingSkeleton/ErrorState.

## Separation of Concerns

- Pages compose registry components + feature hooks; no business logic (that lives in backend services).

## Docker & Deploy

- No change.

## Out of Scope

Drag-drop interactions (feature 08), bell/feed polish (feature 09).

## Acceptance Criteria

- [ ] Five surfaces render the backend's real data
- [ ] CRUD works end-to-end (create project, task, comment)
- [ ] Empty/loading/error states present on all async surfaces


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
