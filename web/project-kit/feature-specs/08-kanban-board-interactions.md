# Feature 08 — Kanban Board Interactions (Drag-Drop)

## Type

NEW FEATURE

## What This Delivers

Board interactivity: html5/dnd-based drag-and-drop of task cards between columns with optimistic Apollo cache updates + REST persistence (`PATCH /tasks/{id}` order/status), column reorder, and the web-only status language.

## Dependencies

- Web feature 07 (board surface).
- Backend feature 06 (bulk + position contract).

## Context To Read First

- `ui-rules.md` (drag-drop is web-only; mobile uses a picker)
- `state-and-data.md` (optimistic updates)

## Files Owned

- `web/src/features/board/dnd/**`, `web/src/features/board/hooks/useMoveTask.ts`

## Implementation Notes

- No drag library unless polish demands it (research decision: start with native HTML5 DnD + pointer events; revisit in refinement).
- Optimistic reorder updates `ColumnId`/`Position` in the cache; on failure roll back + toast + refetch.
- `Position` is decimal; reordering renumbers within a column.

## Separation of Concerns

- DnD interactions in the board feature; persistence via `useMoveTask` (TanStack); cache policy via Apollo `typePolicies`.

## Docker & Deploy

- No change.

## Out of Scope

Mobile drag-drop (mobile uses a picker), multi-board drag.

## Acceptance Criteria

- [ ] Cards drag between columns with optimistic UI; failure rolls back cleanly
- [ ] Position/order persists via the API; tests cover reorder merging (no cache corruption)


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Project scope under org: every board lives under a project that carries `OrganizationId`; all DnD mutations ride the org-scoped REST path (no org field sent client-side — the JWT v2 `org` claim is authoritative).
- Client view never renders internal boards: the `Client` org role receives only the `ClientProjectViewDto` progress view (percent-complete, milestones, activity digest); board routes and column internals are guarded away for clients by route guards (web 05) and server-side (backend 34).
- Board access by role inside the company: Owner/Admin see ALL company projects' boards, PM only assigned projects, Member only their member boards — the UI mirrors the server boundary, never assumes it.
- Suspended org (`403 org_suspended`): drag/drop and reorder are disabled with a read-only banner; optimistic updates are not attempted while suspended.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
