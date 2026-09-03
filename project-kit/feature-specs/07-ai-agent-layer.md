# Feature 07 — AI Layer: Copilot, Agents & MCP (Trigger.dev v3)

## Type

NEW FEATURE (`[own-stack]` extension)

## What This Delivers

The AI differentiator that wraps the bootcamp stack without replacing it: the in-app **Copilot** panel (streaming chat + propose-before-write mutations), **scheduled agents** (due-date reminders, weekly digest, stale-board detection, Monday standup), and the **Griot MCP server** exposing board/task tools to external AI clients. Everything reads/writes *only* through the existing .NET GraphQL API with `GRIOT_SERVICE_TOKEN`.

## Dependencies

- Feature 04 (GraphQL + `ai-agent` principal + `/api/webhooks/trigger`).
- Feature 05 (Copilot panel surface exists, feature-flagged).
- Node 20 per-repo; Trigger.dev project; `GRIOT_SERVICE_TOKEN` + LLM key for dev.

## Context To Read First

- `context/architecture-context.md` (AI Boundary)
- `context/library-docs.md` (AI Layer)
- `research/ai-integration.md`

## Files Owned

- `ai/**` (own npm package, own lockfile)
- `mcp/**` (own npm package, own lockfile)
- `web/src/features/copilot/**` (panel activation)

## Files

CREATE: `ai/` Trigger.dev v3 project — `griotCopilot` agent, scheduled tasks `dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`; tools calling GraphQL with the service token; token-budget/cost-cap guard using Redis.
CREATE: `mcp/` MCP server — tools `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project`; stdio + Streamable HTTP transports.
CREATE: Golden-transcript tests (`ai/`) with a mocked LLM client; MCP tool contract tests (`mcp/`).
MODIFY: `web` — Copilot panel wires `@trigger.dev/react-hooks` `RealtimeProvider`/streaming; approval cards call the API directly for writes.

## Setup / Initialization

```bash
cd ai && nvm use
npx trigger.dev@latest login && npx trigger.dev@latest init --project-ref <PROJECT_REF>
npm i @trigger.dev/sdk @trigger.dev/react-hooks
echo "GRIOT_API_URL=…" >> .env && echo "GRIOT_SERVICE_TOKEN=…" >> .env
echo "ANTHROPIC_API_KEY=…" >> .env        # LLM keys live ONLY here, never in web/

cd ../mcp && nvm use && npm init -y
npm i @modelcontextprotocol/sdk zod
npx @modelcontextprotocol/inspector node src/server.ts   # GUI smoke test
```

## Separation of Concerns

- `ai/` = orchestration (agents, schedules, tool calls) — **no DB access, no API routes.**
- `mcp/` = exposure (tools as pure `(graphqlClient, input) → output` functions) — no scheduling, no UI.
- `web/` copilot = presentation of streams + human approval gate — the agent never executes a mutation itself.
- The .NET API stays the single owner of data; the `ai-agent` principal enforces a reduced role (no deletes, no invites, workspace-scoped).
- Audit contract: every tool call logs `workspaceId`, `tool`, `payloadHash`, `runId` (tracable across Trigger ↔ MCP ↔ ActivityLog).

## Docker & Deploy

- **ai/**: Trigger.dev cloud (canonical) or self-hosted on Railway — scheduled + agent runs are durable/observable from the Trigger dashboard. Env: `GRIOT_SERVICE_TOKEN`, `ANTHROPIC_API_KEY`/`OPENAI_API_KEY`, `GRIOT_API_URL`.
- **mcp/**: Docker image running **Streamable HTTP** mode on Railway (a `mcp` service beside `api` in compose for local parity); stdio mode stays for local Claude/Cursor/Cline.
- Both Node projects get the same CI gate (`npm run lint && npm run typecheck && npm test`) — no LLM in CI (mocked clients).

## Out of Scope

- AI writes outside the approved tool roster, agent-initiated deletes/invites, on-device models, multi-provider key rotation (v2).

## Acceptance Criteria

- [ ] Copilot streams answers from board data; approval cards create tasks via the API after user confirm
- [ ] Scheduled agents run in dev on schedule (digest/reminders/stale-board)
- [ ] MCP server works in stdio + Streamable HTTP; `get_board`/`create_task` verified in an MCP client
- [ ] Golden transcripts + MCP contract tests + stubbed-copilot Cypress green in CI
- [ ] Cost cap enforced; audit log contract recorded
- [ ] OWASP review logged for the AI principal surface

## Future Modifications

- Feature 08/09 deploy `ai/` + `mcp/`; Feature 10 runs the Week-7 end-to-end AI validation.