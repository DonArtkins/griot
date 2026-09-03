# Week 02 · Diagram 10 — AI System Context (Web · AI · MCP · Backend)

**Master spec + Figma Make paste prompts.** This diagram captures the ENTIRE AI flow: web Copilot → Trigger agents → GraphQL (service token) → backend → SQL Server, plus external AI clients through MCP. Everything the AI layer touches, in one detailed diagram.

---

## 1. Actors / components

- **Web Copilot panel** (app shell right rail) — streams answers + renders approval cards; mutations approved → app calls REST itself.
- **AI Agents (Trigger.dev v3)** — `griotCopilot` agent + scheduled tasks (`dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`); streaming to web via realtime WS.
- **MCP Server** (`mcp/`) — tools `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project`; stdio + Streamable HTTP.
- **Backend API** — GraphQL surface with `GRIOT_SERVICE_TOKEN` → `ai-agent` principal (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes/invites); HMAC `/api/webhooks/trigger`.
- **SQL Server** — source of truth; **AI never touches directly**.
- **External AI clients** — Claude Desktop / Cursor / Cline / any MCP client.
- **ActivityLogs/AuditLogs** — the AI audit trail (every tool call).

## 2. Flows to draw

1. **Copilot chat**: user asks in web → `ai/` agent (stream via realtime) → tool call → backend GraphQL (service token) → SQL Server → stream answer back.
2. **Propose-before-write**: agent returns a proposal → web renders approval card → user approves → **web app itself** calls REST `POST /tasks` → backend writes → caches update.
3. **Scheduled**: Trigger cron → agent → GraphQL (service token) → notifications via backend.
4. **MCP read/write**: external AI client → MCP tool → backend GraphQL (service token) → SQL Server → result JSON to client.
5. **Audit**: every tool call → ActivityLogs/AuditLogs row (workspaceId, tool, payloadHash, runId).

## 3. Figma Make prompts

### PROMPT A

```
AI system context diagram for Griot. Left: web Copilot panel (chat UI) + external AI clients (Claude/Cursor/Cline). Center: ai/ Trigger.dev agents (griotCopilot + 4 scheduled: dueReminders, sprintDigest, staleBoard, standupBuilder) with a realtime WebSocket arrow to web Copilot. Below: mcp/ server box listing the 9 tools. Right: backend API box with 'GraphQL + GRIOT_SERVICE_TOKEN → ai-agent principal (no deletes/invites)' and 'HMAC /api/webhooks/trigger'. Far right: SQL Server box with a big red X annotation 'AI NEVER writes to SQL Server directly — every read/write through the API'.
Arrows: web->ai (WS stream); ai->backend (GraphQL, service token); ai->backend (HMAC webhook return); mcp->backend (GraphQL, service token); external clients->mcp (MCP stdio/HTTP); backend->SQL Server (T-SQL).
```

### PROMPT B — propose-before-write + audit

```
Add two annotations to the AI context diagram:
1) A dashed green frame around web Copilot + ai/ agents labeled "PROPOSE-BEFORE-WRITE: agent returns proposed action → human approves in UI → the WEB APP performs the write via REST (never the agent)". Connect to the web Copilot box.
2) A dashed blue note near ActivityLogs/AuditLogs: "Every tool call logged: workspaceId, tool, payloadHash, runId — traceable across Trigger run ↔ MCP call ↔ backend ActivityLog (audit + summarize_project source)".
```

### Fix snippets

- "Change the SQL Server arrow color to red X (no direct write)."
- "Add a small box 'token budget (Redis)' next to the agents."

---

## Definition of Done

- [ ] Copilot chat, scheduled agents, MCP, backend, SQL Server all visible
- [ ] Service-token + ai-agent scope + HMAC drawn; SQL Server has "no direct AI write" mark
- [ ] Propose-before-write + audit annotations present
- [ ] Approved → PNG → `project-kit/diagrams/architecture/ai-system-context.png`