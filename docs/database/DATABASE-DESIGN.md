# Database Design — Griot (SQL Server 2022)

> The complete, production-ready schema decided **before implementation** so we never come back to redesign. Names/values = contract across backend, web, mobile, AI, MCP. Built from the Figma Make ERD + the Lyncxs knowledge-base database conventions.

Full diagram source: `PROMPTS/week-02/01-database-schema-erd-figma-make.md` + `02-erd-figma-make-master-prompt.md` (single extensive master prompt — no length limit).

---

## 1. Stores

| Store | Role | Notes |
|---|---|---|
| SQL Server 2022 | primary source of truth | EF Core 8 (95% CRUD) + Dapper 2.x (2 procs) |
| PostgreSQL 16 | secondary / test | cohort exercises, alternate-engine discussion |
| Redis 7 | runtime support | rate limit, refresh metadata, AI token budgets |

## 2. Tables (16) & enums (5)

### Core (13)
`Users` · `Workspaces` · `WorkspaceMembers` (M:N join + Role) · `Invites` · `Projects` · `Boards` · `Columns` · `TaskItems` · `Comments` · `Attachments` · `ActivityLogs` (feed + AI audit) · `Notifications` · `RefreshTokens`

### Observability & audit (3 — added in planning so schema never changes)
- **ApiLogs**: `Id`,`RequestId`(UQ),`UserId?`,`Method`,`Path`,`QueryString?`,`StatusCode`,`DurationMs`,`UserAgent?`,`IpAddress?`(masked),`CreatedAt`
- **ErrorLogs**: `Id`,`RequestId?`,`UserId?`,`ExceptionType`,`Message`,`StackTrace?`,`Source?`,`FixStatus`(enum),`SolvedByUserId?`,`FixedAt?`,`CreatedAt`
- **AuditLogs**: `Id`,`ActivityId?`(→ActivityLogs),`ActorId`,`Action`,`EntityType`,`EntityId`,`Before`(JSON?),`After`(JSON?),`CreatedAt`

### Enums
`TaskStatus` (Backlog·Todo·InProgress·InReview·Done) · `Priority` (Low·Medium·High·Urgent) · `WorkspaceRole` (Owner·Admin·Member) · `NotificationType` (Mention·Assignment·DueDate·System) · `ErrorFixStatus` (Open·Investigating·Fixed·Verified·WonTFix)

## 3. Key conventions (Lyncxs-informed)

- `UNIQUEIDENTIFIER` PKs (`Guid.NewGuid()` in app; `newsequentialid()` optional for hot tables).
- Timestamps `SYSUTCDATETIME()` (UTC) via `SaveChangesAsync` override; **no `GETDATE()`** (local-time bugs).
- Enums stored as `varchar` via `HasConversion<string>()` (readable SQL).
- `nvarchar(max)` only for bodies/JSON; everything else sized.
- Soft delete pattern: not used on core v1 (hard delete + AuditLog tombstone) — decided; revisit if restore needed.
- **Audit/error trails are append-only.**
- JSON fields only for naturally whole documents (`Payload`, `Before`, `After`, `QueryString`); never for relational data.

## 4. Indexes (from the ERD + Lyncxs query-shape discipline)

### Baseline indexes (v1 — shipped with schema)
- Every FK non-clustered.
- `TaskItems(ColumnId, Position)` — board read + drag hot path.
- `TaskItems(AssigneeId)`, `TaskItems(DueDate)` — my tasks + reminder agent.
- `ActivityLogs(WorkspaceId, CreatedAt DESC)` — feed + summarize_project.
- `Notifications(UserId, ReadAt)` — unread count.
- `RefreshTokens(UserId)`, `RefreshTokens(TokenHash)` UQ — rotation lookup.
- `Invites(Token)` UQ; `Invites(WorkspaceId, Email) WHERE Status = 'Pending'` filtered UQ — no duplicate pending invites.
- `ApiLogs(RequestId)` UQ; `ApiLogs(UserId, CreatedAt)`; `ApiLogs(Path)`.
- `ErrorLogs(FixStatus)` partial; `ErrorLogs(FixedAt)` pruning.
- `AuditLogs(ActorId)`, `AuditLogs(ActivityId)`.
- `Users.Email` UQ, `Workspaces.Slug` UQ.

### Phase 2 optimization indexes (add post-k6 baseline if p95 >500ms)
- **`TaskItems(BoardId, ColumnId, Position) INCLUDE (Title, Status, AssigneeId, DueDate)`** — covering index for board reads; eliminates key lookups; ~200ms → <100ms p95 improvement.
- **`ActivityLogs(CreatedAt DESC)`** — partition-ready for monthly pruning; supports time-range queries for observability.
- **`ApiLogs(CreatedAt DESC)`** — partition-ready; supports p95 latency analysis queries.
- **`ErrorLogs(FixStatus, CreatedAt)`** — filtered scans for open/investigating errors; error dashboard queries.

**Decision gate:** Add Phase 2 indexes only after k6 load tests prove p95 board reads exceed 500ms target under sustained load (500 concurrent users, 5 req/s/user for 10 min). See `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` §3.

## 5. Stored procedures (Dapper hot paths)

- `usp_BulkUpdateTaskStatus(@WorkspaceId, @TaskIds TVP, @Status)` — atomic bulk status update (transaction), `SYSUTCDATETIME()` on Update.
- `usp_GetDashboardSummary(@workspaceId)` — one-round-trip dashboard (counts, urgent-open, activity head). **Phase 1 optimization:** cached in Redis with 60s TTL (p95 400ms → 100ms).

Parametrized; `NOCOUNT ON`; idempotent file creation under `Sql/`.

### Read replica strategy (Phase 2 — deferred)
- **Trigger:** k6 evidence showing p95 latency targets missed after Phase 2 indexes are applied. DAU >2–3k is insufficient alone; read replica adds only when indexes + caching cannot meet NFRs.
- **Implementation:** Railway SQL Server readable secondary or Azure SQL geo-replica.
- **Routing:** EF Core read-only contexts point to replica connection string; writes stay on primary.
- **Benefit:** 10× read throughput; removes single-reader ceiling.
- **Cost:** ~$50–200/month (Railway) or ~$100–500/month (Azure SQL Business Critical).
- **Decision gate:** Add only after k6 evidence + Phase 2 indexes prove insufficient. See `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` §3.2.

## 6. Relationships & cardinality map

See ERD prompt file §3: WorkspaceMembers joins Users↔Workspaces (Role); Workspaces→Projects→Boards→Columns→TaskItems; TaskItems has TWO FKs to Users (Assignee? , Creator required); Comments/Attachments→TaskItems; Workspaces→ActivityLogs+Invites; Users→RefreshTokens (rotation chain)+Notifications+ApiLogs+ErrorLogs; ActivityLogs→(optional)AuditLogs.

## 7. Retention & pruning

- ApiLogs/ActivityLogs/AuditLogs: 90-day hot, archive/prune monthly.
- ErrorLogs: keep Open/Investigating; prune Fixed after 90 days (`FixedAt`).
- RefreshTokens: purge `ExpiresAt < now-7d` nightly.
- Job: nightly SQL agent job or scheduled task (documented in infra/qa).

## 8. The "don't come back later" guarantee

This file + the approved ERD are the **single schema contract**. A schema change re-enters through the ERD (Figma Make prompt G/H style), re-approval, then backend spec 02 + all dependent specs + type files + MCP schemas — via the contract-sync skill. Default state: no changes post-approval unless a tested fix is documented (CHANGE-MANAGEMENT.md).

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**