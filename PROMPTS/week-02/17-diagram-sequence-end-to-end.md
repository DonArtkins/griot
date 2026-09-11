# Week 02 · Diagram 13 — Sequence: End-to-End (All Systems)

> Backend 09 contract: Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of` resolves a real-user OBO principal (`ai-on-behalf-of`), never a synthetic member. Exactly four scope claims are issued: ReadWorkspace/CreateTask/AddComment/CreateNotification. AI OBO bulk status, deletes, invites and member management are denied; ActivityLog persistence is planned for backend 20. See `docs/api/ai-service-token-contract.md`.

**Master spec + Figma Make paste prompts.** The **master sequence diagram** that ties the entire system together — one page, five labelled frames, every lifeline that matters: the human journey (auth → board → task → fan-out), the AI Copilot propose-before-write round-trip, the external MCP client flow, and the scheduled AI digest. Where 07/08/09 zoom into single API flows, this diagram shows **cross-system choreography** (web ↔ ai ↔ mcp ↔ backend ↔ SQL Server) in one view.

> **Contract source of truth:** `docs/ARCHITECTURE.md` §3 (request flows 3.1–3.4) · `project-kit/context/system-map.md` (boundaries) · `project-kit/context/integration-contracts.md` (tokens) · the approved ERD. Stay synced.

---

## 1. Lifelines (left → right)

1. **User / Client** (web or mobile — the human)
2. **Web App / Mobile App** (React 18 / Flutter 3.19)
3. **API — backend** (REST controllers + GraphQL resolvers → Griot.Application)
4. **SQL Server** (16 tables: Users, Workspaces, …, TaskItems, ActivityLogs, Notifications, AuditLogs)
5. **Redis** (rate-limit, refresh metadata, token budgets)
6. **ai/ agent** (Trigger.dev v4 — `griotCopilot` / scheduled)
7. **mcp/ server** (8 MCP tools)
8. **External AI client** (Claude Desktop / Cursor / Cline)
9. **Email provider** (SMTP)

## 2. Frames

**FRAME A — Login + dashboard read** (happy path, per 07):
`POST /api/auth/login` → Argon2 verify (Users) → Redis session + rate-limit → issue access JWT (15-min `sub`/`email`/`jti`) + opaque refresh (web: httpOnly cookie; mobile: secure storage) → GraphQL bootstrap (`me`, `projects`, `boards`, `tasks`). Refresh rotation + replay-race handled in 07 — reference, don't redraw.

**FRAME B — Create task + fan-out** (per 08):
`createTask {title, columnId, assigneeId?}` → BEGIN TX (UPDLOCK position lock) → INSERT TaskItems + ActivityLogs + Notifications + AuditLogs → COMMIT → Trigger-realtime notification (assignee) → 201. Assignee subscribed → realtime event; else next board load pulls.

**FRAME C — AI Copilot propose-before-write** (per 14):
User → Copilot panel → Trigger WS (stream) → ai/ reads via GraphQL (service token, ReadWorkspace) → proposal (diff card) → web approval UI (chrome-ink confirm) → **user approves** → **web app** calls REST as the user → backend writes + ActivityLogs/AuditLogs → cache refetch. Agent NEVER writes directly; every tool call logged (workspaceId, tool, payloadHash, runId).

**FRAME D — MCP external client** (per 14):
External client → mcp/: `get_board` / `create_task` / `add_comment` (stdio | Streamable HTTP Bearer `GRIOT_MCP_TOKEN`) → API REST + GraphQL with `GRIOT_SERVICE_TOKEN` + transport-bound `X-On-Behalf-Of: {real User.Id}` and active backend delegation (ai-on-behalf-of; CreateTask/AddComment scope, else 403) → SQL Server write + audit → result JSON.

**FRAME E — Scheduled digest / reminder**:
## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make (Plan mode first). It draws all five frames on one page.

```text
UML sequence diagram: Griot end-to-end system choreography. Lifelines left→right:
User/Client, Web App/Mobile App, API (backend ASP.NET Core 8), SQL Server, Redis, ai/ agents (Trigger.dev), mcp/ server, External AI client, Email provider. Draw 5 labeled frames, each with its own small caption.

FRAME A (LOGIN + READ): User → Web: POST /api/auth/login {email,password} → API: Argon2 verify (Users table) → Redis: session + rate-limit → API: issue access JWT (15min, sub/email/jti) + opaque refresh [web: Set-Cookie HttpOnly; mobile: JSON body → secure storage] → Web → API: GET /graphql bootstrap (me, projects, boards, tasks) → SQL Server → 200.

FRAME B (TASK FANOUT): Web → API: createTask {title, columnId, assigneeId?} → API: BEGIN TX; SELECT MAX(Position) UPDLOCK/HOLDLOCK; INSERT TaskItems + ActivityLogs + Notifications + AuditLogs; COMMIT → Trigger realtime → assignee client (realtime if subscribed, else next load) → 201.

FRAME C (COPILOT PROPOSE-BEFORE-WRITE, green frame): User → Web Copilot panel: "what should I finish?" → Web → ai/ WS: prompt (streaming) → ai/ → API (GRIOT_SERVICE_TOKEN): read board + tasks → API: verify ai-on-behalf-of ReadWorkspace (else 403) → SQL Server → ai/: draft proposal (diff card) → Web: render approval card (chrome-ink confirm button) → USER approves → WEB APP: PATCH /api/tasks/{id} as user → SQL Server write + ActivityLogs + AuditLogs → web: refetch cache. Annotation: "AGENT NEVER WRITES DIRECTLY — the web app executes the approved proposal as the user."

FRAME D (MCP EXTERNAL): External client → mcp/: create_task, add_comment, get_board → mcp/ (Bearer GRIOT_MCP_TOKEN) → API: REST + GraphQL GRIOT_SERVICE_TOKEN + transport-bound X-On-Behalf-Of and active backend delegation (ai-on-behalf-of; CreateTask/AddComment scopes; else 403) → SQL Server → result JSON → external client + ActivityLog row (tool, payloadHash, runId).

FRAME E (SCHEDULED): Trigger cron → ai/: dueReminders/sprintDigest → ai/ → API: GraphQL notifications → SQL Server → email provider: weekly digest → user inbox.

TOP ANNOTATION (red): "AI writes always via backend API with GRIOT_SERVICE_TOKEN — never to SQL Server/Redis directly."
MIDDLE ANNOTATION (green): "Propose-before-write: propose → approve → web-app writes (never the agent). Recovery: 401 → refresh (once) → retry."
STYLE: mono protocol labels, dashed HMAC + deploy edges, light canvas (#F7F8FA), token colors only (ui-tokens.md), one page.
```

### Refine

- "Move Frame C to the center and make it the visual hero."
- "Draw the realtime WS line thicker (streaming)."
- "Grey out Frame E (scheduled background job) so the interactive flows stand out."

---

## Definition of Done

- [ ] Five frames drawn with all 9 lifelines; no missing actors
- [ ] The non-negotiable boundary (AI → API only) + propose-before-write annotations present
- [ ] GRIOT_SERVICE_TOKEN + GRIOT_MCP_TOKEN + scope check visible on the right edges
- [ ] Matches `docs/ARCHITECTURE.md` §3 (flows 3.1–3.4) and `system-map.md`
- [ ] Approved → PNG → `diagrams/architecture/sequence-end-to-end.png`
Trigger cron → ai/ (`dueReminders` / `sprintDigest`) → API (service token) → Notifications → SQL Server → email provider → inbox.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

The current REST transport uses JSON refresh tokens for Postman/mobile. Web
HttpOnly cookie transport in the design remains a backend prerequisite for web
Feature 05; do not treat the cookie diagrams as live behavior or store tokens in
localStorage. SQL Server owns refresh rows; Redis currently owns login limits.
