# AI Integration — Griot Copilot, AI Agents & MCP (Trigger.dev v4)

> **Status: `[own-stack]` extension.** The bootcamp PDF defines no AI layer. This doc adds one that *wraps* the bootcamp stack and never replaces it (Now includes Level 4 Autonomous Reasoning, System Reports, and OTP 2FA per ai-features-research.md): .NET 8 + SQL Server stays the single source of truth; AI services read/write it **only through the API**. Everyone on the programme builds the mandated stack — this is the differentiator bolted on top.
> **Why now:** 2025–26 is the era of AI agents, MCP (Model Context Protocol) and agent-powered UIs. Apps ship in-app copilots, chat-with-your-data, and connected "skills". Griot is a PM tool — a category where AI has *obvious* product value (sprint summaries, blocked-work detection, standups, task drafting). Adding it also makes the capstone portfolio-grade.

---

## 1. What we're building (the product story)

1. **Griot Copilot (in-app)** — a chat panel in the App Shell. Ask "what's blocked this week?", "summarize the Design project", "draft 3 tasks from this brief". Answers stream into the UI (chat-style) and can **act** on approved delegated task/comment creation (status changes are submitted separately by the human app) — always visually confirmed before mutation.
2. **Proactive agents (background workflows)** — scheduled Trigger.dev tasks: daily **due-date reminders**, weekly **sprint digest**, **stale-board detection** ("This task has been In Review for 9 days"), Monday **standup builder** from last week's board changes.
3. **MCP server for Griot** — exposes the product's data as MCP **tools** so *external* AI agents can work on Griot data: Claude Desktop, Cursor, VS Code Copilot, Cline, or any MCP-compatible client → `list_projects`, `get_board`, `create_task`, etc.
4. **MCP clients inside Griot (skills)** — Griot's own agents connect to external MCP servers (GitHub, Slack, Notion, Linear) for richer workflows: "post the digest to Slack", "link the GitHub PR to the task".

**One rule everywhere:** AI never writes to SQL Server directly — every read/write goes through the .NET/GraphQL API with a scoped service token. Deterministic, auditable, and it keeps the bootcamp backend as the owner of data.

---

## 2. Architecture

```
                ┌──────────────────────────── Browser ─────────────────────────────┐
                │  web/ (Vite + React + MUI)                                       │
                │   · Copilot panel (real-time stream)  · /app/*                   │
                └──────┬───────────────────────────────────┬───────────────────────┘
                        │  GraphQL/REST (Bearer)           │  Trigger realtime (WS)
                        ▼                                  ▼
        ┌─────────────────────────────┐        ┌──────────────────────────────┐
        │  src/  ASP.NET Core 8 API   │        │  ai/  Trigger.dev v4 (Node 20)│
        │  REST + HotChocolate GraphQL│◄──────►│  agents.json · tasks · skills │
        │  SQL Server 2022 (source)   │  HTTP  │  MCP client connections       │
        │  /api/webhooks/trigger      │ -------│  LLM SDK (OpenAI/Anthropic)   │
        └─────────────▲───────────────┘  HMAC  └──────────────┬───────────────┘
                      │ GraphQL (service token)               │ runs scheduled + on-demand
        ┌─────────────┴───────────────┐                       │
        │  mcp/  Griot MCP Server     │◄─────────────────────┘ (agents also call it)
        │  @modelcontextprotocol/sdk  │      Streamable HTTP / stdio
        │  tools backed by GraphQL    │◄──── external AI clients: Claude/Cursor/Cline
        └─────────────────────────────┘
```

Data flow: **Copilot prompt → `ai/` agent → (tool calls) → .NET GraphQL → SQL Server → results streamed back.**

## 2b. AI Superpowers (2026-09-11 user wave — all PLANNED)

Beyond the Copilot single-tool calls, the AI layer gains three user-commanded superpowers, each as a scoped, typed, audited capability:

1. **Knowledge agent + system auditor (ai 06)** — ask anything about the system (answers grounded in real API rows with citations) and run whole-system audits (stale tasks, incomplete evidence, permitted incident symptoms — verification state such as 2FA is intentionally never exposed to AI, spec 23) that persist as Report rows. Read-only data plane; RBAC respected; never touches auth/OTP.
2. **Report generation (ai 07)** — typed reports (CAB, QA/test, post-deployment, regression, sprint/status and requested action-summary templates (backend 24 registry)) as **CSV always + PDF (A4, brand tokens, text layer)**; numbers are deterministic aggregations in Node — the LLM writes only narrative; artifacts stored via blob (backend 11) and surfaced through backend 24; scope and coverage are explicit without disclosing inaccessible-row counts.
3. **Advanced executor (ai 08)** — Level-4 planning loop (PLAN → human gate → ACT → OBSERVE → summarize-as-report) for explicitly delegated operations only, with full-plan approval, idempotency keys, typed tool-output validation, confidence thresholds (clarify instead of guess), step observability, and prompt-injection-neutralized board content.

**Boundaries that scaling does not move:** no OTP/auth/MFA/delete/invite/member tools ever (backend 23 is human-only; the OBO principal is 403); report-row creation rides the new scoped `CreateReport` capability (backend 24) — the OBO grant is never loosened; after backend 20 ships, runs are attributable through ApiLogs/AuditLogs with the real user id + runId (backend 20). MCP exposes the same tools to external clients (mcp 06). Trivial lookups short-circuit: rule-based agents answer from a cached snapshot; only genuinely generative turns call the LLM (cost control).

### 2a. Orchestration contract — Trigger.dev as a separate service, orchestrated by .NET (authoritative)

This subsection is the **binding integration contract** for how the AI layer attaches to the bootcamp stack (researched and ratified before backend spec 09 implementation; all AGENTS files, project kits, and specs across systems reference it).

**Architecture:**
- **Trigger.dev tasks (TypeScript)** — a standalone Node/TS project (`ai/`, own `.nvmrc` 20 + own lockfile), deployed independently (Trigger cloud only). This is where AI calls and long-running background work actually execute.
- **ASP.NET Core backend** — stays the source of truth for domain data. It triggers jobs via Trigger.dev's REST API/SDK (`POST` to trigger a task by ID) whenever something async or AI-related needs to happen. It is the only trigger source for request-originated work.
- **Scheduled agents (the one explicit exception)** — `dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder` are started by Trigger.dev's own cron scheduler, because no user request exists to originate them. The backend does NOT enqueue these. They remain non-data-owners: each scheduled run reads through backend GraphQL with `GRIOT_SERVICE_TOKEN` and writes back only via the .NET API (`POST /api/webhooks/trigger` HMAC or service-token REST). Beyond the backend and these schedules, no trigger source exists.
- **React/Vite frontend (and Flutter mobile)** — **never touch Trigger.dev for triggering or status.** They only call the .NET API, which internally kicks off Trigger.dev tasks. The one exception is the Copilot **output stream**: the web panel consumes Trigger's realtime WS with a scoped access token (read-only delivery channel — no triggering, no status polling, no public API calls).

**Why AI integration fits in Trigger.dev, not C#:**
- AI SDKs (OpenAI, Anthropic, Vercel AI SDK, LangChain.js) are TS/JS-first — richer, faster-updated tooling than the .NET equivalents.
- AI calls are often slow/streaming/retryable — exactly what Trigger.dev is built for (built-in retries, concurrency controls, run visibility, waitpoints for human-in-the-loop steps).
- Keeps prompt orchestration, streaming, and model-provider logic out of the core domain API, so the .NET backend doesn't become a dumping ground for AI SDK churn.

**Typical flow:**
1. User action hits the React frontend (or Flutter app) → calls the .NET API.
2. .NET API validates/persists the request, then calls Trigger.dev to enqueue a task (e.g. `generateSummary`, `processDocument`, `sendNotificationBatch`).
3. Trigger.dev task runs (calls OpenAI/Anthropic, does the heavy lifting, handles retries).
4. Task calls **back into the .NET API** (authenticated HTTP callback → `POST /api/webhooks/trigger`, HMAC-verified, or service-token REST) to write results back into the DB — **Trigger.dev never owns domain/ledger data.**
5. Frontend polls the .NET API or uses SignalR/websockets for status — never Trigger.dev's dashboard/API directly.

**Non-negotiables (keep it clean):**
1. **.NET remains the only writer of source-of-truth data** (tenant-scoped, immutable where relevant) — Trigger.dev is a compute/orchestration adapter, **not a data owner**. AI still never connects to SQL Server directly (rule above).
2. **Auth between .NET ↔ Trigger.dev is a server-to-server secret/API key** (`TRIGGER_SECRET_KEY` on the backend trigger side, `WEBHOOK_SECRET` for HMAC callbacks — read by `WebhookController` as `Webhook:Secret ?? WEBHOOK_SECRET`) — never exposed to the frontend or mobile.
3. **Don't let the frontend call Trigger.dev's public API even for "simple" cases** — it breaks the single-API-surface pattern and duplicates auth logic. One authoritative entry point: the .NET API, for web and mobile alike.


---

## 3. Folders & repos (all inside the GTP tree — isolation preserved)

```
griot/
├── src/            # .NET 8 API — UNCHANGED core (bootcamp)
├── web/            # Vite/React/MUI — UNCHANGED core (bootcamp) + features/copilot/
├── mobile/         # Flutter — UNCHANGED core (bootcamp)
├── ai/             # NEW — Trigger.dev v4 project (Node 20, own .nvmrc, own lockfile)
│   ├── trigger.config.ts
│   └── src/
│       ├── agents/      # griotCopilot.ts, standupWriter.ts, taskSplitter.ts
│       ├── tasks/       # digests.ts, reminders.ts, staleBoard.ts
│       └── mcp/         # external-skill connections (slack, github, notion)
├── mcp/            # NEW — Griot MCP server (Node 20, own package, Docker image)
│   └── src/tools/  # listProjects.ts, getBoard.ts, createTask.ts, updateStatus.ts…
└── docker-compose.yml   # + mcp service (streamable-http mode)
```

Both new dirs are **type-checked, linted, and tested inside the same GitHub Actions pipeline** with their own lockfiles — the same isolation contract as everything else.

---

## 4. Setup (Parrot, non-invasive)

```bash
cd ~/sababisha/projects/gtp/griot/ai
# Node 20 auto-switches via .nvmrc (no manual nvm use needed)
npx trigger.dev@latest init --skip-package-install --project-ref <PROJECT_REF> --api-url https://api.trigger.dev
# generates trigger.config.ts + .trigger/ registry + src/ sample task
echo "ANTHROPIC_API_KEY=..." >> .env   # or OPENAI_API_KEY — both supported
npm ci # retain the reviewed 4.5.16 CLI/SDK/react-hooks set

cd ~/sababisha/projects/gtp/griot/mcp
npm init -y && npm i @modelcontextprotocol/sdk zod
# optionally: npx @modelcontextprotocol/inspector node src/server.js   (MCP Inspector GUI)
```

> Keys live only in the `ai/` project environment (never in `web/`). Costs stay capped via a per-project daily budget + Redis counters (see §7).

## 5. Trigger.dev: scheduled workflows + on-demand agents

The runtime dependency contract is Trigger.dev v4 [own-stack], with CLI/SDK/react-hooks pinned to 4.5.16 (AI 01). This preserves commit `7e90c5b`; the [vendor migration notice](https://trigger.dev/docs/migrating-from-v3) rules out cloud v3 after July 1, 2026. Use `@trigger.dev/sdk` imports and the installed CLI after `npm ci`.

Planned execution sequence (not a runnable SDK example):

1. The backend authorizes and stores schedules. Cron resolves that trusted schedule and a live delegation; it never accepts a model-selected user or service credential.
2. A due-reminder task reads permitted backend data. Notification writes require backend 22; durable recovery requires backend 20. Neither exists merely because CreateNotification is a reserved scope.
3. Copilot tasks call a separately verified LLM orchestration API and permission-scoped read tools. Mutations are proposals approved and executed by the app. No task-update, auth, delete or invite capability is granted to the model.

Verify scheduled-task and LLM-loop APIs against the pinned dependencies during AI 01/02; do not infer an `agent` or `tool` export from Trigger's task API.

## 6. Griot MCP server (the "Griot skills" external agents can use)

```ts
// mcp/src/server.ts
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";

export const server = new McpServer({ name: "griot", version: "0.1.0" });

server.tool("get_board", { boardId: z.string() }, async ({ boardId }) => {
  const data = await graphql.query(getBoardQuery, { boardId }, { token: await serviceToken() });
  return { content: [{ type: "text", text: JSON.stringify(data) }] };
});
server.tool("create_task", { title: z.string(), columnId: z.string() }, async (input) => {
  // mutation via .NET GraphQL with GRIOT_SERVICE_TOKEN → authoritative writes only
  return { content: [{ type: "text", text: JSON.stringify((await graphql.mutate(createTaskMutation, input)).data) }] };
});
// stdio (local CLIs) OR Streamable HTTP (Docker/Railway + web apps)
```

**Tool roster (v1):** `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `add_comment`, `get_activity_feed`, `summarize_project` (deterministic backend-data aggregation over permitted reads — not a direct agent/LLM call). **Clients:** Claude Desktop (`claude mcp add`), Cursor (`@Griot /mcp`), VS Code Copilot, Cline, plus Griot's own agents.

## 7. Security & cost guardrails (also the Week-6 OWASP surface)

- **Service-to-service auth**: static long-lived `GRIOT_SERVICE_TOKEN` validated on the .NET side → with trusted `X-On-Behalf-Of: {real User.Id}` plus an unexpired server-side grant (ServiceToken:Delegations:{userId}, workspace IDs, scopes, UTC expiry) it resolves to a **real-user On-Behalf-Of (OBO) principal** (role `ai-on-behalf-of`; scope claims `ReadWorkspace`, `CreateTask`, `AddComment`, `CreateNotification` — no deletes, no invites, no member management), NOT a virtual `ai-agent` member. Contract: `docs/api/ai-service-token-contract.md`.
- **Prompt-injection mitigation**: user text is treated as **data, not instructions**; tools apply their own project/workspace scoping; the agent system prompt bans tool-call modification of unrelated entities.
- **Level 4 Reasoning & Mutation confirmation**: the agent operates a Level 4 reasoning loop, capable of planning multi-step actions. It returns a *proposed* action plan; the Copilot UI renders it ("Create task *…* in column *…*?") for the human to approve. Deterministic digest/reminder deliveries skip per-run approval only for a backend-stored authorized schedule with idempotency; an OBO identity alone cannot create or authorize delivery. Incident alerts use backend 27's fixed operator-provisioned policy/audience; notice drafts still require human confirmation.
- **Token/cost caps**: daily token budget per workspace recorded in Redis; alarms on over-budget runs.
- **Audit**: every tool call logged with `workspaceId`, `tool`, `payloadHash`, `runId` — traceable across Trigger run ↔ MCP call ↔ .NET audit log.

## 8. Testing the AI layer (feeds week-06)

- **Golden-transcript tests**: fixed conversations + **mocked LLM client** (injected at construction; no network in CI) → assert the tool-call sequence.
- **MCP tool tests**: each tool is a pure function `(graphqlClient, input) → output`; unit-test the JSON contract; plus an MCP Inspector smoke pass (manual).
- **Contract gate**: both new Node services run `npm run lint && npm run typecheck && npm test` in the same GitHub Actions job that gates the .NET code.
- **E2E (Cypress)**: chat panel with a **stubbed copilot** (MSW intercept) so CI never pays LLM latency/cost.

## 9. Definition of Done — AI layer

- [ ] `ai/` Trigger.dev project created, connected, and `griotCopilot` agent answering from board data
- [ ] Scheduled: `dueReminders`, `sprintDigest`, `staleBoard` running on schedule in dev
- [ ] Web Copilot panel streaming responses + approving multi-step plans before write
- [ ] System Reports integrated into ad-hoc and scheduled workflows
- [ ] `mcp/` server runnable in stdio + Streamable HTTP; `get_board`/`create_task` verified in Claude Desktop (or Cursor)
- [ ] External skill: Slack digest path working (or GitHub PR linking)
- [ ] OWASP review logged for the AI principal + tool-call surface
- [ ] Cost cap + token budget enforced and observed
- [ ] Tests: golden transcripts, MCP tool tests, Cypress stub — all green in CI

---
**Griot Copilot — the team's history, on demand.**