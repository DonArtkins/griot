# Week 02 · Diagram 05C — Sequence: Bulk Status Update (Transaction Boundary)

**Master spec + Figma Make paste prompts.** The `PATCH /api/tasks/bulk-status` path — the stored-proc TVP transaction boundary from Week-2 §4. Drawn so the web drag-drop and the API validation agree on atomicity.

---

## 1. Lifelines

1. **Client** (board drag-drop multi-select / bulk action)
2. **TaskController** (`PATCH /api/tasks/bulk-status`)
3. **TaskService** (validation + orchestration)
4. **SQL Server** (`usp_BulkUpdateTaskStatus` + TVP)

## 2. The flow

```
Client → TaskController: PATCH /api/tasks/bulk-status { workspaceId, taskIds[], status }
TaskController → TaskService: BulkUpdateStatusAsync(workspaceId, ids, status)
TaskService: validate every id belongs to workspace (permissions)
TaskService → SQL Server: EXEC usp_BulkUpdateTaskStatus @WorkspaceId, @TaskIds (TVP), @Status
SQL Server (proc): BEGIN TRAN; UPDATE Tasks SET Status=@Status, UpdatedAt=SYSUTCDATETIME() WHERE Id IN (SELECT … TVP); COMMIT
SQL Server → TaskService: affected rows / ok
TaskService → SQL Server: INSERT ActivityLogs (bulk update)
TaskService → SQL Server: INSERT AuditLogs (before/after JSON of moved ids)
TaskService → Client: 200 { updatedCount, failedIds: [] }
alt partial failure
    proc rolls back WHOLE batch → 409/400 with error detail
```

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the bulk-update flow + rollback in one pass.

```text
UML sequence diagram: Griot bulk status update. Lifelines left→right: Client (board multi-select), TaskController (PATCH /api/tasks/bulk-status), TaskService (Griot.Application), SQL Server (usp_BulkUpdateTaskStatus + TVP).

FLOW:
Client → TaskController: PATCH /api/tasks/bulk-status { workspaceId, taskIds[], status }
TaskController → TaskService: BulkUpdateStatusAsync(workspaceId, ids, status)
TaskService: validate every id belongs to the workspace (alt invalid id → early 404/400, no DB call)
TaskService → SQL Server: EXEC usp_BulkUpdateTaskStatus @WorkspaceId, @TaskIds (TVP), @Status
SQL Server (proc):
    BEGIN TRAN
    UPDATE Tasks SET Status = @Status, UpdatedAt = SYSUTCDATETIME()
    WHERE Id IN (SELECT value FROM @TaskIds) AND WorkspaceId = @WorkspaceId
    COMMIT
SQL Server → TaskService: affected row count
TaskService → SQL Server: INSERT ActivityLogs (bulk update)
TaskService → SQL Server: INSERT AuditLogs (before/after JSON of moved ids)
TaskService → TaskController: 200 { updatedCount, failedIds: [] }

FAILURE ALT (red):
Any invalid id or update error → the ENTIRE proc transaction ROLLS BACK → response 409 { failedIds }
Draw a labeled BRACKET around the proc's BEGIN TRAN / UPDATE / COMMIT: "transaction boundary (all-or-nothing — no half-applied drag)"

ANNOTATION box:
"Atomicity contract: usp_BulkUpdateTaskStatus is a single transaction; if ANY id in the TVP is invalid, the ENTIRE batch rolls back. The service returns 409 with {failedIds}. Web drag-drop + mobile picker both rely on this — never a half-applied status."
```

### Refine

- "Make the rollback alt red-bordered; label 'any bad id → whole batch rolls back'."

---

## Definition of Done

- [ ] TVP proc call + transaction boundary drawn
- [ ] Validation + rollback alt (409) explicit
- [ ] ActivityLog + AuditLog writes included
- [ ] Approved → PNG → `project-kit/diagrams/architecture/sequence-bulk-status.png`