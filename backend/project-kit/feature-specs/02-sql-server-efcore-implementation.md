# Feature 02 — Implementation in SQL Server (EF Core 8 + migration)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Implementation in SQL Server"**: the approved ERD transcribed into SQL Server 2022 via EF Core 8 — `Griot.Domain` entities/enums, `GriotDbContext` mappings, and the `InitialCreate` migration applied to the `sababisha-sqlserver` container.

## Dependencies

- Feature 01 (approved ERD).
- Compose data stack up (infra spec 03).
- .NET 8 per-repo tooling (`research/gtp-2026-prep.md` §6.5).

## Context To Read First

- `backend/AGENTS.md` + `backend/project-kit/context/{architecture,data-layer}.md`
- `/.agents/skills/dotnet-ef-core/SKILL.md`
- `research/week-02-backend-api-development.md` §3

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- `backend/.agents/skills/sql-server-2022/SKILL.md`

## Files Owned

- `backend/Griot.sln`, `global.json`, per-project `.csproj`
- `backend/src/Griot.Domain/**`, `backend/src/Griot.Infrastructure/GriotDbContext.cs`, `Migrations/`

## Files

CREATE: solution + classlib projects (Domain, Application, Infrastructure) + webapi stub (Api).
CREATE: `Griot.Domain` entities + enums per the ERD.
CREATE: `GriotDbContext` with `OnModelCreating` mappings, string enums, Guid keys, timestamps, indexes.
CREATE: `backend/global.json` (`--sdk-version 8.0.1xx`).
RUN: `dotnet ef migrations add InitialCreate`; `dotnet ef database update`.

## Setup / Initialization

```bash
cd backend
dotnet new globaljson --sdk-version 8.0.1xx --roll-forward latestFeature
dotnet new tool-manifest && dotnet tool install dotnet-ef
dotnet new sln -n Griot
dotnet new classlib -n Griot.Domain -o src/Griot.Domain
… # see /.agents/skills/dotnet-ef-core for the full scaffold list
```

## Separation of Concerns

- `Griot.Domain`: entities + enums, **zero references**.
- `Griot.Infrastructure`: persistence only (DbContext, migrations).
- `Griot.Application`: stubbed now; Feature 04 wires services.

## Docker & Deploy

- Local: migration against `sababisha-sqlserver` (compose).
- Production: migrations as the Railway release command (infra spec 05), never assumed local.

## Out of Scope

REST/GraphQL/auth (specs 04–05, 08); stored procs (spec 03); Postgres schema.

## Future Modifications

- Spec 03 adds procs; spec 04 wires services/repos.

## Acceptance Criteria

- [ ] `dotnet build` clean; `InitialCreate` applied; table list matches the ERD
- [ ] Enums/entities names match the approved ERD exactly
- [ ] Indexes match the ERD annotations
