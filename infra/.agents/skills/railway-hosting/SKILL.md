---
name: railway-hosting
description: "Deploy the Griot backend + MCP containers to Railway (primary host; Render fallback; Azure App Service variant documented). Migrations run as the release command."
metadata:
  version: "0.1.0"
---

# Railway Hosting Skill

## Backend

- Service from the repo's backend `Dockerfile`; set env (`ConnectionStrings__Default`, `JWT__*`, `Redis__Connection`, `GRIOT_SERVICE_TOKEN`, `Cors__AllowedOrigins`).
- **Release command**: `dotnet tool restore && dotnet ef database update` — never assume a local migration.

## MCP

- Separate Railway service for `mcp/` (Streamable HTTP on 3001).

## Fallback / variant

- Render: similar Docker service; Azure App Service: `az webapp up` (documented in `deployment-targets.md`).
