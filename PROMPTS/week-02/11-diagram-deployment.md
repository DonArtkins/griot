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

## 3. Figma Make prompts

### PROMPT A

```
UML deployment diagram for Griot production. Draw 4 zones as big rounded-rect containers:
PUBLIC INTERNET (top): node User Browser (web); node Mobile Device (Flutter); node External AI clients (Claude/Cursor/Cline).
VERCEL (cloud): node Web App (Vite static, CDN, no server). HTTPS arrows: Browser->Web App; Mobile->Web App.
RAILWAY (private network): node API container :8080 (public 443 via proxy); node MCP container :3001 (Streamable HTTP); node SQL Server 2022 (internal, no public port); node PostgreSQL 16 (internal); node Redis 7 (internal). Internal arrows labeled TCP 1433 TDS / TCP 5432 / TCP 6379.
TRIGGER.DEV (cloud): node AI agents (scheduled tasks + Copilot). Arrow AI agents -> API: GraphQL + GRIOT_SERVICE_TOKEN; Web App -> AI agents: Trigger realtime WebSocket (stream).
EXTERNAL: node Email provider (SMTP); node GitHub Actions. Arrows: GitHub Actions -> Vercel/Railway/Trigger (deploy, dashed, one line each).
Annotate: "what's public vs Railway-internal" and "no DB port exposed to internet".
```

### PROMPT B — release command note

```
Add to the Griot deployment diagram, near GitHub Actions→Railway: red/amber note "EF Core migrations run as Railway RELEASE command (dotnet ef database update) — never a local-first assumption". Near GitHub Actions→Vercel: "Vite framework preset". Near Railway API: "health endpoint /health checked by Railway + uptime ping".
```

### Fix snippets

- "Move SQL Server/Postgres/Redis inside the Railway private network, no public port."
- "Add WebSocket arrow Web App→AI agents (Trigger realtime) for Copilot streaming."

---

## Definition of Done

- [ ] 4 zones + external SaaS all visible; network boundaries explicit
- [ ] Every deploy path (CI) + every runtime path (HTTPS/WS/internal TCP) drawn
- [ ] "no public DB port" + release-command notes present
- [ ] Approved → PNG → `project-kit/diagrams/architecture/deployment-production.png`