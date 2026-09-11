# Week 02 · Diagram 02 — C4 Container Diagram (Level 2)

> Backend 09 contract: Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of` resolves a real-user OBO principal (`ai-on-behalf-of`), never a synthetic member. Exactly four scope claims are issued: ReadWorkspace/CreateTask/AddComment/CreateNotification. AI OBO bulk status, deletes, invites and member management are denied; ActivityLog persistence is planned for backend 20. See `docs/api/ai-service-token-contract.md`.

**Master spec + Figma Make paste prompts.** This is the box-level view. The **five compose-deployable services** (`api`, `mcp`, `sababisha-sqlserver`, `sababisha-postgres`, `sababisha-redis`) match the service keys in `project-kit/context/integration-contracts.md` (the `infra/` compose file is a Week-5 deliverable; when `infra/docker-compose.yml` lands, its service keys must equal these). They are also the target Railway services, though the Railway production topology may differ (see `infra/AGENTS.md` for the authoritative deployment config — Railway may combine or split services). `web` (Vercel), `mobile` (Play/APK), and `ai` (Trigger cloud) are **external hosts/clients** drawn as containers with their host badge, NOT compose services. Protocols on every arrow.

---

## 1. Containers (boxes) — these map to deployable units

| Container | Tech | Notes |
|---|---|---|
| **Web App** | Vite + React 18 + MUI | Public shell + App shell + Copilot panel; served by Vercel; talks REST + GraphQL |
| **Mobile App** | Flutter 3.19 / Dart 3 | Android companion; same endpoints as web |
| **API** | ASP.NET Core 8 (.NET) | ONE process serving REST `/api` + GraphQL `/graphql` + `/health`; port 8080; container `api` |
| **MCP Server** | Node 20 + @modelcontextprotocol/sdk | Streamable HTTP (3001) + stdio; container `mcp` |
| **AI Agents** | Trigger.dev v4 (Node 20) | Copilot agent + scheduled tasks; hosted by Trigger cloud |
| **SQL Server** | 2022 | Primary DB; container `sababisha-sqlserver`; port 14333 host |
| **PostgreSQL** | 16 | Secondary/test store; container `sababisha-postgres`; port 5433 host |
| **Redis** | 7 | Rate limit + refresh tokens + token budgets; container `sababisha-redis`; port 6380 host |

## 2. Arrows & protocols (every arrow labeled)

| From | To | Protocol |
|---|---|---|
| Web App | API | HTTPS / REST (`/api`) + GraphQL (`/graphql`), JWT Bearer |
| Mobile App | API | HTTPS / REST + GraphQL, JWT Bearer |
| Web App | AI Agents | Trigger realtime (WebSocket) — streaming answers |
| AI Agents | API | HTTPS / GraphQL, `GRIOT_SERVICE_TOKEN` (HMAC webhooks back) |
| MCP Server | API | HTTPS / GraphQL, `GRIOT_SERVICE_TOKEN` |
| External AI clients | MCP Server | MCP (stdio local / Streamable HTTP remote) |
| API | SQL Server | TCP 1433 (TDS), EF Core + Dapper |
| API | PostgreSQL | TCP 5432 (secondary/test only) |
| API | Redis | TCP 6379 (rate limit, refresh, budgets) |
| API | Email provider | SMTP / HTTPS (invites, reminders, digests) |

> **MCP transport trust model — stdio vs Streamable HTTP:**
>
> | Transport | Trust boundary | Authentication | Scopes / permissions | Rejection behavior |
> |---|---|---|---|---|
> | **stdio (local)** | Process-level — client runs as the same OS user who launched the MCP server; no network exposure | None required. OS process isolation is the trust boundary; the MCP server trusts the calling process implicitly. | Full tool roster available | N/A — connection is rejected at the OS level if the process is not authorized to spawn the server. Never available in Docker/Railway. |
> | **Streamable HTTP (remote / Docker / Railway)** | Network-exposed endpoint — `http://mcp:3001` is the **internal container hop only** (compose network); the Railway/remote public endpoint is exposed as **HTTPS/TLS only** (load-balanced TLS termination), reachable by any agent/machine that can reach the public ingress | Bearer token required: `Authorization: Bearer <GRIOT_MCP_TOKEN>`, transmitted over the TLS-protected public endpoint. `GRIOT_MCP_TOKEN` is a long-lived static secret (distinct from `GRIOT_SERVICE_TOKEN`), injected via environment and validated on every request before any tool dispatch. | Scoped to read + task-write operations: `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `add_comment`, `get_activity_feed`, `summarize_project`. No admin operations (workspace delete, invite, role change). Scope enforcement is in the MCP server layer; the .NET API enforces it again via the `ai-on-behalf-of` principal. | Missing or invalid bearer token → `401 Unauthorized` (rejected before any tool dispatch). Unrecognized tool name → `404 Tool Not Found`. Scope violation → `403 Forbidden`. All rejections logged with `clientIp`, `tool`, and `timestamp` for OWASP audit. |
>
> External AI clients (Claude Desktop, Cursor, VS Code Copilot, Cline, Griot's own `ai/` agents) **must** present a valid `GRIOT_MCP_TOKEN` when connecting over HTTP. Stdio connections bypass this gate and are only permitted in local development.

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It builds all 8 containers AND every arrow in one extensive pass.

```text
C4 Container diagram Level 2 for Griot, the box-level deployment view. Draw these 8 boxes (rounded rects), each with a tech badge and a host corner-badge. Mark which are compose/Railway deployable services (`api`, `mcp`, `sababisha-sqlserver`, `sababisha-postgres`, `sababisha-redis`) vs external host clients (`web`, `mobile`, `ai`):

1. web — Vite + React 18 + MUI; Public shell + App shell + Copilot panel [host: Vercel]
2. mobile — Flutter 3.19 / Dart 3; Android companion [host: Play/APK]
3. api — ASP.NET Core 8; REST /api + GraphQL /graphql + /health; port 8080 [host: Railway container:api]
4. mcp — Node 20 + @modelcontextprotocol/sdk; Streamable HTTP 3001 + stdio [host: Railway container:mcp]
5. ai — Trigger.dev v4; Copilot agent + scheduled tasks [host: Trigger cloud]
6. sababisha-sqlserver — SQL Server 2022; primary DB [host: Railway container:sababisha-sqlserver, host port 14333]
7. sababisha-postgres — PostgreSQL 16; secondary/test [host: Railway container:sababisha-postgres, host port 5433]
8. sababisha-redis — Redis 7; rate limit + refresh + budgets [host: Railway container:sababisha-redis, host port 6380]

CONNECT WITH LABELED ARROWS (protocol on every arrow):
- web → api: HTTPS REST /api + GraphQL /graphql (JWT Bearer)
- mobile → api: HTTPS REST + GraphQL (JWT Bearer)
- web → ai: Trigger realtime WebSocket (Copilot streaming)
- ai → api: HTTPS GraphQL GRIOT_SERVICE_TOKEN; api → ai: HMAC webhook (dashed return)
- mcp → api: HTTPS GraphQL GRIOT_SERVICE_TOKEN
- External AI clients → mcp: MCP stdio/Streamable HTTP
Note on mcp arrow: "stdio (local dev): OS process trust — no token required.
    Streamable HTTP (Docker/Railway): Bearer GRIOT_MCP_TOKEN required.
    Missing/invalid token → 401. Scope violation → 403. All rejections logged."
- api → sqlserver: TCP 1433 TDS (EF Core 8 + Dapper 2)
- api → postgres: TCP 5432 (secondary/test only)
- api → redis: TCP 6379
- api → email provider: SMTP/HTTPS (invites, reminders, digest)

ANNOTATION: "The compose/Railway deployable services shown are: api, mcp, sababisha-sqlserver, sababisha-postgres, sababisha-redis — matching the service keys in integration-contracts.md (the infra/docker-compose.yml will use these exact keys). web → Vercel, mobile → Play/APK, ai → Trigger cloud are EXTERNAL hosts/clients, not compose services. Note: Railway may split or combine services differently from compose for production (see infra/AGENTS.md); this diagram shows the logical service topology, not a 1:1 Railway config guarantee. Color: web=Vercel brand, api/mcp/sababisha-*=Railway, ai=Trigger. Readable at 100% zoom; orthogonal routing."
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

### Refine

- "Label the api box port badge 8080; rename container names to compose service keys."
- "Draw the api↔ai HMAC return arrow dashed."
- "Recolor mcp to the Railway color group."

---

## 3. Definition of Done

- [ ] 8 boxes present with tech + host + port badges; 5 compose/Railway services vs 3 external hosts clearly distinguished
- [ ] Every arrow has a protocol label; no unlabeled edges
- [ ] Container names == compose service keys (`api`, `mcp`, `sababisha-sqlserver`, `sababisha-postgres`, `sababisha-redis`); matches `integration-contracts.md` + deployment docs
- [ ] MCP arrow annotated with dual trust model: stdio (OS process trust, no token) vs Streamable HTTP (Bearer `GRIOT_MCP_TOKEN` required; 401 on miss, 403 on scope violation, all rejections logged)
- [ ] `GRIOT_MCP_TOKEN` is distinct from `GRIOT_SERVICE_TOKEN`; both documented in the diagram notes
- [ ] Approved → PNG → `diagrams/architecture/c4-container.png`
