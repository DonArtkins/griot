# Integration Contracts (cross-system)

These contracts are owned cross-system. Change one and the contract-sync gate (`/.agents/skills/contract-sync`) fires.

## Ports (host → container)

| Service | Container | Host:Container | Notes |
|---|---|---|---|
| sababisha-sqlserver | SQL Server 2022 | 14333:1433 | primary; `sababisha_*` volumes |
| sababisha-postgres | PostgreSQL 16 | 5433:5432 | secondary/test |
| sababisha-redis | Redis 7 | 6380:6379 | auth support |
| api | Griot.Api | 8080:8080 | `ASPNETCORE_URLS=http://+:8080` |
| mcp | Griot MCP | 3001:3001 | Streamable HTTP |

## Environment variables

| Scope | Var | Purpose |
|---|---|---|
| backend | `ConnectionStrings__Default` | SQL Server (compose: `Server=sababisha-sqlserver,1433;Database=griot;User Id=sa;Password=…`) |
| backend | `JWT__SigningKey` `JWT__Issuer` `JWT__Audience` | Token sign/validate |
| backend | `Redis__Connection` | `sababisha-redis:6379` |
| backend | `GRIOT_SERVICE_TOKEN` | AI/MCP service calls (Bearer) |
| backend | `Cors__AllowedOrigins` | Vercel origin prod; localhost dev |
| web | `VITE_API_URL` | deployed API base |
| ai/mcp | `GRIOT_API_URL` `GRIOT_SERVICE_TOKEN` | API access |
| ai | `ANTHROPIC_API_KEY`/`OPENAI_API_KEY` | LLM keys ONLY in `ai/.env` |
| infra | `SABABISHA_SA_PASSWORD` `SABABISHA_PG_PASSWORD` | local compose dev DB passwords |

## REST route contract (owned by backend spec 04)

Prefix `/api`. Auth module `register/login/refresh/logout`; then `/workspaces`, `/workspaces/{id}/members`, `/invites/{token}/accept`, `/projects`, `/boards`, `/columns`, `/tasks`, `/tasks/bulk-status`, `/tasks/{id}/comments`, `/tasks/{id}/attachments`, `/activity`, `/notifications`, `/notifications/read-all`, `/dashboard/summary`, `/webhooks/trigger`. Full shapes in `backend/project-kit/context/api-surface.md`.

## GraphQL surface (owned by backend spec 05)

Queries: `me`, `workspace(id)`, `projects`, `board(id)`, `tasks(filter, sort)`, `notifications`, `dashboardSummary`. Mutations mirror REST. Fields/entities named exactly per ERD.

## Entities & enums (owned by the approved ERD)

`Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`. Enums: `TaskStatus`, `Priority`, `WorkspaceRole`, `NotificationType`. These names are used verbatim by web TS types, mobile Dart models, GraphQL SDL, and MCP tool schemas.

## AI/MCP tool contract

Tools (ids): `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project`. All write via GraphQL with `GRIOT_SERVICE_TOKEN`; no deletes/invites.

## CI/CD contract

GitHub Actions job names (qa owns suites, infra owns pipeline): `test-dotnet`, `test-web`, `test-mobile`, `test-ai`, `test-mcp`, `newman`, `cypress`, `deploy`. Merge blocked if any red.
