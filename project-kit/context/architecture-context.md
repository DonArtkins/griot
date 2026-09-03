# Architecture Context

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md` in the same branch. Contracts include EF Core entities/relations and enum values, REST route signatures, GraphQL type/query/mutation names, auth token claims and endpoints, `GRIOT_SERVICE_TOKEN` behavior, env variables, Docker/Compose service names and ports, storage paths, generated file structure, package versions, and file ownership.

## System Boundary

Griot is a classic three-tier system plus an AI extension:

```
┌──────────────────────── Browser  ────────────────────────┐
│  web/  Vite + React 18 + MUI                             │
│   Public shell (landing, pricing, login)  ·  App shell   │
└───────┬───────────────────────────────────────┬──────────┘
        │ REST/GraphQL (Bearer JWT)             │ Trigger realtime (WS)
┌───────▼────────────────────────────┐   ┌──────▼──────────────┐
│  backend/  ASP.NET Core 8 (one proc)│   │  ai/ Trigger.dev v3 │
│  REST controllers + HotChocolate    │◄──►│  agents · tasks     │
│  Griot.Application services         │HTTP│  skills · LLM SDK   │
│  Griot.Domain entities              │HMAC│  (own lockfile)     │
│  Griot.Infrastructure DbContext     │    └────────┬────────────┘
└───────┬───────────────────▲─────────┘             │ GraphQL
        │ T-SQL             │ service token         │
┌───────▼─────────┐  ┌──────┴───────────┐  ┌───────▼─────────────┐
│ SQL Server 2022 │  │  Redis 7         │  │ mcp/ Griot MCP      │
│  EF Core 8      │  │  rate-limit +    │  │ Streamable HTTP /   │
│  Dapper procs   │  │  refresh tokens  │  │ stdio (ext clients) │
└─────────────────┘  └──────────────────┘  └─────────────────────┘
                              PostgreSQL 16 (secondary/test)
```

## Repository Layout (Separation of Concerns)

```
griot/
├── backend/                 # .NET 8 solution — one concern per project
│   ├── src/Griot.Api/          # Program.cs, controllers, GraphQL, middleware, auth
│   ├── src/Griot.Application/  # services (business logic); no EF, no HTTP
│   ├── src/Griot.Domain/       # entities + enums; zero dependencies
│   ├── src/Griot.Infrastructure/ # DbContext, Dapper repos, Redis, migrations, Sql/
│   └── tests/Griot.Tests/      # xUnit + WebApplicationFactory
├── web/                     # Vite + React 18 + MUI + Apollo + TanStack + Zustand
├── mobile/                  # Flutter + Riverpod + graphql_flutter + dio
├── ai/                      # Trigger.dev v3 agents (Node 20, own lockfile)
├── mcp/                     # Griot MCP server (own lockfile)
├── docker-compose.yml       # api + sqlserver + postgres + redis
├── .github/workflows/       # CI/CD (test gate → deploy)
├── PROMPTS/                 # week-grouped Figma/FigJam/agent prompts
└── project-kit/             # context/, feature-specs/, examples/, diagrams/
```

**Ownership rules (absolute):**
- `Griot.Api` owns HTTP concerns only (routing, serialization, auth middleware, CORS).
- `Griot.Application` owns business rules only; it depends on `Griot.Domain` interfaces/abstractions.
- `Griot.Infrastructure` owns persistence (EF Core `GriotDbContext`, Dapper repos, T-SQL scripts, Redis client).
- `Griot.Domain` references nothing external.
- `web/` and `mobile/` own presentation only; they never connect to a database.
- `ai/` and `mcp/` own intelligence only; they hold no database credentials and write exclusively through the API's GraphQL surface with `GRIOT_SERVICE_TOKEN`.
- Tests live in `tests/Griot.Tests` (backend), co-located `*.test.ts(x)` in `web/` (Jest+RTL), `mobile/test/` (Flutter), `ai/` + `mcp/` (Vitest).

## Database Architecture

| Store | Role | Notes |
|---|---|---|
| SQL Server 2022 | Primary source of truth | Container `gtp-sqlserver`, host port 14333; EF Core migrations; T-SQL procs (`usp_BulkUpdateTaskStatus`, `usp_GetDashboardSummary`) |
| PostgreSQL 16 | Secondary/test | Container `gtp-postgres`, host port 5433; same schema in an alternate engine for cohort exercises |
| Redis 7 | Runtime support (own-stack) | Container `gtp-redis`, host port 6380; rate limiting, refresh-token/session store, token budgets |

## Auth Architecture (own-stack)

- **Password hashing**: Argon2 (`Konscious.Security.Cryptography`), per-user salt.
- **Access tokens**: JWT, 15-min TTL, claims `sub`/`wid`, signed by env-provided key.
- **Refresh tokens**: opaque, stored **hashed** in SQL Server `RefreshTokens` table, rotated on use (old revoked, new issued).
- **Rate limiting**: Redis sliding window on `/api/auth/login` + query-cost guard on `/graphql`.
- **CORS**: allow-list to the Vercel origin in prod, localhost in dev.
- **AI principal**: `GRIOT_SERVICE_TOKEN` resolves to a dedicated `ai-agent` workspace member with a reduced role (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes, no invites). Verified via HMAC for `/api/webhooks/trigger`.

## API Surface

- REST (`/api/*`): auth, workspace/project/board/task/comment/notification CRUD, file upload, bulk ops.
- GraphQL (`/graphql`): HotChocolate `QueryType`/`MutationType`; `DataLoader<,>` batch loading for assignees/comments; consumed by web Apollo, mobile `graphql_flutter`, and the AI layer.
- Both call the same `Griot.Application` services — zero business logic in controllers or resolvers (drift-proof).

## AI Boundary (own-stack)

- Copilot prompt → `ai/` agent → tool calls → .NET GraphQL → SQL Server → streaming results to the UI.
- Mutations are always **proposed first**, then approved by the user in the UI; the app performs the write itself.
- Scheduled agents: due-date reminders, weekly sprint digest, stale-board detection, Monday standup builder.
- `mcp/` exposes `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project` to external clients.
- Audit: every tool call logged with `workspaceId`, `tool`, `payloadHash`, `runId` — traceable across Trigger run ↔ MCP call ↔ .NET audit log (`ActivityLogs`).

## Environment Variables

| Scope | Env | Purpose |
|---|---|---|
| backend | `ConnectionStrings__Default` | SQL Server connection (compose: `Server=gtp-sqlserver,1433;Database=griot;…`) |
| backend | `JWT__SigningKey`, `JWT__Issuer`, `JWT__Audience` | Token signing/validation |
| backend | `Redis__Connection` | `gtp-redis:6379` |
| backend | `GRIOT_SERVICE_TOKEN` | Accepts the AI layer's service calls (Bearer) |
| backend | `Cors__AllowedOrigins` | Comma-separated allow-list |
| web | `VITE_API_URL` | Base URL of the deployed API |
| ai/mcp | `GRIOT_API_URL`, `GRIOT_SERVICE_TOKEN`, `ANTHROPIC_API_KEY`/`OPENAI_API_KEY` | Call the API; LLM keys never leave `ai/.env` |
| compose | `GTP_SA_PASSWORD`, `GTP_PG_PASSWORD` | Local DB passwords (dev defaults only) |

## Deployment Targets

- **web/** → Vercel (Vite framework preset).
- **backend/** → Railway (Docker image; Render fallback; Azure App Service variant documented).
- **mobile/** → Play Store / APK artifact from CI (Docker-pinned Flutter build).
- **ai/** → Trigger.dev cloud (or self-hosted Railway).
- **mcp/** → Docker on Railway (Streamable HTTP).

## Invariants

1. The bootcamp PDF stack is never replaced without an explicit `[own-stack]` marker.
2. AI never writes to SQL Server directly.
3. Schema entities always trace to a Week-1 screen (no orphan tables).
4. Both API layers (REST + GraphQL) share the service layer.
5. Local parity: `docker compose up` reproduces production wiring before deploy day.
6. No secrets in code; `.env` git-ignored; `.env.example` documents shape.
7. Tests gate every merge via GitHub Actions.