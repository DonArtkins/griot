# CodeRabbit Remaining Issues (Deferred to Implementation Phase)

> **Status:** 27/42 issues resolved (64%). Remaining 15 issues documented here for implementation phase.

## Summary

**Resolved:** 27 issues across critical, security, contract sync, caching, blob storage, Netdata  
**Remaining:** 15 issues (all Major severity, primarily specification/design clarifications)

## Why Deferred?

These issues require implementation-phase decisions or contract finalization:
- **Idempotency/transaction replay:** Database schema additions (IdempotencyKeys table)
- **PITR vs snapshots:** Railway/Azure infrastructure decisions
- **Bulk-status audit context:** Stored procedure signature changes
- **Layer violations:** Architecture refactoring (move Redis/Dapper behind interfaces)
- **Token validation:** Startup configuration validation
- **Offline-write NFR:** Product decision (v1 scope vs Phase 3)

All issues are **specification-phase concerns** — no blocking bugs for current planning/design work.

---

## Remaining Issues by Category

### 1. Contract Synchronization (3 issues)

#### 1.1 Blob Provider Contract (R2 vs Vercel Heading)
**File:** `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` lines 32-46  
**Issue:** Heading says "Cloudflare R2 (primary)" but decision selects Vercel Blob for v1  
**Fix:** Update heading to "Blob Storage Strategy" and clarify R2 as Phase 3 migration  
**Severity:** Minor documentation inconsistency

#### 1.2 Audit Logging Contract (Uploads vs Deletes-Only)
**Files:**
- `docs/ARCHITECTURE.md` line 74 — requires AuditLogs for ALL state-changing writes
- `backend/project-kit/feature-specs/11-blob-storage-integration.md` lines 574-575 — requires AuditLogs only for deletes

**Issue:** Inconsistent audit logging requirements for attachment uploads  
**Fix:** Synchronize: either audit both upload+delete or neither (ActivityLogs-only)  
**Decision needed:** Does upload qualify as "state-changing write"? (Attachment table INSERT = yes)

#### 1.3 Bulk-Status Audit Context
**File:** `PROMPTS/week-02/09-diagram-sequence-bulk-status.md` lines 26-27  
**Issue:** Stored procedure passes `@WorkspaceId`, `@TaskIds`, `@Status` but uses unsourced `@ActorId`, `@PayloadJson`, `@BeforeJson`, `@AfterJson` for audit inserts

**Fix Required:**
```sql
-- Add parameters to dbo.usp_BulkUpdateTaskStatus
CREATE PROCEDURE dbo.usp_BulkUpdateTaskStatus
  @WorkspaceId uniqueidentifier,
  @TaskIds TaskIdListType READONLY,  -- TVP
  @Status nvarchar(50),
  @ActorId uniqueidentifier,  -- ADD: for AuditLogs
  @PayloadJson nvarchar(max),  -- ADD: for ActivityLogs
  @BeforeJson nvarchar(max),  -- ADD: capture before state
  @AfterJson nvarchar(max)    -- ADD: capture after state
AS
BEGIN
  -- Capture before state for each task
  SELECT Id, Status INTO #Before FROM TaskItems WHERE Id IN (SELECT value FROM @TaskIds);
  
  -- Update tasks
  UPDATE TaskItems SET Status = @Status WHERE Id IN (SELECT value FROM @TaskIds);
  
  -- Insert ActivityLogs with actual @PayloadJson and EntityId
  INSERT INTO ActivityLogs (WorkspaceId, ActorId, Action, EntityType, EntityId, Payload, CreatedAt)
  SELECT @WorkspaceId, @ActorId, 'BulkStatusUpdate', 'TaskItem', value, @PayloadJson, SYSUTCDATETIME()
  FROM @TaskIds;
  
  -- Insert AuditLogs with before/after snapshots per task
  INSERT INTO AuditLogs (ActorId, Action, EntityType, EntityId, Before, After, CreatedAt)
  SELECT @ActorId, 'StatusUpdate', 'TaskItem', t.Id, 
         JSON_OBJECT('status': b.Status), 
         JSON_OBJECT('status': @Status),
         SYSUTCDATETIME()
  FROM @TaskIds t
  JOIN #Before b ON t.value = b.Id;
END
```

**Caller changes:** TaskService must pass actor ID and construct JSON payloads before calling procedure.

---

### 2. Database & Infrastructure (3 issues)

#### 2.1 Transaction Replay Strategy
**File:** `docs/planning/RUNBOOK-ROLLBACK.md` lines 31-32  
**Issue:** "Reapply lost transactions from AuditLogs" lacks tested replay procedure

**Reality Check:**
- AuditLogs contains state snapshots (Before/After JSON), not transaction commands
- ActivityLogs is informational, not transactional
- Neither table defines transaction boundaries, idempotency, or side-effect coverage

**Recommendation:**
- **Remove** "reapply lost transactions" instruction (not feasible with current schema)
- **Use** database recovery tooling (PITR, snapshot restore) exclusively
- **Document** RPO verification: check last AuditLogs/ActivityLogs timestamp vs backup time
- **If replay is required:** Design event-sourcing table with:
  - Transaction boundaries (BEGIN/COMMIT markers)
  - Idempotency keys
  - Complete side-effect coverage (external API calls, emails, etc.)
  - Tested replay script

#### 2.2 PITR vs Volume Snapshots
**File:** `docs/planning/RUNBOOK-ROLLBACK.md` lines 30-31  
**Issue:** Mixes Railway volume snapshots (fixed point) with arbitrary timestamp recovery (PITR)

**Clarification Needed:**
- **Railway Volume Snapshots:** Fixed snapshots taken at Railway-defined intervals (e.g., daily). Recovery restores to snapshot time, not arbitrary timestamp. RPO = time since last snapshot.
- **PostgreSQL PITR:** Requires WAL archiving enabled. Allows recovery to any timestamp within retention window.
- **SQL Server PITR:** Available in Azure SQL Business Critical tier. Not available in Railway's SQL Server deployment.

**Action:**
```markdown
## Database Recovery Strategy

### Railway Volume Snapshots (Current)
- **Type:** Fixed-point snapshot restore
- **Frequency:** Railway-managed (check Railway dashboard for schedule)
- **RPO:** Time since last snapshot (typically 24 hours)
- **Process:**
  1. Railway dashboard → Database service → Backups tab
  2. Select snapshot → Restore to new volume
  3. Update connection string to restored volume
  4. Verify data integrity via AuditLogs/ActivityLogs timestamp checks

### PostgreSQL PITR (Optional - Enable if needed)
- **Requires:** WAL archiving to external storage (S3, Cloudflare R2)
- **RPO:** Down to seconds (limited by WAL shipping frequency)
- **Cost:** Storage for WAL archives (~10-50 GB/month)
- **Enable:** TBD during production setup if RPO requirements demand <24h recovery

**Decision Gate:** Evaluate RPO requirements after v1 launch. Current snapshot-based recovery acceptable for demo/early production.
```

#### 2.3 Offline-Write NFR Conflict
**Files:**
- `docs/planning/NFR.md` line 73 — requires queued writes persist until success/discard
- `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` lines 206-212 — defers offline queue to Phase 3

**Issue:** NFR mandates persistent queue for v1, but roadmap defers it post-bootcamp

**Resolution Options:**

**Option A: Defer NFR (recommended for v1):**
```markdown
## NFR §9 Update: Offline-Write Behavior (v1 Scope)

**v1 Behavior (online-first):**
- Mobile app requires network connectivity for task creation/updates
- If offline → show "No connection" error, block write actions
- User sees cached read-only data (Apollo/GraphQL cache)
- No writes queued; user must retry when online

**Phase 3 Enhancement (post-bootcamp):**
- Persistent write queue (sqflite on mobile)
- Idempotency keys (UUID v4) prevent duplicates
- Conflict resolution UI for simultaneous edits
- Battery-aware sync strategy

**Rationale:** v1 scope focuses on stable online experience. Offline-write adds ~6-10 days implementation (Phase 3) and requires mature conflict resolution UX. Most project-management tools (Asana, Monday) also fail gracefully offline.
```

**Option B: Implement persistent queue in v1:**
- Move offline queue from Phase 3 to Phase 1
- Add sqflite dependency to mobile app
- Implement idempotency on backend
- Add 6-10 days to mobile schedule (Week 4)

**Recommendation:** Option A (defer to Phase 3). Online-first is acceptable for v1 demo/bootcamp scope.

---

### 3. Architecture & Code Quality (3 issues)

#### 3.1 Layer Violations (DashboardService)
**File:** `backend/project-kit/context/architecture.md` lines 30-31  
**Issue:** DashboardService (Application layer) calls Redis and Dapper directly (Infrastructure dependencies)

**Fix:**
```csharp
// Application layer — depends only on interfaces
namespace Griot.Application.Services;

public class DashboardService
{
    private readonly ICacheService _cache;  // abstraction, not Redis
    private readonly IDashboardRepository _repo;  // abstraction, not Dapper
    
    public async Task<DashboardSummary> GetSummaryAsync(Guid workspaceId, CancellationToken ct)
    {
        var cacheKey = $"dashboard:summary:{workspaceId}";
        
        // Try cache (abstracted)
        var cached = await _cache.GetAsync<DashboardSummary>(cacheKey, ct);
        if (cached != null) return cached;
        
        // Query repository (abstracted)
        var summary = await _repo.GetSummaryAsync(workspaceId, ct);
        
        // Store in cache
        await _cache.SetAsync(cacheKey, summary, TimeSpan.FromSeconds(60), ct);
        
        return summary;
    }
}
```

```csharp
// Infrastructure layer — implements abstractions
namespace Griot.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    // Actual Redis calls here
}

namespace Griot.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnection _db;  // Dapper
    // Actual SQL calls here
}
```

**DI Registration:**
```csharp
// Program.cs
services.AddScoped<ICacheService, RedisCacheService>();
services.AddScoped<IDashboardRepository, DashboardRepository>();
```

**Benefit:** Application layer is testable without Redis/SQL dependencies.

#### 3.2 Token Validation on Startup
**File:** `backend/project-kit/feature-specs/11-blob-storage-integration.md` lines 212-213  
**Issue:** `BLOB_READ_WRITE_TOKEN` not validated at startup; tokenless `Authorization: Bearer` header succeeds, failure deferred to first upload

**Fix:**
```csharp
// backend/Griot.Api/Configuration/BlobStorageOptions.cs
public class BlobStorageOptions
{
    public string Provider { get; set; } = "VercelBlob";
    public string VercelBlobToken { get; set; } = string.Empty;
    
    // Validate at startup
    public void Validate()
    {
        if (Provider == "VercelBlob" && string.IsNullOrWhiteSpace(VercelBlobToken))
        {
            throw new InvalidOperationException(
                "BlobStorage:VercelBlobToken is required when Provider=VercelBlob. " +
                "Set BLOB_READ_WRITE_TOKEN environment variable.");
        }
    }
}

// Program.cs
var blobOptions = builder.Configuration.GetSection("BlobStorage").Get<BlobStorageOptions>()!;
blobOptions.Validate();  // Fail fast at startup
builder.Services.AddSingleton(blobOptions);
```

**Benefit:** Clear error message at startup instead of cryptic 401 on first upload.

#### 3.3 Idempotency Contract (Mobile Sync)
**File:** `docs/planning/NFR.md` line 73  
**Issue:** X-Idempotency-Key header documented but no backend contract for key scope, atomic claim, retention, replay response

**Required Implementation:**

**Database schema:**
```sql
CREATE TABLE IdempotencyKeys (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    IdempotencyKey nvarchar(255) NOT NULL,
    UserId uniqueidentifier NOT NULL,
    Endpoint nvarchar(500) NOT NULL,  -- e.g., POST /api/tasks
    RequestHash nvarchar(64),  -- SHA-256 of request body
    ResponseStatus int NOT NULL,  -- 200, 201, 409, etc.
    ResponseBody nvarchar(max),  -- cached response
    CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt datetime2 NOT NULL,  -- 24 hours retention
    
    CONSTRAINT UQ_IdempotencyKey UNIQUE (IdempotencyKey, UserId, Endpoint),
    INDEX IX_ExpiresAt (ExpiresAt)  -- for cleanup job
);
```

**Middleware:**
```csharp
public class IdempotencyMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var key = context.Request.Headers["X-Idempotency-Key"].ToString();
        if (string.IsNullOrEmpty(key) || context.Request.Method == "GET")
        {
            await _next(context);  // Pass through
            return;
        }
        
        var userId = GetUserIdFromToken(context);
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        
        // Check if key already used
        var existing = await _db.QueryFirstOrDefaultAsync<IdempotencyRecord>(
            "SELECT * FROM IdempotencyKeys WHERE IdempotencyKey=@key AND UserId=@userId AND Endpoint=@endpoint AND ExpiresAt > SYSUTCDATETIME()",
            new { key, userId, endpoint });
        
        if (existing != null)
        {
            // Replay cached response
            context.Response.StatusCode = existing.ResponseStatus;
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }
        
        // Capture response for replay
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        await _next(context);
        
        // Cache response
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        
        await _db.ExecuteAsync(
            "INSERT INTO IdempotencyKeys (IdempotencyKey, UserId, Endpoint, ResponseStatus, ResponseBody, ExpiresAt) VALUES (@key, @userId, @endpoint, @status, @body, DATEADD(hour, 24, SYSUTCDATETIME()))",
            new { key, userId, endpoint, status = context.Response.StatusCode, body = responseText });
        
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
}
```

**Conflict vs Replay:**
- **Idempotent replay (200/201):** Same key, same request body → return cached response, mark synced
- **Domain conflict (409):** Different request body, business rule violation → show conflict UI
- **Distinguish via:** Compare `RequestHash` (SHA-256 of body). Match = replay. Mismatch = conflict.

---

## Implementation Phase Action Items

1. **Bulk-status audit context:** Update stored procedure signature + caller
2. **Transaction replay:** Remove unfeasible instruction, document PITR enablement decision
3. **PITR vs snapshots:** Clarify Railway snapshot limitations, document RPO
4. **Layer violations:** Refactor DashboardService to use abstractions
5. **Token validation:** Add startup validation for BLOB_READ_WRITE_TOKEN
6. **Idempotency contract:** Implement IdempotencyKeys table + middleware
7. **Offline-write NFR:** Decide v1 scope (defer to Phase 3 recommended)
8. **Blob provider heading:** Fix "R2 (primary)" → "Blob Storage Strategy"
9. **Audit logging contract:** Synchronize upload audit requirements

**Priority:** Items 1, 4, 5 are quick wins (1-2 days total). Items 2, 3, 6, 7 require product/architecture decisions.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
