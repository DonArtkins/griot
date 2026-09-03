# AI Integration — Griot Copilot, AI Agents & MCP (Trigger.dev v3)

> **Status: `[own-stack]` extension.** The bootcamp PDF defines no AI layer. This doc adds one that *wraps* the bootcamp stack and never replaces it: .NET 8 + SQL Server stays the single source of truth; AI services read/write it **only through the API**. Everyone on the programme builds the mandated stack — this is the differentiator bolted on top.
> **Why now:** 2025–26 is the era of AI agents, MCP (Model Context Protocol) and agent-powered UIs. Apps ship in-app copilots, chat-with-your-data, and connected "skills". Griot is a PM tool — a category where AI has *obvious* product value (sprint summaries, blocked-work detection, standups, task drafting). Adding it also makes the capstone portfolio-grade.

---

## 1. What we're building (the product story)

1. **Griot Copilot (in-app)** — a chat panel in the App Shell. Ask "what's blocked this week?", "summarize the Design project", "draft 3 tasks from this brief". Answers stream into the UI (chat-style) and can **act** (create tasks, change status) — always visually confirmed before mutation.
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
        │  src/  ASP.NET Core 8 API   │        │  ai/  Trigger.dev v3 (Node 20)│
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

Data flow: **Copilot prompt → `ai/` agent → (tool calls) → .NET GraphQL → SQL Server → results streamed back.** Trivial lookups short-circuit: rule-based agents answer from a cached snapshot; only genuinely generative turns call the LLM (cost control).

---

## 3. Folders & repos (all inside the GTP tree — isolation preserved)

```
griot/
├── src/            # .NET 8 API — UNCHANGED core (bootcamp)
├── web/            # Vite/React/MUI — UNCHANGED core (bootcamp) + features/copilot/
├── mobile/         # Flutter — UNCHANGED core (bootcamp)
├── ai/             # NEW — Trigger.dev v3 project (Node 20, own .nvmrc, own lockfile)
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
cd ~/gtp/griot/ai
nvm use          # .nvmrc → 20 (pinned for this project)
npx trigger.dev@latest init --project-ref <PROJECT_REF> --api-url https://api.trigger.dev
# generates trigger.config.ts + .trigger/ registry + src/ sample task
echo "ANTHROPIC_API_KEY=..." >> .env   # or OPENAI_API_KEY — both supported
npm install @trigger.dev/sdk @trigger.dev/react-hooks

cd ~/gtp/griot/mcp
npm init -y && npm i @modelcontextprotocol/sdk zod
# optionally: npx @modelcontextprotocol/inspector node src/server.js   (MCP Inspector GUI)
```

> Keys live only in the `ai/` project environment (never in `web/`). Costs stay capped via a per-project daily budget + Redis counters (see §7).

## 5. Trigger.dev: scheduled workflows + on-demand agents

```ts
// ai/src/tasks/reminders.ts — a scheduled workflow (every day 08:00)
import { task } from "@trigger.dev/sdk/v3";

export const dueReminders = task({
  id: "due-reminders",
  schedule: { cron: "0 8 * * *", timezone: "Africa/Nairobi" },
  run: async ({ griotUrl, token }) => {
    // 1. GraphQL: query tasks due within 48h per workspace
    // 2. For each → create Notification via the API (same token)
    // 3. Optional MCP skill call: post Slack digest
  },
});
```

```ts
// ai/src/agents/griotCopilot.ts — the in-app Copilot (conversational + tool-calling)
import { agent, tool } from "@trigger.dev/sdk/v3";
import { z } from "zod";

export const griotCopilot = agent({
  name: "griot-copilot",
  model: { provider: "anthropic", model: "claude-sonnet-4-5", temperature: 0 },
  system: "You are Griot's assistant. Answer ONLY from the tools. Never invent task statuses. Confirm before mutating.",
  tools: [
    tool({ name: "getBoard", description: "…", input: z.object({ boardId: z.string() }),
           handler: async ({ boardId }) => boardApi.getBoard(boardId) }),
    tool({ name: "createTask", description: "…", input: z.object({ title: z.string(), columnId: z.string() }),
           handler: async (input) => taskApi.createTask(input) }),
    // + updateTaskStatus, addComment
  ],
});
```

> Exact import names (e.g. `agent`, `tool`, MCP client helper) do move between Trigger.dev versions — the shape above is **illustrative**; pin to the SDK generated by `trigger.dev init`, which types against the CLI-generated registry. Concept stays: agents get typed tools; tools hit the .NET API; runs are replayable & observable.

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

**Tool roster (v1):** `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project` (calls the LLM agent). **Clients:** Claude Desktop (`claude mcp add`), Cursor (`@Griot /mcp`), VS Code Copilot, Cline, plus Griot's own agents.

## 7. Security & cost guardrails (also the Week-6 OWASP surface)

- **Service-to-service auth**: static long-lived `GRIOT_SERVICE_TOKEN` validated on the .NET side → resolved to a **dedicated "ai-agent" workspace member with restricted role** (`CanReadWorkspace`, `CanCreateTask`, `CanComment`, `CanNotify` — no deletes, no invites).
- **Prompt-injection mitigation**: user text is treated as **data, not instructions**; tools apply their own project/workspace scoping; the agent system prompt bans tool-call modification of unrelated entities.
- **Mutation confirmation**: the agent returns a *proposed* action; the Copilot UI renders it ("Create task *…* in column *…*?") for the human to approve. Deterministic paths (digest/reminders) skip this.
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
- [ ] Web Copilot panel streaming responses + approving mutations before write
- [ ] `mcp/` server runnable in stdio + Streamable HTTP; `get_board`/`create_task` verified in Claude Desktop (or Cursor)
- [ ] External skill: Slack digest path working (or GitHub PR linking)
- [ ] OWASP review logged for the AI principal + tool-call surface
- [ ] Cost cap + token budget enforced and observed
- [ ] Tests: golden transcripts, MCP tool tests, Cypress stub — all green in CI

---
**Griot Copilot — the team's history, on demand.**