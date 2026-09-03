# System Map — the 7 systems of Griot

## Systems

```
┌──────────────────────────── Browser  ─────────────────────────────┐
│  web/  React 18 + Vite 5 + MUI v6                                 │
│   Public shell · App shell · Copilot panel                        │
└───────┬───────────────────────────────────────────────┬───────────┘
        │ REST/GraphQL (Bearer JWT)                      │ Trigger realtime (WS)
┌───────▼─────────────────────────────┐   ┌──────────────▼───────────┐
│  backend/  ASP.NET Core 8 (one proc)│   │  ai/  Trigger.dev v3     │
│  REST controllers + HotChocolate    │◄──►│  agents · tasks · skills │
│  Griot.Application services         │HTTP│  (own lockfile)          │
│  Griot.Domain entities              │HMAC│                          │
│  Griot.Infrastructure (EF+Dapper)   │    └────────────┬─────────────┘
└───────┬──────────────────────▲──────┘                 │ GraphQL
        │ T-SQL                │ service token          │ (service token)
┌───────▼──────────┐   ┌───────┴────────┐   ┌───────────▼──────────────┐
│ SQL Server 2022  │   │ Redis 7        │   │  mcp/  Griot MCP server   │
│ (EF Core 8)      │   │ rate-limit +   │   │  Streamable HTTP / stdio  │
│ PostgreSQL 16    │   │ refresh tokens │   └───────────┬──────────────┘
└──────────────────┘   └────────────────┘               │ MCP
                                                   ┌─────▼─────┐
                                                   │ external  │
                                                   │ AI clients│
                                                   └───────────┘
        ┌────────────────────────────────────────────────────────────┐
        │ infra/  Docker Engine + Compose v2 · Vercel · Railway · CI  │
        └────────────────────────────────────────────────────────────┘
        ┌────────────────────────────────────────────────────────────┐
        │ qa/  xUnit · Jest+RTL · Flutter · Cypress · Newman · k6     │
        └────────────────────────────────────────────────────────────┘
```

## Communication boundaries

| From | To | Protocol | Auth |
|---|---|---|---|
| web | backend | REST `/api/*` + GraphQL `/graphql` | JWT access token (Bearer) |
| mobile | backend | REST + GraphQL (same endpoints) | JWT access token |
| ai | backend | GraphQL only | `GRIOT_SERVICE_TOKEN` (ai-agent principal) |
| mcp | backend | GraphQL only | `GRIOT_SERVICE_TOKEN` |
| backend | ai | `POST /api/webhooks/trigger` (HMAC) | `X-Trigger-Signature` |
| web | ai | Trigger realtime (WS) | Trigger access token |
| external AI clients | mcp | MCP (stdio / Streamable HTTP) | per-client config |
| infra | all | Docker images, env, CI/CD | infra secrets |
| qa | all | HTTP + test harnesses | test credentials / CI tokens |

## Non-negotiable boundary

AI (`ai/` + `mcp/`) never connects to SQL Server/Redis directly and never holds DB credentials. All data access is through the backend API. `web/` and `mobile/` never touch a database. `backend/` never contains UI or agent code.
