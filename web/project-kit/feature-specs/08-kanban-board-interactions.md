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
