# Backend Feature Spec 15 — Tasks & Comments

## Goal
Full task lifecycle (list/create/get/update/delete/move) + comments, scoped to the
containing workspace through the board/column hierarchy. The existing bulk-status
stored-procedure path (feature 06) is preserved.

## Depends
- Spec 14 (boards/columns) — task columns must belong to the board
- Spec 06 (bulk-status TVP) — unchanged

## Routes
- `GET|POST /api/boards/{id}/tasks` · `GET|PUT|DELETE /api/tasks/{id}` · `PATCH /api/tasks/{id}/move`
- `PATCH /api/tasks/bulk-status` (existing)
- `GET|POST /api/tasks/{id}/comments` · `PUT|DELETE /api/tasks/{id}/comments/{commentId}`

## Acceptance (implemented)
- [x] Task CRUD + move (same-board column validation) + position
- [x] Comments add/list/update/delete behind task visibility (author-only update/delete)
- [x] `TaskItems.BoardId` denormalized (migration `20260910082854_AddTaskItemBoardId`, backfilled from `Columns.BoardId`); kept in sync on create/move
- [x] Bulk-status still delegates to `ITaskRepository` (TVP)
- [x] No 501 across tasks/comments
