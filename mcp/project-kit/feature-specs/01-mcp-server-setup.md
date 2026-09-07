# Feature 01 - MCP Server Setup (stdio + Streamable HTTP)

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The `mcp/` package with a bootable `McpServer` (name `griot`) supporting both stdio and Streamable HTTP transports, and a shell health tool to prove the pipeline.

## Dependencies

- Node 20 per-project. (Backend 09 token wiring comes in feature 03.)

## Context To Read First

- `research/ai-integration.md` sec 6
- `mcp/project-kit/context/architecture.md`

## Agent Skills To Use

- `mcp/.agents/skills/mcp-sdk-tools/SKILL.md`

## Setup / Initialization

```bash
mkdir -p mcp/src mcp/tests && cd mcp
npm init -y
npm i @modelcontextprotocol/sdk zod
```

## Files Owned

- `mcp/**` (scaffold)

## Files

CREATE: `src/server.ts` (McpServer), `src/transports.ts` (stdio + StreamableHTTP), `src/tools/ping.ts` (shell tool for smoke tests), `package.json` scripts (dev/build/test).

## Implementation Notes

- Keep transport bootstrap separate from tool registration (testability).
- `server.tool("ping", ...)` returns `{ ok: true }` as text content.

## Separation of Concerns

- Server/tools/transport separated; no backend coupling yet.

## Docker & Deploy

- Run in stdio locally; Streamable HTTP in containers (feature 04).

## Acceptance Criteria

- [ ] `npm run dev` boots; MCP Inspector smoke passes for stdio
- [ ] Streamable HTTP endpoint responds (health)
