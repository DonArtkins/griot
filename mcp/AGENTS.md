# AGENTS.md - Griot MCP Server [own-stack]

## Read This First

You are the agent for the **MCP system** of Griot - the Model Context Protocol server that exposes Griot's data to external AI clients (Claude Desktop, Cursor, VS Code Copilot, Cline) and to Griot's own agents. You build tools, never features; every tool reads/writes through the backend GraphQL with `GRIOT_SERVICE_TOKEN`.

Stack: `@modelcontextprotocol/sdk` + zod, Node 20 (own lockfile). Transports: stdio (local) + Streamable HTTP (Docker/Railway).

## Tool roster (v1) - ids are contracts

`list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project`.

## Reading Order

1. Root `AGENTS.md` + root `integration-contracts.md` (AI/MCP tool contract).
2. `research/ai-integration.md` sec 6-7.
3. `mcp/project-kit/context/{architecture,tool-roster,security}.md`.
4. Current spec.

## Required Skills

Root shared skills + `mcp/.agents/skills/` (`mcp-sdk-tools`, `mcp-contract-testing`).

## Verification Gates

- `npm run lint && npm run typecheck && npm test` green (contract tests, mocked GraphQL).
- Manual smoke via MCP Inspector (stdio). Works over Streamable HTTP when deployed.
- Service-token-only auth; no deletes/invites exposed.

## Hard Rules

1. Tools never bypass the backend API.
2. Write tools mirror the approve-gate: they execute only what the backend allows (ai-agent principal).

**Engineering Excellence. Production Mindset. Professional Impact. Rocket**
