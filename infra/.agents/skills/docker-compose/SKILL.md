---
name: docker-compose
description: "Docker Compose v2 for the Griot local topology (api + sqlserver + postgres + redis + mcp): service definitions, volumes, networks, health checks, port isolation (gtp-*)."
metadata:
  version: "0.1.0"
---

# Docker Compose Skill

## Topology (local parity = production)

```yaml
services:
  sqlserver:   # gtp-sqlserver on 14333:1433, volume mssql_data
  postgres:    # gtp-postgres on 5433:5432, volume pg_data
  redis:       # gtp-redis on 6380:6379
  api:         # build backend/, ASPNETCORE_URLS=http://+:8080, ports 8080:8080, depends_on sqlserver+redis
  mcp:         # build mcp/, Streamable HTTP on 3001:3001
volumes: { mssql_data:, pg_data: }
```

## Rules

- Daemons prefixed `gtp-*`; never collide with a personal stack's ports.
- Secrets via env (`.env`, git-ignored); no hardcoded passwords.
- Named volumes, no host-path binds for data.
- Health checks for `api` and `sqlserver`.
