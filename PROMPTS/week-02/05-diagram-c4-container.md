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

## 3. Figma Make prompts (≤2000 chars each, ONE canvas, order matters)

### PROMPT A — Boxes

```
C4 Container diagram Level2 for Griot PM app. Draw these 8 containers (rounded rects), fill by owner, port badges in the corner:
web (Vite+React18+MUI, Public+App shells+Copilot) [Vercel]
mobile (Flutter3.19/Dart3 Android) [Play/APK]
api (ASP.NET Core8, REST /api + GraphQL /graphql + /health, port 8080) [Railway container:api]
mcp (Node20 @modelcontextprotocol/sdk, Streamable HTTP 3001 + stdio) [Railway container:mcp]
ai (Trigger.dev v3, Copilot agent + scheduled tasks) [Trigger cloud]
sqlserver (SQL Server 2022, primary DB, host 14333) [Railway container:sqlserver]
postgres (PostgreSQL 16, secondary/test, host 5433) [Railway container:postgres]
redis (Redis 7, rate limit + refresh + budgets, host 6380) [Railway container:redis]
```

### PROMPT B — Arrows/protocols

```
Connect the C4 L2 containers with labeled arrows:
web -> api HTTPS REST /api + GraphQL /graphql (JWT)
mobile -> api HTTPS REST+GraphQL (JWT)
web -> ai Trigger realtime WS (streaming)
ai -> api HTTPS GraphQL GRIOT_SERVICE_TOKEN; api -> ai HMAC webhook
mcp -> api HTTPS GraphQL GRIOT_SERVICE_TOKEN
external AI clients -> mcp MCP stdio/Streamable HTTP
api -> sqlserver TCP 1433 TDS (EF Core8 + Dapper2)
api -> postgres TCP 5432 (secondary/test)
api -> redis TCP 6379
api -> email provider SMTP/HTTPS (invites, reminders, digest)
This diagram is the docker-compose.yml + Railway service list contract: 8 containers, no extras.
```

### Fix snippets

- "Label the api box port badge 8080; rename container names to compose service keys (api, sqlserver, postgres, redis, mcp)."
- "Draw the api<->ai arrow with a return HMAC arrow dashed."
- "Recolor web to Vercel color, api/mcp/sqlserver/postgres/redis to Railway color, ai to Trigger color."

---

## 3. Definition of Done

- [ ] 8 containers present with tech + host + port badges
- [ ] Every arrow has a protocol label; no unlabeled edges
- [ ] Container names == compose service keys; matches deployment docs
- [ ] Approved → PNG → `project-kit/diagrams/architecture/c4-container.png`