# Week 02 · Diagram 11 — Data Flow Diagram (DFD) — Entire System

> Backend 09 contract: Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of` resolves a real-user OBO principal (`ai-on-behalf-of`), never a synthetic member. Exactly four scope claims are issued: ReadWorkspace/CreateTask/AddComment/CreateNotification. AI OBO bulk status, deletes, invites and member management are denied; ActivityLog persistence is planned for backend 20. See `docs/api/ai-service-token-contract.md`.

**Master spec + Figma Make paste prompts.** This is the **whole-system Data Flow Diagram (Level 0 + Level 1)**. It answers *"what data moves between which actors, processes, and stores?"* — every external entity, every process, every store, every labelled flow. Draw it after the C4 context (04) and before the full architecture (16): the DFD is the data-moving contract, the architecture prompt is the component/layout contract.

> **Contract source of truth:** `project-kit/context/system-map.md` (7 systems + boundaries) · `project-kit/context/integration-contracts.md` (ports, env, tokens) · `project-kit/context/ui-tokens.md` + `docs/design/MASTER-DESIGN-SYSTEM.md` (design language) · the approved ERD (`diagrams/erd/`, 16 tables + 5 enums). This diagram must stay synced with those files.

---

## 1. External entities (terminators — rectangles)

| Entity | Type | Touches |
|---|---|---|
| Workspace Owner | Human (web/mobile) | Auth, workspaces, projects, boards, tasks, members, invites, logs |
| Team Member | Human (web/mobile) | Boards, tasks, comments, notifications, activity feed |
| External AI clients | External system (Claude Desktop / Cursor / Cline / any MCP client) | MCP tools only |
| Email provider | External SaaS (SMTP/transactional) | Invites, due-date reminders, digest delivery (AI-scheduled) |
| GitHub Actions | External CI | Builds + tests (qa suites) + deploys to Vercel / Railway / Trigger.dev |
| Vercel | External host | Static web (Public + App shells); NOT an API proxy |
| Railway | External host | `api`, `mcp`, `sababisha-sqlserver`, `sababisha-postgres`, `sababisha-redis` |
| Trigger.dev | External host | `ai/` scheduled agents + Copilot background runs |
| Postman / Newman | External tool | Contract-test client (REST + GraphQL) |

## 2. Processes (circles / rounded rects in DFD notation)

| # | Process | Owns | Tech |
|---|---|---|---|
| P1 | Web App | Public shell, App shell, Copilot panel, client cache (Apollo/TanStack) | React 18 · Vite 5 · MUI v6 |
| P2 | Mobile App | Android companion — same endpoints as web | Flutter 3.19 / Dart 3 |
| P3 | API (one process) | REST `/api/*` + GraphQL `/graphql` + `/health`; Auth, Workspace, Project, Board, Task, Comment, Attachment, Notification, Dashboard, Webhook | ASP.NET Core 8 · Griot.Application · EF Core 8 + Dapper 2 |
| P4 | MCP Server | 9 tools (list_projects … summarize_project); stdio + Streamable HTTP | Node 20 + @modelcontextprotocol/sdk |
| P5 | AI Agents | `griotCopilot` + scheduled (`dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`); streams to web via Trigger realtime | Trigger.dev v3 |
| P6 | Email dispatch | Invites (user-triggered), reminders + digests (AI-triggered) | SMTP provider |

## 3. Data stores (open-sided rectangles / cylinders)

| # | Store | Contains | Access |
|---|---|---|---|
| D1 | SQL Server 2022 (sababisha-sqlserver) | 16 tables: `Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`, `ApiLogs`, `ErrorLogs`, `AuditLogs` | ONLY P3 (API). **AI never touches it directly.** |
| D2 | Redis 7 (sababisha-redis) | rate-limit windows, refresh-token metadata, token budgets (LLM cost caps) | P3 (API); budget reads via P3 only |
| D3 | PostgreSQL 16 (sababisha-postgres) | secondary/test store | P3 (API) only |

## 4. Data flows (label EVERY arrow)

| # | From → To | Data / label |
|---|---|---|
| F1 | Workspace Owner / Team Member → P1/P2 | credentials (email+password), workspace + board + task operations, comments |
| F2 | P1/P2 → P3 | HTTPS REST + GraphQL, JWT Bearer (`sub`, `email`, `jti`) |
| F3 | P3 → D1/D2/D3 | T-SQL (EF Core 8 + Dapper 2: `usp_BulkUpdateTaskStatus`, `usp_GetDashboardSummary`); Redis TCP |
| F4 | P3 → P1/P2 | 200/201 responses + `Set-Cookie refreshToken` (web) / JSON body (mobile) |
| F5 | P3 → Clients | 401/404/403 rejection semantics (404 never discloses existence) |
| F6 | P1 → P5 | Trigger realtime WebSocket (Copilot stream + notification fan-out) |
| F7 | P5 / P4 → P3 | GraphQL only, `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of` → `ai-on-behalf-of` principal (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes, no invites) |
| F8 | P4 → External AI clients | MCP tool results (stdio local / Streamable HTTP with `Authorization: Bearer GRIOT_MCP_TOKEN`) |
| F9 | External AI clients → P4 | MCP tool calls (get_board, create_task, update_task_status, add_comment, summarize_project, …) |
| F10 | P3 → Email provider | SMTP invites/reminders/digests (digest payload authored by P5 scheduled jobs) |
| F11 | GitHub Actions → Vercel / Railway / Trigger.dev | deploy triggers (dashed — indirect) |
| F12 | P3 → P5 | HMAC webhook return (`POST /api/webhooks/trigger`, `X-Trigger-Signature`) — dashed |
| F14 | P3 → D1 (audit) | every state-changing write also → `ActivityLogs` + `AuditLogs` (workspaceId, tool, payloadHash, runId) |
## 5. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make (Plan mode first). It draws the entire DFD in one pass.

```text
Data Flow Diagram (Level 0 + Level 1) for Griot, a project-management web app + AI copilot. One page, left-to-right flow, DFD notation: external entities = rectangles, processes = circles (numbered P1–P6), data stores = open-sided rectangles (numbered D1–D3). Every arrow labelled with the flow number + payload (F1–F14).

EXTERNAL ENTITIES (left edge): Workspace Owner (human), Team Member (human), External AI clients (Claude Desktop / Cursor / Cline), Postman/Newman (contract-test client), GitHub Actions (CI), Email provider (SMTP), Vercel (static host), Railway (API + DB host), Trigger.dev (AI host).

PROCESSES (center, left→right):
- P1 Web App — React 18 · Vite 5 · MUI v6 (Public shell, App shell, Copilot panel)
- P2 Mobile App — Flutter 3.19 (Android companion, same endpoints)
- P3 API — ASP.NET Core 8: REST /api/* + GraphQL /graphql + /health; Griot.Application services; EF Core 8 + Dapper 2
- P4 MCP Server — Node 20, 9 tools, stdio + Streamable HTTP
- P5 AI Agents — Trigger.dev v3: griotCopilot + dueReminders, sprintDigest, staleBoard, standupBuilder
- P6 Email dispatch — SMTP provider integration

DATA STORES (right edge): D1 SQL Server 2022 (sababisha-sqlserver) — 16 tables: Users, Workspaces, WorkspaceMembers, Invites, Projects, Boards, Columns, TaskItems, Comments, Attachments, ActivityLogs, Notifications, RefreshTokens, ApiLogs, ErrorLogs, AuditLogs. D2 Redis 7 (sababisha-redis) — rate limits, refresh metadata, token budgets. D3 PostgreSQL 16 (sababisha-postgres) — secondary/test.

ARROWS (label each with flow # + payload):
F1 Owner/Member → P1/P2 (credentials, board/task ops, comments)
F2 P1/P2 → P3 (HTTPS REST + GraphQL, JWT Bearer)
F3 P3 → D1/D2/D3 (T-SQL: EF Core 8 + Dapper usp_BulkUpdateTaskStatus + usp_GetDashboardSummary; Redis)
F4 P3 → P1/P2 (responses; web Set-Cookie refreshToken HttpOnly; mobile JSON body)
F6 P1 → P5 (Trigger realtime WebSocket — Copilot stream + notification fan-out)
F7 P5/P4 → P3 (GraphQL, GRIOT_SERVICE_TOKEN + X-On-Behalf-Of → ai-on-behalf-of principal)
F8 P4 → External AI clients (MCP results)
F9 External AI clients → P4 (MCP tool calls)
F10 P3 → Email provider (SMTP invites/reminders/digests)
F11 GitHub Actions → Vercel/Railway/Trigger.dev (deploy, dashed)
F12 P3 → P5 (HMAC webhook POST /api/webhooks/trigger, dashed)
F14 P3 → D1 (ActivityLogs + AuditLogs writes, payloadHash + runId)

ANNOTATION 1 (red callout attached to D1): "AI (P4 + P5) NEVER writes to SQL Server directly — every read/write goes through P3 (API) with GRIOT_SERVICE_TOKEN + X-On-Behalf-Of → ai-on-behalf-of principal. No deletes, no invites, workspace-scoped."
ANNOTATION 2 (green callout attached to P1 + P5): "Propose-before-write: AI proposes → human approves in the web UI → the WEB APP performs the write via REST (never the agent)."
ANNOTATION 3 (blue callout near F6): "Notification fan-out: web = Trigger realtime; mobile = poll on focus + pull-to-refresh; NO email in v1 (AI digest owns email)."
ANNOTATION 4 (small note bottom): "Compose service keys: api, mcp, sababisha-sqlserver, sababisha-postgres, sababisha-redis (integration-contracts.md). Denied = 404 not 403 on ownership scopes."

STYLE: light canvas (#F7F8FA), white process boxes with 1px hairlines, token-named fills only (per ui-tokens.md — no raw ad-hoc colors), mono labels for flow IDs, readable at 100% zoom, one page.
```

### Refine

- "Move P3 to dead center; widen it so the service-layer name is readable."
- "Draw F12 dashed (webhook return path)."
- "Make D1 larger than D2/D3 and label it 'source of truth'."

---

## Definition of Done

- [ ] All 6 processes, 3 data stores, 9 external entities present
- [ ] Every flow F1–F14 drawn with payload label; no unlabelled edges
- [ ] "AI never writes to SQL Server directly" + propose-before-write + fan-out + sababisha-* naming annotations present
- [ ] Store names match `integration-contracts.md` (`sababisha-*`); table names match the ERD exactly
- [ ] Approved → PNG → `diagrams/architecture/dfd-system.png`
