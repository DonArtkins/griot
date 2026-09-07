# Week 02 · Diagram 05C — Sequence: Bulk Status Update (Transaction Boundary)

**Master spec + Figma Make paste prompts.** The `PATCH /api/tasks/bulk-status` path — the stored-proc TVP transaction boundary from Week-2 §4. Drawn so the web drag-drop and the API validation agree on atomicity.

> **Contract:** route + payloads + 409 shape = `backend/project-kit/context/api-surface.md` (mirrored in diagram 13, DFD 15 process P3). TVP type `dbo.TaskIdListType` (`TABLE (value uniqueidentifier)`); proc `dbo.usp_BulkUpdateTaskStatus(@WorkspaceId, @TaskIds READONLY, @Status, @ActorId, @PayloadJson)`. Any invalid id → entire batch rolls back.

---

## 1. Lifelines

1. **Client** (board drag-drop multi-select / bulk action)
2. **TaskController** (`PATCH /api/tasks/bulk-status`)
3. **TaskService** (validation + orchestration)
4. **SQL Server** (`usp_BulkUpdateTaskStatus` + TVP)

## 2. The flow

STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```
Client → TaskController: PATCH /api/tasks/bulk-status { workspaceId, taskIds[], status }
TaskController → TaskService: BulkUpdateStatusAsync(workspaceId, ids, status)
TaskService: validate every id belongs to workspace (permissions)
TaskService → SQL Server: EXEC usp_BulkUpdateTaskStatus @WorkspaceId, @TaskIds (TVP), @Status
SQL Server (proc): BEGIN TRAN
    -- Capture before state for audit (before UPDATE)
    SELECT Id, Status INTO #BeforeState FROM TaskItems WHERE Id IN (SELECT value FROM @TaskIds)
    
    UPDATE TaskItems SET Status=@Status, UpdatedAt=SYSUTCDATETIME() WHERE Id IN (SELECT value FROM @TaskIds) AND WorkspaceId=@WorkspaceId
    DECLARE @affected INT = @@ROWCOUNT
    DECLARE @expected INT = (SELECT COUNT(DISTINCT value) FROM @TaskIds)
    IF @affected != @expected THROW 50409, 'Invalid task ID in batch', 1  -- all-or-nothing validation
    
    -- INSERT ActivityLogs with sourced parameters
    INSERT INTO ActivityLogs (WorkspaceId, ActorId, Action, EntityType, EntityId, Payload, CreatedAt)
    SELECT @WorkspaceId, @ActorId, 'BulkStatusUpdate', 'TaskItem', value, @PayloadJson, SYSUTCDATETIME()
    FROM @TaskIds
    
    -- INSERT AuditLogs with before/after snapshots per task
    INSERT INTO AuditLogs (ActorId, Action, EntityType, EntityId, Before, After, CreatedAt)
    SELECT @ActorId, 'StatusUpdate', 'TaskItem', t.value,
           (SELECT Status FROM #BeforeState WHERE Id = t.value FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
           JSON_OBJECT('status': @Status),
           SYSUTCDATETIME()
    FROM @TaskIds t
    
    COMMIT
SQL Server → TaskService: ok / throw
TaskService → Client: 200 { updatedCount } OR 409 { error: 'Invalid task ID in batch' }

**Stored Procedure Signature:**
```sql
CREATE PROCEDURE dbo.usp_BulkUpdateTaskStatus
    @WorkspaceId uniqueidentifier,
    @TaskIds TaskIdListType READONLY,  -- TVP: TABLE(value uniqueidentifier)
    @Status nvarchar(50),
    @ActorId uniqueidentifier,  -- Caller's user ID for audit trail
    @PayloadJson nvarchar(max)  -- JSON with taskIds and new status for ActivityLogs
AS
-- (body as shown above with proper parameter usage)
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the bulk-update flow + rollback in one pass.

```text
UML sequence diagram: Griot bulk status update. Lifelines left→right: Client (board multi-select), TaskController (PATCH /api/tasks/bulk-status), TaskService (Griot.Application), SQL Server (usp_BulkUpdateTaskStatus + TVP).

FLOW:
Client → TaskController: PATCH /api/tasks/bulk-status { workspaceId, taskIds[], status }
TaskController → TaskService: BulkUpdateStatusAsync(workspaceId, ids, status)
TaskService: validate every id belongs to the workspace
TaskService → SQL Server: EXEC usp_BulkUpdateTaskStatus @WorkspaceId, @TaskIds (TVP), @Status
SQL Server (proc):
    BEGIN TRAN
    UPDATE TaskItems SET Status = @Status, UpdatedAt = SYSUTCDATETIME()
    WHERE Id IN (SELECT value FROM @TaskIds) AND WorkspaceId = @WorkspaceId
    DECLARE @affected INT = @@ROWCOUNT
    DECLARE @expected INT = (SELECT COUNT(DISTINCT value) FROM @TaskIds)
    IF @affected != @expected THROW 50409, 'Invalid task ID in batch', 1
    INSERT INTO ActivityLogs (WorkspaceId, ActorId, Action, EntityType, Payload, CreatedAt)
    VALUES (@WorkspaceId, @ActorId, 'BulkStatusUpdate', 'TaskItem', @PayloadJson, SYSUTCDATETIME())
    INSERT INTO AuditLogs (ActorId, Action, EntityType, EntityId, Before, After, CreatedAt)
    SELECT @ActorId, 'StatusUpdate', 'TaskItem', Id, @BeforeJson, @AfterJson, SYSUTCDATETIME() FROM @TaskIds
    COMMIT
SQL Server → TaskService: ok / throw
TaskService → TaskController: 200 { updatedCount } OR 409 { error: 'Invalid task ID in batch' }

FAILURE ALT (red):
If @affected != @expected → THROW → transaction ROLLS BACK (including UPDATE + all INSERTs) → response 409
Draw a labeled BRACKET around BEGIN TRAN to COMMIT: "atomic unit: status update + activity logs + audit logs"

ANNOTATION box:
"Atomicity contract: usp_BulkUpdateTaskStatus is a single transaction; if ANY id in the TVP is invalid, the ENTIRE batch rolls back. The service returns 409 { error: 'Invalid task ID in batch' }. Web drag-drop + mobile picker both rely on this — never a half-applied status."
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

### Refine

- "Make the rollback alt red-bordered; label 'any bad id → whole batch rolls back'."

---

## Definition of Done

- [ ] TVP proc call + transaction boundary drawn
- [ ] Validation + rollback alt (409) explicit
- [ ] ActivityLog + AuditLog writes included
- [ ] Approved → PNG → `diagrams/architecture/sequence-bulk-status.png`