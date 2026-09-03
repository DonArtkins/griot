# Week 02 · Diagram 02 — C4 Container Diagram (Level 2)

**Master spec + Figma Make paste prompts.** This diagram is the box-level view that must map **1:1 to `docker-compose.yml` and the Railway service list**. Protocols on every arrow.

---

## 1. Containers (boxes) — these map to deployable units

| Container | Tech | Notes |
|---|---|---|
| **Web App** | Vite + React 18 + MUI | Public shell + App shell + Copilot panel; served by Vercel; talks REST + GraphQL |
| **Mobile App** | Flutter 3.19 / Dart 3 | Android companion; same endpoints as web |
| **API** | ASP.NET Core 8 (.NET) | ONE process serving REST `/api` + GraphQL `/graphql` + `/health`; port 8080; container `api` |
| **MCP Server** | Node 20 + @modelcontextprotocol/sdk | Streamable HTTP (3001) + stdio; container `mcp` |
| **AI Agents** | Trigger.dev v3 (Node 20) | Copilot agent + scheduled tasks; hosted by Trigger cloud |
| **SQL Server** | 2022 | Primary DB; container `sqlserver`; port 14333 host |
| **PostgreSQL** | 16 | Secondary/test store; container `postgres`; port 5433 host |
| **Redis** | 7 | Rate limit + refresh tokens + token budgets; container `redis`; port 6380 host |

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

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It builds all 8 containers AND every arrow in one extensive pass.

```text
C4 Container diagram Level 2 for Griot, the box-level deployment view. Draw these 8 containers (rounded rects), each with a tech badge and a host corner-badge:

1. web — Vite + React 18 + MUI; Public shell + App shell + Copilot panel [host: Vercel]
2. mobile — Flutter 3.19 / Dart 3; Android companion [host: Play/APK]
3. api — ASP.NET Core 8; REST /api + GraphQL /graphql + /health; port 8080 [host: Railway container:api]
4. mcp — Node 20 + @modelcontextprotocol/sdk; Streamable HTTP 3001 + stdio [host: Railway container:mcp]
5. ai — Trigger.dev v3; Copilot agent + scheduled tasks [host: Trigger cloud]
6. sqlserver — SQL Server 2022; primary DB [host: Railway container:sqlserver, host port 14333]
7. postgres — PostgreSQL 16; secondary/test [host: Railway container:postgres, host port 5433]
8. redis — Redis 7; rate limit + refresh + budgets [host: Railway container:redis, host port 6380]

CONNECT WITH LABELED ARROWS (protocol on every arrow):
- web → api: HTTPS REST /api + GraphQL /graphql (JWT Bearer)
- mobile → api: HTTPS REST + GraphQL (JWT Bearer)
- web → ai: Trigger realtime WebSocket (Copilot streaming)
- ai → api: HTTPS GraphQL GRIOT_SERVICE_TOKEN; api → ai: HMAC webhook (dashed return)
- mcp → api: HTTPS GraphQL GRIOT_SERVICE_TOKEN
- External AI clients → mcp: MCP stdio/Streamable HTTP
- api → sqlserver: TCP 1433 TDS (EF Core 8 + Dapper 2)
- api → postgres: TCP 5432 (secondary/test only)
- api → redis: TCP 6379
- api → email provider: SMTP/HTTPS (invites, reminders, digest)

ANNOTATION: "This diagram maps 1:1 to docker-compose.yml + the Railway service list — exactly these 8 containers, no extras." Container names must match the compose service keys (api, sqlserver, postgres, redis, mcp). Color: web=Vercel brand, api/mcp/sqlserver/postgres/redis=Railway, ai=Trigger. Readable at 100% zoom; orthogonal routing.
```

### Refine

- "Label the api box port badge 8080; rename container names to compose service keys."
- "Draw the api↔ai HMAC return arrow dashed."
- "Recolor mcp to the Railway color group."

---

## 3. Definition of Done

- [ ] 8 containers present with tech + host + port badges
- [ ] Every arrow has a protocol label; no unlabeled edges
- [ ] Container names == compose service keys; matches deployment docs
- [ ] Approved → PNG → `project-kit/diagrams/architecture/c4-container.png`