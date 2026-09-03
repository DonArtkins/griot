# Backend Data Layer

## Entities (from the approved Figma Make ERD — names are contracts)

`Users` · `Workspaces` · `WorkspaceMembers` (M:N join with `Role`) · `Invites` · `Projects` · `Boards` · `Columns` · `TaskItems` · `Comments` · `Attachments` · `ActivityLogs` (feed + AI audit) · `Notifications` · `RefreshTokens`.

## Enums

- `TaskStatus`: Backlog · Todo · InProgress · InReview · Done
- `Priority`: Low · Medium · High · Urgent
- `WorkspaceRole`: Owner · Admin · Member
- `NotificationType`: Mention · Assignment · DueDate · System

## EF Core 8 mapping (`Griot.Infrastructure`)

- `GriotDbContext` maps everything in `OnModelCreating`; enums via `HasConversion<string>()`.
- `Guid` PKs; timestamps from `SYSUTCDATETIME()` in `SaveChangesAsync`.
- Cascade rules: deleting a Task removes its Comments/Attachments; deleting a Workspace removes its members.

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
