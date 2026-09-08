# Week 2 — Backend & API Development (ASP.NET Core, EF Core, SQL Server, HotChocolate)

> Guide's official Week 2 goals: DB schema design in Figma → implementation in SQL Server → stored procedures & optimized queries → REST APIs using .NET 8 → GraphQL layer via HotChocolate → bulk operations & advanced data handling → API testing using Postman.
> Adapted at **zero stack level**: this is the exact bootcamp backend. Only deployment detail differs (SQL Server runs as an official Linux container on Parrot).

---

## 1. Decisions & Rationale

**The backend project is a clean-room re-write to the bootcamp stack** — every decision below starts from the PDF's §3.1 line items and nothing else.

- **ASP.NET Core Web API (.NET 8, LTS)** — the server. Controllers (REST) and HotChocolate (GraphQL) share one process, one `GriotDbContext`, one set of DTOs. Target `net8.0`, C# 12. No Express anywhere.
- **Entity Framework Core 8** — object-relational mapping + migrations. Code-first (`dotnet ef migrations add …`), mapping the Week-1 Figma-derived entity list to SQL Server. This fully replaces the old Prisma draft.
- **Dapper 2.x** — the "stored procedures & optimized queries" workhorse: hand-tuned SQL and procs (bulk updates, dashboard summary) bypass EF and ride Dapper's `[sql]`-first path. EF for 95% of CRUD, Dapper for the 5% that needs raw SQL — exactly the pair the bootcamp names.
- **HotChocolate GraphQL 14+** — the GraphQL layer: types, queries, mutations, `DataLoader<,>` batch loading, filtering/sorting. Week 3's Apollo Client consumes this.
- **SQL Server 2022 (primary)** — the Week-2 deliverable database. Runs as `mcr.microsoft.com/mssql/server:2022-latest` on Parrot (no native Debian package; the image *is* SQL Server, not a fake). DBeaver/SSMS-compatible. All schema, procs, and queries in this doc target SQL Server T-SQL.
- **PostgreSQL 16 (secondary)** — also in the compose file per the guide's stack list: used as the **secondary/test** store (e.g., running the same schema in an alternate engine for cohort discussions, or as a scratch DB). The capstone's canonical DB is SQL Server.
- **Auth: `[own-stack]`** — custom JWT access/refresh implemented on ASP.NET Core Identity-free middleware (Argon2 + Redis). The guide avoids naming a vendor, so the mechanism is "whatever is encoded in-house, auditable in Week 6."
- **Redis** — `[own-stack]` rate limiting; SQL Server stores refresh tokens, compose service since Week 1.
- **Solution layout** is modular-but-pragmatic, single `.sln`:

```
src/
├── Griot.Api/                  # Web API (REST controllers + HotChocolate + Program.cs)
├── Griot.Application/          # services: taskService, workspaceService, projectService, boardService, commentService, notificationService, authService
├── Griot.Domain/               # entities + enums (TaskStatus, Priority, Role, NotificationType)
├── Griot.Infrastructure/       # GriotDbContext (EF Core 8), repositories (Dapper), Redis, migrations
└── Griot.sln
```

---

## 2. Prisma vs. EF Core — why this mapping is a straight swap

The old research planned Prisma; the bootcamp mandates EF Core. Everything the old doc wanted from the ORM — migrations-as-code, typed queries, raw-SQL escape hatch — EF Core + Dapper provide as a *cohort-standard* pairing:

| Worry | EF Core 8 + Dapper 2.x answer |
|---|---|
| Schema-as-source-of-truth | `GriotDbContext` + migration scripts committed to `src/Griot.Infrastructure/Migrations` |
| Raw SQL for hot paths | Dapper `QueryAsync<T>("dbo.usp_…")` — the guide's stored-procedure deliverable |
| Type-safe-ish queries | LINQ → strongly-typed `DbSet<T>` (C# types instead of TS types) |

## 3. EF Core schema (from Week-1's Figma entity table → SQL Server)

Entities are later transcribed from three services: they mirror the table from week-01 §4.4. Draft of the core model:

```csharp
// Griot.Domain/Entities/TaskItem.cs
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Backlog;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime? DueDate { get; set; }
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
    public Guid ColumnId { get; set; }
    public Column Column { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
```
- Enums persisted as string columns (`HasConversion<string>()`) so SSMS/DBeaver show readable values.
- Indexes mirror the "optimized queries" requirement: `IX_Task_ColumnId`, `IX_Task_Column_Status(DONE first)`, `IX_ActivityLog_Workspace_CreatedAt`.
- First migration: `dotnet ef migrations add InitialCreate` → `dotnet ef database update`.

## 4. Stored procedures via Dapper — the exact Week-2 deliverable

```sql
CREATE OR ALTER PROCEDURE dbo.usp_BulkUpdateTaskStatus
    @TaskIds   dbo.IdList READONLY,      -- table-valued param (or JSON OPENJSON)
    @Status    NVARCHAR(32)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
        UPDATE t SET t.[Status] = @Status, t.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Tasks t
        INNER JOIN @TaskIds ids ON ids.Id = t.Id;
    COMMIT;
END;
```
Called from `Griot.Infrastructure` with Dapper:
```csharp
await _db.ExecuteAsync("dbo.usp_BulkUpdateTaskStatus",
    new { TaskIds = tvp, Status = "Done" },
    commandType: CommandType.StoredProcedure);
```
> Second required proc: `usp_GetDashboardSummary(workspaceId)` → one round-trip for the Week-3 dashboard query (task counts, urgent-open, activity head). Both procs committed in `src/Griot.Infrastructure/Sql/`.

## 5. REST + GraphQL sharing one service layer (drift-proof)

- `Griot.Api/Controllers/*Controller.cs` — thin REST.
- `Griot.Api/GraphQL/` — HotChocolate `QueryType`/`MutationType`, `[DataLoader]` batch loaders for assignees/comments.
- Both call `Griot.Application.*Service` — zero business logic in controllers or resolvers.
- Config: `services.AddGraphQLServer().AddQueryType<GriotQuery>()…`, `.AddDataLoader()`; expose `/graphql` beside `/api`.

## 6. Auth (own-stack, no vendor) — built on ASP.NET Core primitives

- **Argon2** password hashing (`Konscious.Security.Cryptography`).
- **JWT access** tokens, 15-min TTL, claims `sub`/`email`/`jti`, signed by an env-provided key.
- **Opaque refresh tokens**, stored **hashed** in SQL Server (`RefreshTokens` table), **rotated on use** (old revoked, new issued) — the pattern Week-6's OWASP pass verifies.
- **Redis sliding-window rate limit** on `/api/auth/login` and a query-cost guard on `/graphql`.
- CORS allow-list set to the Vercel origin in prod, localhost dev.

### 6.5 AI-facing API surface (own-stack extension — see `ai-integration.md`)

- GraphQL + REST are the **only** way AI touches data. The Griot MCP server and the Trigger agents call existing endpoints with a `GRIOT_SERVICE_TOKEN` (Bearer header), which the pipeline resolves to a dedicated **"ai-agent" workspace member** with an explicit reduced role (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes, no invites). Same policy code as every member.
- New trusted-surface endpoint: `POST /api/webhooks/trigger` — accepts signed Trigger.dev webhooks (HMAC `X-Trigger-Signature` verified in middleware) so background runs can request non-LLM work (e.g. "build the digest") without talking to a model.
- The activity feed (Week-1 screen → `ActivityLog`) doubles as the AI layer's audit + `summarize_project` source.
- **Zero AI code in this solution.** The .NET app only exposes typed reads/writes; the intelligence is the separate Node project in the GTP tree.

## 7. Postman (guide deliverable)
One collection with `REST` + `GraphQL` folders, chained via environment vars (`baseUrl`, `accessToken`, `refreshToken`), exactly as planned. Postman natively runs GraphQL — one tool covers both layers. This collection is reused headlessly as Newman in Week 5/6 CI.

## 8. Definition of Done — Week 2
- [ ] SQL Server 2022 container reachable via DBeaver/SSMS-compatible tooling
- [ ] `Griot.sln` compiles: `dotnet build` clean
- [ ] EF Core `InitialCreate` migration applied to SQL Server
- [ ] Service layer + REST endpoints live for auth & all six modules
- [ ] HotChocolate schema (+ queries/mutations/DataLoaders) live at `/graphql`
- [ ] Stored procs `usp_*` created and invoked via Dapper
- [ ] Postman collection complete and reusable from CI
- [ ] `docker compose` runs the full backend stack (api + sqlserver + postgres + redis) in one command
- [ ] Service-token auth resolves to a restricted `ai-agent` principal; `/api/webhooks/trigger` verifies HMAC (AI layer, see `ai-integration.md`)

---
**Engineering Excellence. Production Mindset. Professional Impact.**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.
