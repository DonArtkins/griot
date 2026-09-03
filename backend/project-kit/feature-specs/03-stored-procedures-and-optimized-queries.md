# Feature 03 — Stored Procedures & Optimized Queries (Dapper)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Stored procedures & optimized queries"**: `usp_BulkUpdateTaskStatus` (TVP bulk status update, transactional) and `usp_GetDashboardSummary` (one-round-trip dashboard), versioned under `Griot.Infrastructure/Sql/` and invoked through Dapper repository methods.

## Dependencies

- Feature 02 (schema + tables exist).

## Context To Read First

- `data-layer.md` (proc contracts + indexes)
- `research/week-02-backend-api-development.md` §4

## Agent Skills To Use

- `backend/.agents/skills/dapper-stored-procs/SKILL.md`
- `backend/.agents/skills/sql-server-2022/SKILL.md`

## Files Owned

- `backend/src/Griot.Infrastructure/Sql/{usp_BulkUpdateTaskStatus,usp_GetDashboardSummary}.sql`
- `backend/src/Griot.Infrastructure/Repositories/TaskRepository.cs`, `DashboardRepository.cs`

## Files

CREATE: the two proc SQL files (idempotent creation).
CREATE: Dapper repository methods (parameterized, `CommandType.StoredProcedure`).
RUN: apply procs to `gtp-sqlserver`; smoke-test both.

## Setup / Initialization

```bash
cd backend
# ensure Dapper is referenced:
dotnet add src/Griot.Infrastructure package Dapper
# apply procs (idempotent):
docker exec -i gtp-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$GTP_SA_PASSWORD" -C < src/Griot.Infrastructure/Sql/usp_BulkUpdateTaskStatus.sql
```

## Implementation Notes

- Bulk update: table-valued parameter of task ids, status, one UPDATE over the join, `BEGIN TRAN`/`COMMIT`, `SYSUTCDATETIME()` on `UpdatedAt`.
- Dashboard: single round-trip: task counts by status, urgent-open count, recent activity head.
- Every raw SQL call beyond these two procs is forbidden in v1 (EF covers the remaining 95%).

## Separation of Concerns

- SQL files + Dapper calls live in `Griot.Infrastructure` only.
- Services see `ITaskRepository.BulkUpdateStatusAsync(...)` — no SQL knowledge in `Griot.Application`.

## Docker & Deploy

- Apply to any environment as part of migration/release (infra owns the release command; procs are idempotent so they can re-run).

## Out of Scope

More procs, query tuning beyond the dashboard, EF-to-Dapper parity tooling.

## Future Modifications

- Spec 06 exposes the bulk operation over REST `/api/tasks/bulk-status`.

## Acceptance Criteria

- [ ] Both procs exist and run with sample data
- [ ] Bulk update is atomic (all-or-nothing)
- [ ] Dashboard returns within one round-trip
