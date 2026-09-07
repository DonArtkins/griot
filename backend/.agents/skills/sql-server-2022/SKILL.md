---
name: sql-server-2022
description: "SQL Server 2022 as the primary database on Linux via the official container: connection, schema conventions, indexing, and T-SQL style."
metadata:
  version: "0.1.0"
---

# SQL Server 2022 Skill

## Container

`mcr.microsoft.com/mssql/server:2022-latest` on host port 14333; data in `sababisha_mssql` volume (infra owns the compose file).

```bash
docker exec sababisha-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SABABISHA_SA_PASSWORD" -C -Q "SELECT @@VERSION"
```

## Conventions

- `dbo` schema; PascalCase names matching entities; `nvarchar(max)` bodies; `UNIQUEIDENTIFIER` PKs; `SYSUTCDATETIME()` timestamps.
- Index every FK; composite indexes for board reads and the dashboard query.
- Procs: `usp_` prefix, `NOCOUNT ON`, fully parameterized (OWASP line).
- T-SQL versioned in `.sql` files under `Griot.Infrastructure/Sql/`.

## Verify

- Table list matches the ERD; the dashboard query plan is single-pass without object scans.
