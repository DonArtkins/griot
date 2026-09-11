# Week 02 · Diagram 10 — AI System Context (Web · AI · MCP · Backend)

> Backend 09 contract: Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of` resolves a real-user OBO principal (`ai-on-behalf-of`), never a synthetic member. Exactly four scope claims are issued: ReadWorkspace/CreateTask/AddComment/CreateNotification. AI OBO bulk status, deletes, invites and member management are denied; ActivityLog persistence is planned for backend 20. See `docs/api/ai-service-token-contract.md`. **2026-09-11 wave (PLANNED):** a FIFTH scope `CreateReport` (backend 24) powers report/audit superpowers — knowledge agent + system auditor (ai 06), reports PDF/CSV (ai 07), advanced executor (ai 08); auth/OTP step-up (backend 23) is human-only and AI is 403 there.

**Master spec + Figma Make paste prompts.** This diagram captures the ENTIRE AI flow: web Copilot → Trigger agents → GraphQL (service token) → backend → SQL Server, plus external AI clients through MCP. Everything the AI layer touches, in one detailed diagram.

---

## 1. Actors / components

- **Web Copilot panel** (app shell right rail) — streams answers + renders approval cards; mutations approved → app calls REST itself.
- **AI Agents (Trigger.dev v4)** — `griotCopilot` agent + scheduled tasks (`dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`); streaming to web via realtime WS.
- **MCP Server** (`mcp/`) — tools `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `add_comment`, `get_activity_feed`, `summarize_project`; stdio + Streamable HTTP.
- **Backend API** — GraphQL with Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of: {real User.Id}` resolves a real-user OBO principal (`ai-on-behalf-of`). Exactly four scopes are issued: ReadWorkspace, CreateTask, AddComment, CreateNotification; there is no `UpdateTaskStatus` scope. Status updates use existing task APIs and real-user RBAC; bulk status, deletes, invites and member management are denied to AI OBO callers. In-app Copilot writes remain propose-only and execute as the user after approval. HMAC protects `/api/webhooks/trigger`.
- **SQL Server** — source of truth; **AI never touches directly**.
- **External AI clients** — Claude Desktop / Cursor / Cline / any MCP client.
- **ActivityLogs/AuditLogs** — the AI audit trail (every tool call).

## 2. Flows to draw

1. **Copilot chat**: user asks in web → `ai/` agent (stream via realtime) → tool call → backend GraphQL (service token) → SQL Server → stream answer back.
2. **Propose-before-write**: agent returns a proposal → web renders approval card → user approves → **web app itself** calls REST `POST /tasks` → backend writes → caches update.
3. **Scheduled**: Trigger cron → agent → GraphQL (service token) → notifications via backend.
4. **MCP read/write**: external AI client → MCP tool → backend GraphQL (service token) → SQL Server → result JSON to client.
5. **Audit**: every tool call → ActivityLogs/AuditLogs row (workspaceId, tool, payloadHash, runId).

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the entire AI flow (components + all 5 flows + annotations) in one pass.

```text
AI system context diagram for Griot. Build it with these components and flows:

COMPONENTS:
- Left group "WEB": box "Web Copilot panel (app-shell right rail, chat UI)"; box "External AI clients (Claude Desktop / Cursor / Cline)".
- Center group "ai/ (Trigger.dev v4)": box "griotCopilot agent" + box "scheduled: dueReminders, sprintDigest, staleBoard, standupBuilder"; small box "token budget (Redis)" attached.
- Lower-left group "mcp/": box "Griot MCP server" listing the 8 tools: list_projects, list_boards, get_board, get_task, create_task, add_comment, get_activity_feed, summarize_project.
- Right group "backend": box "API — GraphQL + /api/webhooks/trigger" with badges "GRIOT_SERVICE_TOKEN + X-On-Behalf-Of → real-user OBO principal (ai-on-behalf-of): ReadWorkspace, CreateTask, AddComment, CreateNotification" and "HMAC X-Trigger-Signature". Show no synthetic member; CreateReport is planned for backend 24 but is not active. Bulk status, deletes, invites and member management are denied.
- Far right: box "SQL Server 2022 (source of truth)" with a BIG RED X annotation "AI NEVER writes to SQL Server directly — every read/write through the API".

ARROWS (label each):
- web Copilot → ai/ agents (Trigger realtime WebSocket, streaming)
- ai/ agents → backend API (GraphQL, GRIOT_SERVICE_TOKEN)
- backend API → ai/ agents (HMAC webhook return, dashed)
- external AI clients → mcp/ server (MCP stdio / Streamable HTTP)
- mcp/ server → backend API (GraphQL, GRIOT_SERVICE_TOKEN)
- backend API → SQL Server (T-SQL)

ANNOTATION 1 (dashed green frame around web Copilot + ai/ agents):
"PROPOSE-BEFORE-WRITE: agent returns a proposed action → human approves in the UI → the WEB APP performs the write via REST (never the agent)."

ANNOTATION 2 (dashed blue note near ActivityLogs/AuditLogs):
"Every tool call logged: workspaceId, tool, payloadHash, runId — traceable across Trigger run ↔ MCP call ↔ backend ActivityLog (audit + summarize_project source)."

ANNOTATION 3 (under scheduled agents):
"Deterministic paths (digest/reminders) skip approval; generative mutations require human approval."
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

### Refine

- "Change the SQL Server arrow color to red X (no direct write)."
- "Add a small box 'token budget (Redis)' next to the agents."

---

## Definition of Done

- [ ] Copilot chat, scheduled agents, MCP, backend, SQL Server all visible
- [ ] Service-token + ai-on-behalf-of scope + HMAC drawn; SQL Server has "no direct AI write" mark
- [ ] Propose-before-write + audit annotations present
- [ ] Approved → PNG → `diagrams/architecture/ai-system-context.png`
