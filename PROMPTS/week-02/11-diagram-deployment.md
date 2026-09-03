# Week 02 · Diagram 07 — Deployment Diagram (production)

**Master spec + Figma Make paste prompts.** This is the diagram that turns Week-5 Docker/CI work into something reviewable **before** deploying anything. Show the network boundaries: what's public internet, what's Railway-internal, who deploys what.

---

## 1. Nodes & boundaries

**Public internet zone** (top)
- **User Browser** (web app)
- **Mobile Device** (Flutter)
- **External AI clients** (Claude Desktop, Cursor, Cline)

**Vercel zone** (edge)
- **Web App** (Vite static, Vercel CDN, edge functions for nothing in v1 — pure static + env)

**Railway zone** (private network)
- **API container** port 8080 (public:443 via Railway proxy)
- **MCP container** port 3001 (Streamable HTTP)
- **SQL Server** (internal, no public port)
- **PostgreSQL** (internal, no public port)
- **Redis** (internal, no public port)

**Trigger.dev (cloud)**
- **AI agents** (scheduled + Copilot)

**External SaaS**
- **Email provider** (SMTP)
- **GitHub Actions** (CI → deploys)

## 2. Deployment flow arrows

- GitHub Actions → Vercel (deploy web)
- GitHub Actions → Railway (deploy api + mcp containers; run EF migrations as release command)
- GitHub Actions → Trigger.dev (deploy ai tasks)
- Browser/Mobile → Vercel → Railway API (HTTPS public)
- Browser → Trigger realtime (WebSocket, for Copilot stream)
- External AI → Railway MCP (Streamable HTTP public) → Railway API (internal)

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws all four zones + deploy/runtime arrows in one pass.

```text
UML deployment diagram for Griot production. Draw 4 zones as big rounded-rect containers + external SaaS:

ZONE 1 — PUBLIC INTERNET (top):
- node: User Browser (web)
- node: Mobile Device (Flutter)
- node: External AI clients (Claude Desktop / Cursor / Cline)

ZONE 2 — VERCEL (cloud):
- node: Web App (Vite static, CDN; no server runtime)
- arrows: Browser → Web App (HTTPS); Mobile → Web App (HTTPS)

ZONE 3 — RAILWAY (private network):
- node: API container :8080 (public 443 via Railway proxy)
- node: MCP container :3001 (Streamable HTTP)
- node: SQL Server 2022 (internal, NO public port)
- node: PostgreSQL 16 (internal, NO public port)
- node: Redis 7 (internal, NO public port)
- internal arrows (TCP): API→SQL Server 1433 (EF Core8+Dapper2); API→Postgres 5432; API→Redis 6379
- public arrow: Web App → API (HTTPS REST + GraphQL); External AI clients → MCP (Streamable HTTP)

ZONE 4 — TRIGGER.DEV (cloud):
- node: AI agents (Copilot + scheduled: dueReminders, sprintDigest, staleBoard, standupBuilder)
- arrows: Web App → AI agents (Trigger realtime WebSocket, streaming); AI agents → API (GraphQL + GRIOT_SERVICE_TOKEN)

EXTERNAL SaaS:
- node: Email provider (SMTP)
- node: GitHub Actions (CI)
- arrows: GitHub Actions → Vercel (deploy web, dashed); → Railway (deploy api + mcp containers, run EF migrations as release command, dashed); → Trigger.dev (deploy ai, dashed)

ANNOTATION:
1) "What's public internet vs Railway-internal: no DB port is ever exposed to the internet."
2) Red/amber note near GitHub Actions→Railway: "EF Core migrations run as the Railway RELEASE command (dotnet ef database update) — never a local-first assumption."
3) Note near API: "health endpoint /health checked by Railway + uptime ping."
```

### Refine

- "Move SQL Server / Postgres / Redis inside the Railway private zone with no public port."
- "Add WebSocket arrow Web App → AI agents (Trigger realtime)."

---

## Definition of Done

- [ ] 4 zones + external SaaS all visible; network boundaries explicit
- [ ] Every deploy path (CI) + every runtime path (HTTPS/WS/internal TCP) drawn
- [ ] "no public DB port" + release-command notes present
- [ ] Approved → PNG → `project-kit/diagrams/architecture/deployment-production.png`