# Progress Tracker — Backend / API

## Current State

Week 2. Spec 05 is complete (GraphQL layer). Spec 06 (bulk operations & advanced data) is next.

| Spec | Title | Status |
|---|---|---|
| 01 | ERD & schema design (Figma/Figma Make) | ✅ Done |
| 02 | SQL Server implementation (EF Core) | ✅ Done |
| 03 | Stored procedures & optimized queries | ✅ Done |
| 04 | REST APIs (.NET 8) | ✅ Done |
| 05 | GraphQL layer (HotChocolate) | ✅ Done |
| 06 | Bulk operations & advanced data | Next |
| 07 | API testing (Postman) | Pending |
| 08 | Auth: JWT + Argon2 + Redis [own-stack] | Pending |
| 09 | AI service token + webhooks [own-stack] | Pending |
| 10 | API documentation | Pending |
| 11 | Blob storage integration | Pending |

## Next Steps

1. Implement spec 06 (bulk operations & advanced data) on branch `feature/backend/06-bulk-operations-advanced`.
2. Implement subsequent specs sequentially, each feature on its own branch.

## Session Notes

- **2026-09-03** — Kit restructured to one spec per bootcamp deliverable; 11 specs created.
- **2026-09-07** — Spec 02 completed: EF Core entities mapped to approved ERD, GriotDbContext configured, and initial migration generated.
- **2026-09-07** — Spec 03 completed: Added `usp_BulkUpdateTaskStatus` and `usp_GetDashboardSummary` stored procedures in `Griot.Infrastructure/Sql/`, added Dapper dependency, and created `TaskRepository` and `DashboardRepository` along with corresponding interfaces and DTOs in `Griot.Application`.
- **2026-09-07** — Spec 04 completed: Scaffolded REST API controllers corresponding to all routes in `api-surface.md`. Stubbed corresponding application services and DTOs.
- **2026-09-08** — Spec-04 `Program.cs` wiring gap resolved and committed on branch `feature/backend/04-rest-apis-dotnet8`: `Program.cs` now wires DI (controllers + `Griot.Application` services via interfaces), `GriotDbContext` (config-driven `ConnectionStrings:Default`, no hardcoded secrets), generic CORS policy, rate limiting, JWT bearer validation (config-driven `JWT:Key`), `/health` health checks, and a request-id middleware. Added `Microsoft.AspNetCore.Authentication.JwtBearer` to `Griot.Api.csproj`. Contract-synced `api-surface.md` (`GET /health` + `X-Request-Id` convention) and aligned `appsettings.Local.json` key to `ConnectionStrings:Default`. Verified: `dotnet build` green (0 warnings/errors) + live smoke (`/health` 200, `/api/workspaces` 200, Swagger UI). The stale WIP stash (`WIP on feature/backend/02`) was folded in and can now be dropped.
- **2026-09-08** — ⚠️ **DB objects were never applied; fixed + verified on the live dev stack**. `usp_BulkUpdateTaskStatus` + `usp_GetDashboardSummary` SQL files existed in repo but were never executed against SQL Server, and migration `20260907195152_AddOtpAndReports` was never applied. Applied both procs + `IdList` TVP type via sqlcmd to `localhost:14333/Griot`; ran `dotnet ef database update --project src/Griot.Infrastructure --startup-project src/Griot.Api` (applies `AddOtpAndReports`; `OtpChallenges` + `Reports` now present). Smoke-tested both procs with sample data inside a rolled-back transaction (dashboard counts + urgent-open correct; bulk update atomic + workspace-scoped). **Fix (contract-sync):** `GriotDbContextFactory` read `ConnectionStrings:DefaultConnection` while runtime + docs use `ConnectionStrings:Default` — corrected the factory to `Default`.
- **2026-09-08** — ⚠️ **Local dev bootstrap fixed** (branch `fix/backend/dev-bootstrap-config`). `dotnet watch run` from `backend/` failed twice: (1) no `.csproj` in `backend/` — projects live under `src/`, so run with `--project src/Griot.Api`; (2) even with `--project`, startup crashed with `ConnectionStrings:Default is not configured` because runtime config never loaded the git-ignored `appsettings.Local.json` (only the design-time factory did). `Program.cs` now calls `builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)` so local DB connection + JWT dev key load for `dotnet run`/`dotnet watch run` with zero env vars. Seeded dev-only `JWT:Key`/Issuer/Audience into `appsettings.Local.json` (git-ignored). Verified: `dotnet watch run --project src/Griot.Api` boots on `http://localhost:5064`; `/health` → 200; `/api/workspaces` → 200.
- **2026-09-08** — **Spec 05 completed** (branch `feature/backend/05-graphql-layer-hotchocolate`): Implemented HotChocolate 14+ GraphQL layer at `/graphql`. Created 13 GraphQL types (UserType, WorkspaceType, ProjectType, BoardType, TaskItemType, ColumnType, CommentType, NotificationGraphQLType, AttachmentType, ActivityLogType, DashboardSummaryType, NotificationCountType, AuthPayloadType) matching domain entities. Implemented GriotQuery with 11 queries (me, workspace, projects, board, tasks, task, comments, notifications, unreadNotificationCount, activityFeed, dashboardSummary) and GriotMutation with 24 mutations (createWorkspace, updateWorkspace, deleteWorkspace, createProject, updateProject, deleteProject, createBoard, createTask, updateTask, deleteTask, addComment — functional via direct DbContext; register, login, refresh, logout, inviteMember, acceptInvite, updateMember, removeMember, createColumn, updateColumn, deleteColumn, moveTask, bulkUpdateTaskStatus, addAttachment, markNotificationsRead — stubs throwing NotImplementedException for later specs). Added AssigneeDataLoader and CommentDataLoader for N+1 query batching. Configured filtering/sorting on tasks (status, priority, assignee, dueDate) and notifications (readAt, type, createdAt) with pagination (max 1000, default 100). Wired GraphQL server in `Program.cs` with DataLoaders, filtering, sorting, projections, authorization, and 30s execution timeout. Build green (0 errors). **Fix (54103ba):** Changed `AddDbContextFactory` to `AddPooledDbContextFactory` to resolve service lifetime mismatch (singleton factory cannot consume scoped `DbContextOptions`). Startup now works correctly.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
