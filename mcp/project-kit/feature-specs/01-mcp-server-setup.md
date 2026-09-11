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


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Tenant context comes from the delegated OBO principal: the JWT v2 `org` claim resolved server-side by the backend (specs 29/30) — never from a client header, tool argument or model output; it is per-transport session state (stdio bound profile / Streamable HTTP session), extending the existing trusted-identity rule.
- Server bootstrap gains a tenant-context provider handed to tools (`(ctx, input) → output`); org-scoped tools fail closed when no org resolves (platform-level tools unaffected).
- Health/transport behavior, env and container contract unchanged — tenancy adds no transport, port or secret.
- All PLANNED (2026-09-11 wave): ships only after backend 29/30; the implemented status of this spec is unchanged until then.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
