---
name: dotnet-ef-core
description: "Entity Framework Core 8 code-first workflow on the Griot backend: entity mapping, migrations, SQL Server application. Use whenever schema or data-layer work changes."
metadata:
  version: "0.1.0"
---

# EF Core 8 Skill

## Per-repo setup (research/gtp-2026-prep.md §6.5)

```bash
cd backend
dotnet new globaljson --sdk-version 8.0.1xx --roll-forward latestFeature
dotnet new tool-manifest && dotnet tool install dotnet-ef
```

## Mapping rules

- Entities live in `Griot.Domain`; `GriotDbContext` (in `Griot.Infrastructure`) maps them in `OnModelCreating`.
- Enums stored as `varchar` via `HasConversion<string>()` so SQL reads are readable.
- `Guid` keys; `CreatedAt`/`UpdatedAt` set via a `SaveChangesAsync` override using `SYSUTCDATETIME()`.
- Index every FK; composite indexes per the approved ERD (e.g. `TaskItems(ColumnId, Position)`).

## Migration workflow

```bash
dotnet ef migrations add <Name> --project src/Griot.Infrastructure --startup-project src/Griot.Api
# review the generated SQL, then:
dotnet ef database update --project src/Griot.Infrastructure --startup-project src/Griot.Api
```

Migrations commit to `src/Griot.Infrastructure/Migrations/`. Production migrations run as the Railway release command, never assumed local.

## Verify

- `dotnet build` clean; `SELECT name FROM sys.tables` matches the ERD; indexes match the ERD annotations.
- Renames propagate (contract sync): ERD → DbContext → GraphQL types → web/mobile type files → MCP schemas.
