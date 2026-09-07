# Feature 06 — Bulk Operations & Advanced Data Handling

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Bulk operations & advanced data handling"**: expose `usp_BulkUpdateTaskStatus` as `PATCH /api/tasks/bulk-status` (TVP-driven, transactional), plus the advanced data conventions — batch comments/attachments listing, dashboard summary via `usp_GetDashboardSummary`, and pagination/ordering discipline.

## Dependencies

- Feature 03 (procs exist).
- Feature 04 (task + dashboard routes exist).

## Context To Read First

- `data-layer.md` (proc contracts) + `api-surface.md`

## Agent Skills To Use

- `backend/.agents/skills/dapper-stored-procs/SKILL.md`

## Files Owned

- `backend/src/Griot.Api/Controllers/TaskController.cs` (bulk-status action)
- `backend/src/Griot.Infrastructure/Repositories/TaskRepository.cs` (bulk method)
- `backend/src/Griot.Application/TaskService.cs` (bulk orchestration + validation)

## Files

MODIFY: `TaskRepository` — `BulkUpdateStatusAsync(workspaceId, taskIds, status)` using TVP + `usp_BulkUpdateTaskStatus`.
MODIFY: `TaskService` — validate all ids belong to the workspace; atomic failure semantics.
MODIFY: `TaskController` — `PATCH /api/tasks/bulk-status`.
CREATE: pagination helper (cursor or skip/take) for task lists + comments (constant page size).

## Setup / Initialization

```bash
dotnet build  # after changes
# smoke: create tasks, PATCH bulk-status, verify atomic all-or-nothing
```

## Implementation Notes

- Bulk update is transactional at the proc level; a bad id in the set fails the whole batch.
- Dashboard summary is exactly one round-trip.
- Pagination: fixed page sizes, stable ordering `(ColumnId, Position)` for tasks.

## Separation of Concerns

- TVP/SQL lives in `Griot.Infrastructure`; orchestration in `Griot.Application`; HTTP in `Griot.Api`.

## Docker & Deploy

- No new container; released with the backend image (infra).

## Out of Scope

Bulk moves across boards, multi-type bulk ops, soft-batch queueing.

## Future Modifications

- qa spec 05 asserts bulk atomicity; web drag-drop uses ordered `PATCH /tasks/{id}` not bulk.

## Acceptance Criteria

- [ ] `PATCH /api/tasks/bulk-status` works and is atomic
- [ ] Dashboard summary endpoint returns single-round-trip data
- [ ] Task/comment lists paginated with stable order


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
