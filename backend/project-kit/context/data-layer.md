# Backend Data Layer

## Entities (from the approved Figma Make ERD — names are contracts)

**Core (13):** `Users` · `Workspaces` · `WorkspaceMembers` (M:N join with `Role`) · `Invites` · `Projects` · `Boards` · `Columns` · `TaskItems` · `Comments` · `Attachments` · `ActivityLogs` (feed + AI audit) · `Notifications` · `RefreshTokens`

**Observability & audit (3) — added in planning so the schema never changes later:** `ApiLogs` · `ErrorLogs` · `AuditLogs`

## Enums

- `TaskStatus`: Backlog · Todo · InProgress · InReview · Done
- `Priority`: Low · Medium · High · Urgent
- `WorkspaceRole`: Owner · Admin · Member
- `NotificationType`: Mention · Assignment · DueDate · System
- `ErrorFixStatus`: Open · Investigating · Fixed · Verified · WontFix (for `ErrorLogs`)

## Observability/audit semantics (Lyncxs conventions)

- **ApiLogs**: one row per API/GraphQL request; `RequestId` correlates to `ErrorLogs` and web/mobile client traces; `DurationMs` feeds the k6/performance baseline; IP masked per privacy.
- **ErrorLogs**: every handled+unhandled exception row; lifecycle `Open → Investigating → Fixed → Verified` (`WontFix` valid); `FixedAt` supports pruning/retention.
- **AuditLogs**: before/after JSON snapshots on every state-changing write (task move, role change, delete); the Week-6/7 OWASP + compliance trail. Linked (optionally) to `ActivityLogs` for the human-readable feed.

## EF Core 8 mapping (`Griot.Infrastructure`)

- `GriotDbContext` maps everything in `OnModelCreating`; enums via `HasConversion<string>()`.
- `Guid` PKs; timestamps from `SYSUTCDATETIME()` in `SaveChangesAsync`.
- Cascade rules: deleting a Task removes its Comments/Attachments; deleting a Workspace removes its members.
- `ApiLogs`/`ErrorLogs`/`AuditLogs` are written via the same pipeline (middleware + services) and are NOT user-cascadable.

## Dapper 2.x (stored-proc hot paths only)

- `usp_BulkUpdateTaskStatus(workspaceId, @TaskIds TVP, @Status)` — transactional bulk status.
- `usp_GetDashboardSummary(workspaceId)` — one round-trip dashboard query.
- SQL files in `src/Griot.Infrastructure/Sql/`; called from `Repositories/` only.

## Indexes (from the ERD)

- Every FK indexed. Composite: `TaskItems(ColumnId, Position)`, `Notifications(UserId, ReadAt)`, `ActivityLogs(WorkspaceId, CreatedAt DESC)`. Unique: `Users.Email`, `Invites.Token`, `RefreshTokens.TokenHash`, `Workspaces.Slug`.

## Stores

| Store | Role | Port | Notes |
|---|---|---|---|
| SQL Server 2022 | primary | 14333 | EF + Dapper + procs |
| PostgreSQL 16 | secondary/test | 5433 | cohort exercises |
| Redis 7 | auth support | 6380 | rate limit, refresh metadata, token budgets |

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

`RefreshTokens.FamilyId` is a required GUID with composite index
`IX_RefreshTokens_UserId_FamilyId`. `AddRefreshTokenFamilyId` preserves existing
linked chains by assigning their root IDs before requiring the column. See the
linked auth contract for migration and real SQL Server regression tests.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
