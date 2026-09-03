# Feature 03 — Database Schema: EF Core + SQL Server (from the FigJam ERD)

## Type

NEW FEATURE

## What This Delivers

The approved FigJam ERD (`project-kit/diagrams/erd/`) transcribed into the EF Core data layer: entities + enums in `Griot.Domain`, the `GriotDbContext` mappings in `Griot.Infrastructure`, the `InitialCreate` migration applied to the SQL Server 2022 container, and the two Dapper stored procedures. After this feature, the database is the single source of truth every other feature reads from.

## Dependencies

- Feature 02 (containers up).
- **Approved ERD** from `project-kit/diagrams/erd/` — generated per `PROMPTS/week-02/01-database-schema-erd-figma.md` before any schema code.
- .NET 8 SDK per-repo + tool manifest (`research/gtp-2026-prep.md` §6.5).

## Context To Read First

- `context/architecture-context.md`
- `context/code-standards.md` (C# + T-SQL standards)
- `research/week-01-fundamentals-and-system-design.md` §4.4 (screen → entity table)
- `research/week-02-backend-api-development.md`

## Files Owned

- `backend/src/Griot.Domain/**` (entities + enums)
- `backend/src/Griot.Infrastructure/**` (`GriotDbContext`, repositories, migrations, `Sql/`)
- `backend/Griot.sln` + per-project `.csproj` files
- `backend/global.json`

## Files

CREATE: `backend/global.json` — pins SDK 8.0 (`--sdk-version 8.0.1xx`).
CREATE: `backend/Griot.sln` with projects `Griot.Api` (stub), `Griot.Application` (stub), `Griot.Domain`, `Griot.Infrastructure`.
CREATE: `Griot.Domain` entities — `User`, `Workspace`, `WorkspaceMember`, `Invite`, `Project`, `Board`, `Column`, `TaskItem`, `Comment`, `Attachment`, `ActivityLog`, `Notification`, `RefreshToken`.
CREATE: `Griot.Domain` enums — `TaskStatus`, `Priority`, `WorkspaceRole`, `NotificationType` (names must match the ERD + Figma annotation).
CREATE: `Griot.Infrastructure/GriotDbContext` — `DbSet`s + `OnModelCreating`: key types, `HasConversion<string>()` enums, `SYSUTCDATETIME()` timestamps, FK cascade rules, indexes.
CREATE: `Sql/usp_BulkUpdateTaskStatus.sql` — TVP-driven bulk status update in a transaction (Dapper call contract in Feature 04).
CREATE: `Sql/usp_GetDashboardSummary.sql` — one-round-trip dashboard summary per workspace.
RUN: `dotnet ef migrations add InitialCreate` then `dotnet ef database update`.
RUN: Apply stored procedures to the container.

## Setup / Initialization

```bash
cd backend
dotnet new globaljson --sdk-version 8.0.1xx --roll-forward latestFeature
dotnet new tool-manifest && dotnet tool install dotnet-ef
dotnet new sln -n Griot
dotnet new classlib -n Griot.Domain -o src/Griot.Domain
dotnet new classlib -n Griot.Application -o src/Griot.Application
dotnet new classlib -n Griot.Infrastructure -o src/Griot.Infrastructure
dotnet new webapi -n Griot.Api -o src/Griot.Api
dotnet sln add src/Griot.Domain src/Griot.Application src/Griot.Infrastructure src/Griot.Api
dotnet add src/Griot.Infrastructure reference src/Griot.Domain
dotnet add src/Griot.Api reference src/Griot.Application
dotnet add src/Griot.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/Griot.Infrastructure package Dapper
dotnet add src/Griot.Infrastructure package StackExchange.Redis
dotnet add src/Griot.Api package HotChocolate.AspNetCore
# connection string (local compose):
# Server=gtp-sqlserver,1433;Database=griot;User Id=sa;Password=${GTP_SA_PASSWORD};TrustServerCertificate=True
```

## Separation of Concerns

- `Griot.Domain` — entities + enums, **zero references**; enums shared verbatim by web (TS) and mobile (Dart) later.
- `Griot.Infrastructure` — persistence only: `DbContext`, EF repos, Dapper repos (`QueryAsync<T>("dbo.usp_…")`), Redis client, migrations, SQL scripts.
- `Griot.Application` — (stubbed here) will implement `ITaskRepository` etc.; never touches EF directly.
- `Griot.Api` — (stubbed here) hosting only.
- Every FK, key, and index in the ERD has exactly one mapping in `OnModelCreating` — no drift allowed.

## Docker & Deploy

- DB runs as the Feature-02 compose `sqlserver` service. Migrations run locally via `dotnet ef database update`.
- **Production**: migrations execute as the Railway **release command** (`dotnet ef database update`), never assumed from a local run (see Feature 09).
- The stored procedures are versioned in `src/Griot.Infrastructure/Sql/` and applied via idempotent `IF NOT EXISTS` guards.

## ERD Contract (from the approved diagram)

Entities, keys, relations, indexes, and enum values must equal the approved ERD. Changes to the ERD after implementation require re-approval and trigger the contracts sync gate across Feature 04–10 context/specs.

## Out of Scope

- REST/GraphQL endpoints (Feature 04), auth flows (Feature 04), seed data beyond a dev seed.
- Postgres schema (secondary/test only — no `GriotDbContext` targeting Postgres in v1).

## Acceptance Criteria

- [ ] `dotnet build` clean; `InitialCreate` applied to SQL Server (`SELECT name FROM sys.tables` in DB `griot`)
- [ ] All entities/enums from the approved ERD present with matching relationships
- [ ] Both stored procs created and callable via Dapper with a sample result
- [ ] Every FK indexed; query-path indexes per ERD
- [ ] Contracts synchronized in context files + later specs

## Future Modifications

- Feature 04 wires the DbContext + repos into the API and the `ai-agent` principal.
- Feature 07's MCP tools read/write through these entities via GraphQL only.