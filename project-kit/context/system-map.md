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
│ PostgreSQL 16    │   │ login limits   │   └───────────┬──────────────┘
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
| backend | ai | Trigger.dev REST (enqueue task by ID) | `TRIGGER_SECRET_KEY` (server-to-server only) |
| web | ai | Trigger realtime (WS) — read-only stream delivery | Trigger access token |
| mobile | ai | *(none)* — mobile AI/Copilot rides the .NET API exclusively | n/a |
| mobile | ai | *(none)* — mobile never touches Trigger.dev; any mobile AI/Copilot surface consumes the .NET API only (contract: `research/ai-integration.md` §2a) | n/a |
| external AI clients | mcp | MCP (stdio / Streamable HTTP) | per-client config |
| infra | all | Docker images, env, CI/CD | infra secrets |
| qa | all | HTTP + test harnesses | test credentials / CI tokens |

## Non-negotiable boundary

AI (`ai/` + `mcp/`) never connects to SQL Server/Redis directly and never holds DB credentials. All data access is through the backend API. `web/` and `mobile/` never touch a database. `backend/` never contains UI or agent code.

**AI orchestration contract** (`research/ai-integration.md` §2a — authoritative): Trigger.dev (`ai/`) is a standalone compute/orchestration adapter deployed independently; the .NET backend is the only component that triggers tasks (REST/SDK, server-to-server `TRIGGER_SECRET_KEY`) and the only writer of source-of-truth data — task results are written back through the API (`POST /api/webhooks/trigger` HMAC, or `GRIOT_SERVICE_TOKEN` REST). Web/mobile never trigger or poll Trigger.dev — they call the .NET API; the sole direct frontend↔Trigger channel is the web Copilot realtime stream (scoped access token, read-only).

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
