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

## 3. Figma Make prompts (≤2000 chars)

### PROMPT A

```
UML sequence diagram: Griot bulk status update. Lifelines L→R: Client (board multi-select), TaskController (PATCH /api/tasks/bulk-status), TaskService (Griot.Application), SQL Server (usp_BulkUpdateTaskStatus + TVP).
Flow: Client→Controller PATCH {workspaceId, taskIds[], status}; Controller→TaskService BulkUpdateStatusAsync; TaskService validate ids belong to workspace (alt invalid id: 404/400 early); TaskService→SQL Server EXEC usp_BulkUpdateTaskStatus @WorkspaceId,@TaskIds TVP,@Status; SQL Server self: BEGIN TRAN; UPDATE Tasks SET Status=@Status,UpdatedAt=SYSUTCDATETIME() WHERE Id IN(SELECT value FROM @TaskIds); COMMIT; SQL Server→TaskService affected count; TaskService→SQL Server INSERT ActivityLogs bulk; INSERT AuditLogs before/after JSON; TaskService→Client 200 {updatedCount}. Add failure alt: any bad id → whole proc transaction rolls back (atomic), response 409. Label the proc step with bracket 'transaction boundary (all-or-nothing)'.
```

### PROMPT B — rollback annotation

```
On the Griot bulk-status sequence, add a red note: "Atomicity contract: usp_BulkUpdateTaskStatus is a single transaction; if ANY id in the TVP is invalid or the update fails, the ENTIRE batch rolls back. The service returns 409 with {failedIds} — the client never sees a half-applied drag." Connect it to the SQL Server lifeline.
```

---

## Definition of Done

- [ ] TVP proc call + transaction boundary drawn
- [ ] Validation + rollback alt (409) explicit
- [ ] ActivityLog + AuditLog writes included
- [ ] Approved → PNG → `project-kit/diagrams/architecture/sequence-bulk-status.png`