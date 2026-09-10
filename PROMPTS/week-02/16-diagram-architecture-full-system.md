# Week 02 · Diagram 12 — Full-System Architecture

> Backend 09 contract: Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of` resolves a real-user OBO principal (`ai-on-behalf-of`), never a synthetic member. Exactly four scope claims are issued: ReadWorkspace/CreateTask/AddComment/CreateNotification. AI OBO bulk status, deletes, invites and member management are denied; ActivityLog persistence is planned for backend 20. See `docs/api/ai-service-token-contract.md`.

**Master spec + Figma Make paste prompts.** The **one big-picture architecture diagram for the ENTIRE system** — all 7 systems, all boundaries, all runtimes, all protocols on one page. Where the C4 context (04) shows actors and the C4 container (05) shows deployable boxes, this diagram shows the **layered logical architecture + the communication contracts** drawn together. Read it as the visual index of `docs/ARCHITECTURE.md` and `project-kit/context/system-map.md`.

> **Contract source of truth:** `project-kit/context/system-map.md` (7 systems + communication boundaries) · `docs/ARCHITECTURE.md` (big picture + request flows 3.1–3.4) · `project-kit/context/integration-contracts.md` (ports, env, tokens) · `project-kit/context/stack-contract.md` (exact stack + `[own-stack]` markers). Stay synced with those files.

---

## 1. The 7 systems (the layer frame)

| System | Folder | Tech | Week |
|---|---|---|---|
| Backend / API | `backend/` | .NET 8, ASP.NET Core Web API, EF Core 8, Dapper 2.x, HotChocolate 14+, SQL Server 2022, PostgreSQL 16 | Week 2 |
| Web | `web/` | React 18.3, Vite 5, MUI v6, Apollo, Axios, TanStack Query 5 | Week 3 |
| Mobile | `mobile/` | Flutter 3.19+, Dart 3, GraphQL Flutter, Riverpod | Week 4 |
| DevOps / Infra | `infra/` | Docker 26+, Compose v2, Vercel, Railway, GitHub Actions | Week 5 |
| Quality Engineering | `qa/` | xUnit, Jest+RTL, Flutter, Cypress, Newman, k6 | Weeks 6–7 |
| AI agents | `ai/` [own-stack] | Trigger.dev v3 | `ai-integration.md` |
| MCP server | `mcp/` [own-stack] | MCP stdio + Streamable HTTP | `ai-integration.md` |

Auth [own-stack]: JWT + Argon2 + Redis. AI writes never touch SQL Server directly — always through the API with `GRIOT_SERVICE_TOKEN`.

## 2. Layers / zones to draw (top → bottom)

1. **Presentation** — Web App (React 18 + Vite 5 + MUI v6: Public shell · App shell · Copilot panel); Mobile App (Flutter 3.19: Android companion).
2. **Edge** — Vercel (static CDN only — assets + env; **not** an API proxy; browser/mobile call the API directly).
3. **API** — ASP.NET Core 8, one process: REST `/api/*` + GraphQL `/graphql` (HotChocolate) + `/health` (port 8080); `Griot.Application` shared service layer; `Griot.Domain`; infrastructure (EF Core 8 repos + Dapper 2 procs `usp_BulkUpdateTaskStatus` / `usp_GetDashboardSummary`); DataLoader (N+1 prevention); Auth middleware (JWT → principal `sub`/`email`/`jti`; `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of` → `ai-on-behalf-of`); WebhookRelayService (`POST /api/webhooks/trigger`, HMAC `X-Trigger-Signature`).
4. **Data** — SQL Server 2022 (primary, source of truth, host port 14333); PostgreSQL 16 (secondary/test, 5433); Redis 7 (rate limit + refresh metadata + token budgets, 6380). Compose service keys: `sababisha-sqlserver`, `sababisha-postgres`, `sababisha-redis`.
5. **Intelligence** — `ai/` Trigger.dev v3 agents (`griotCopilot` + `dueReminders`/`sprintDigest`/`staleBoard`/`standupBuilder`); `mcp/` server (9 tools; stdio local + Streamable HTTP 3001; Bearer `GRIOT_MCP_TOKEN`).
6. **External AI clients** — Claude Desktop / Cursor / Cline (MCP).
7. **Platform** — Railway (api + mcp + DBs, private network), Vercel (web), Trigger.dev cloud (ai).
8. **Cross-cutting rails** — `infra/` (Docker + Compose v2, CI/CD); `qa/` (test harnesses + gates); observability (ApiLogs/ErrorLogs/AuditLogs, 90-day hot retention).

## 3. Communication boundaries (label the arrows)

| From | To | Protocol | Auth |
|---|---|---|---|
| web | backend | REST `/api/*` + GraphQL `/graphql` | JWT access token (Bearer) |
| mobile | backend | REST + GraphQL (same endpoints) | JWT access token |
| ai | backend | GraphQL only | `GRIOT_SERVICE_TOKEN` (ai-on-behalf-of) |
| mcp | backend | GraphQL only | `GRIOT_SERVICE_TOKEN` |
| backend | ai | `POST /api/webhooks/trigger` (HMAC) | `X-Trigger-Signature` |
| web | ai | Trigger realtime (WS) | Trigger access token |
| external AI clients | mcp | MCP (stdio / Streamable HTTP) | `GRIOT_MCP_TOKEN` (HTTP) / OS trust (stdio) |
| infra | all | Docker images, env, CI/CD | infra secrets |
## 4. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make (Plan mode first). It draws all 8 layers + every boundary arrow in one pass.

```text
Full-system architecture diagram for Griot — one page, layered top-to-bottom. 8 zones as big rounded-rect containers with tech badges, all arrows labelled with protocol + auth.

ZONE 1 — PRESENTATION (top): box "Web App — React 18 · Vite 5 · MUI v6 (Public shell · App shell · Copilot panel)" with light-canvas design-system badge (#F7F8FA, chrome-ink CTA #1E2022); box "Mobile App — Flutter 3.19 / Dart 3 (Android companion)".
ZONE 2 — EDGE: box "Vercel — static CDN ONLY (assets + env; NOT an API proxy)". Annotation: "Browser/Mobile call the Railway API DIRECTLY — Vercel never proxies API calls."
ZONE 3 — API: ONE wide box "backend/ — ASP.NET Core 8": REST /api/* + GraphQL /graphql (HotChocolate) + /health :8080; inside sub-blocks: "Griot.Application (shared service layer — Auth, Workspace, Project, Board, Task, Comment, Attachment, Notification, Dashboard, WebhookRelay)"; "Griot.Domain"; "Infrastructure — EF Core 8 repos + Dapper 2 (usp_BulkUpdateTaskStatus, usp_GetDashboardSummary)"; "DataLoader (N+1 prevention)"; "Auth middleware (JWT → sub/email/jti; GRIOT_SERVICE_TOKEN + X-On-Behalf-Of → ai-on-behalf-of)"; "WebhookRelayService (HMAC X-Trigger-Signature)".
ZONE 4 — DATA: three boxes with compose service-key badges: "sababisha-sqlserver — SQL Server 2022 (primary, source of truth, host 14333)"; "sababisha-postgres — PostgreSQL 16 (secondary/test, 5433)"; "sababisha-redis — Redis 7 (rate limit + refresh metadata + token budgets, 6380)".
ZONE 5 — INTELLIGENCE: box "ai/ — Trigger.dev v3: griotCopilot + scheduled dueReminders, sprintDigest, staleBoard, standupBuilder"; box "mcp/ — Griot MCP server (Node 20): 9 tools (list_projects … summarize_project); stdio + Streamable HTTP :3001".
ZONE 6 — EXTERNAL AI CLIENTS: box "Claude Desktop / Cursor / Cline (MCP clients)".
ZONE 7 — PLATFORM: Railway (private network: api + mcp + the 3 DBs; TLS terminates at proxy), Vercel (web), Trigger.dev cloud (ai).
ZONE 8 — CROSS-CUTTING RAILS (bottom strip): "infra/ — Docker + Compose v2, GitHub Actions CI/CD (jobs: test-dotnet, test-web, test-mobile, test-ai, test-mcp, newman, cypress, deploy)"; "qa/ — xUnit · Jest+RTL · Flutter · Cypress · Newman · k6"; "Observability — ApiLogs / ErrorLogs / AuditLogs (90-day hot retention)".

ARROWS (label every one — protocol + auth):
- web → API: HTTPS REST + GraphQL, JWT Bearer
- mobile → API: HTTPS REST + GraphQL, JWT Bearer
- web → ai/: Trigger realtime WebSocket (Trigger access token)
- ai/ → API: GraphQL, GRIOT_SERVICE_TOKEN + X-On-Behalf-Of → ai-on-behalf-of principal
- mcp/ → API: GraphQL, GRIOT_SERVICE_TOKEN
- backend → ai/: POST /api/webhooks/trigger, HMAC X-Trigger-Signature (dashed)
- External AI clients → mcp/: MCP stdio / Streamable HTTP (Bearer GRIOT_MCP_TOKEN)
- API → sababisha-sqlserver: TCP 1433 TDS (EF Core 8 + Dapper 2)
- API → sababisha-postgres: TCP 5432 (secondary/test only)
- API → sababisha-redis: TCP 6379
- API → Email provider: SMTP (invites/reminders/digests)
- GitHub Actions → Vercel / Railway / Trigger.dev: deploy (dashed)

ANNOTATION 1 (red frame): "AI (ai/ + mcp/) NEVER connects to SQL Server or Redis directly and holds NO DB credentials. All data access is through the backend API with GRIOT_SERVICE_TOKEN."
ANNOTATION 2 (green frame around web Copilot + ai/): "Propose-before-write: AI proposes → human approves in UI → WEB APP writes via REST (never the agent)."
ANNOTATION 3 (near backend): "ONE service layer, TWO API surfaces (REST + GraphQL), ZERO business logic in controllers/resolvers."
ANNOTATION 4 (bottom-left): "Auth [own-stack]: JWT + Argon2 + Redis; refresh rotation with FamilyId (replay-safe). Stack deviations carry [own-stack] per stack-contract.md."

STYLE: light canvas (#F7F8FA), white zone boxes with 1px hairlines (token colors only — ui-tokens.md), tech badges on every box, host corner-badges, protocols in mono, readable at 100% zoom, one page.
```

### Refine

- "Widen the API zone box so the service-layer sub-blocks read as one process."
- "Make the Vercel zone thin and the Railway zone tall (private network)."
- "Draw the db3 (Postgres) with a 'secondary/test only' badge."

---

## Definition of Done

- [ ] All 8 zones + external AI clients + email provider present
- [ ] Every arrow has protocol + auth; dirties (HMAC, deploy) dashed
- [ ] Compose service keys `sababisha-*` on the data services; stack contract respected ([own-stack] markers)
- [ ] The non-negotiable boundary (AI → API only) drawn as a red annotation
- [ ] Matches `docs/ARCHITECTURE.md` + `system-map.md` 1:1
- [ ] Approved → PNG → `diagrams/architecture/architecture-full-system.png`
| qa | all | HTTP + test harnesses | test credentials / CI tokens |
