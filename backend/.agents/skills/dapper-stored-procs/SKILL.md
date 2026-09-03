---
name: dapper-stored-procs
description: "Dapper 2.x + SQL Server stored procedures for the documented hot paths only: bulk task status updates and the dashboard summary."
metadata:
  version: "0.1.0"
---

# Dapper + Stored Procedures Skill

## Contract

Exactly two stored procedures are permitted in v1 (bootcamp deliverables):

1. `usp_BulkUpdateTaskStatus` — TVP-driven, transactional, sets status + `UpdatedAt = SYSUTCDATETIME()`.
2. `usp_GetDashboardSummary` — one round-trip per workspace: task counts, urgent-open, recent activity.

## Rules

- Files live in `backend/src/Griot.Infrastructure/Sql/`, applied idempotently (`IF OBJECT_ID(...) IS NULL`).
- Always parameterized; `commandType: CommandType.StoredProcedure`; `NOCOUNT ON`.
- Called from `Griot.Infrastructure.Repositories` only; services consume repository interfaces.

```csharp
await _db.ExecuteAsync("dbo.usp_BulkUpdateTaskStatus",
    new { TaskIds = tvp, Status = "Done" }, commandType: CommandType.StoredProcedure);
```

## Verify

- Both procs callable with sample results; the bulk update is atomic (qa spec 05 covers it).
