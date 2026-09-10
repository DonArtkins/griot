# AGENTS.md - Griot MCP Server [own-stack]

## Read This First

You are the agent for the **MCP system** of Griot - the Model Context Protocol server that exposes Griot's data to external AI clients (Claude Desktop, Cursor, VS Code Copilot, Cline) and to Griot's own agents. You build tools, never features; every tool reads/writes through the backend GraphQL with `GRIOT_SERVICE_TOKEN`.

Stack: `@modelcontextprotocol/sdk` + zod, Node 20 (own lockfile). Transports: stdio (local) + Streamable HTTP (Docker/Railway).

## Tool roster (v1) - ids are contracts

`list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project`.

**v2 (PLANNED, mcp 06):** `list_reports`, `get_report`, `generate_report`, `download_report`, `system_audit`, `get_audit_log`, `get_metrics` — need backend 20/24 + `CreateReport` scope + ai 06/07. V1 ids are unchanged until mcp 02 ships.

## Reading Order

1. Root `AGENTS.md` + root `integration-contracts.md` (AI/MCP tool contract).
2. `research/ai-integration.md` sec 6-7.
3. `mcp/project-kit/context/{architecture,tool-roster,security}.md`.
4. Current spec.

## Required Skills

Root shared skills + `mcp/.agents/skills/` (`mcp-sdk-tools`, `mcp-contract-testing`).

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P5 —** after infra (P4), because spec 04 deploys to Railway/Docker (needs infra 02–04/06) and spec 03 needs backend 09's service token. Nothing downstream waits on MCP, so the late slot costs nothing. Own order: **01 → 02 → 03 → 04 → 05 → 06** (01–02 technically unblocked anytime — Node 20 only; 06 = report/audit v2 tools after backend 20/24 + ai 06/07). Entry branch: `feature/mcp/01-mcp-server-setup`. Track state in `mcp/project-kit/context/progress-tracker.md`.

## Verification Gates

- `npm run lint && npm run typecheck && npm test` green (contract tests, mocked GraphQL).
- Manual smoke via MCP Inspector (stdio). Works over Streamable HTTP when deployed.
- Service-token-only auth; no deletes/invites exposed.

## Hard Rules

1. Tools never bypass the backend API. Send Bearer `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of: {real User.Id}` from trusted caller context; never use a synthetic member or a model-supplied identity.
2. Write tools mirror the approve-gate: they execute only what the backend allows (real-user OBO principal — 4 scopes, no deletes/invites).
3. MCP is a data/tool surface, not an orchestration trigger: external MCP clients get data through the backend GraphQL only — they never enqueue Trigger.dev tasks or receive Trigger.dev credentials (orchestration contract: `research/ai-integration.md` §2a).

**Engineering Excellence. Production Mindset. Professional Impact. Rocket**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
