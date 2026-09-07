---
name: docker-compose
description: "Docker Compose v2 for the Griot local topology (api + sqlserver + postgres + redis + mcp): service definitions, volumes, networks, health checks, port isolation (sababisha-*)."
metadata:
  version: "0.1.0"
---

# Docker Compose Skill

## Topology (local parity = production)

```yaml
services:
    sabahisha-sqlserver:   # on 14333:1433, volume sababisha_mssql
  sabahisha-postgres:    # on 5433:5432, volume sababisha_pg
  sabahisha-redis:       # on 6380:6379
  api:         # build backend/, ASPNETCORE_URLS=http://+:8080, ports 8080:8080, depends_on sabahisha-sqlserver+sabahisha-redis
  mcp:         # build mcp/, Streamable HTTP on 3001:3001
volumes: { sababisha_mssql:, sababisha_pg: }
```

## Rules

- Daemons prefixed `sababisha-*`; never collide with a personal stack's ports.
- Secrets via env (`.env`, git-ignored); no hardcoded passwords.
- Named volumes, no host-path binds for data.
- Health checks for `api` and `sababisha-sqlserver`.
