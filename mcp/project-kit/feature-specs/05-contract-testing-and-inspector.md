# Feature 05 - Contract Testing + MCP Inspector Smoke

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The deterministic test suite: each tool's JSON contract asserted with a mocked GraphQL client (no network in CI), plus the manual MCP Inspector smoke pass.

## Dependencies

- Features 01-03 (tools + client).

## Context To Read First

- `mcp/project-kit/context/tool-roster.md`
- `mcp/.agents/skills/mcp-contract-testing/SKILL.md`

## Files Owned

- `mcp/tests/**`, CI `test-mcp` job (with infra)

## Files

CREATE: per-tool contract tests (zod schemas + mocked graphql client + expected output JSON); a schema-parity check against root `integration-contracts.md` tool table.

## Implementation Notes

- Contract = ground truth for external clients; changes trigger the contract-sync gate.
- Manual smoke: `npx @modelcontextprotocol/inspector node src/server.ts`.

## Separation of Concerns

- Tests constrain the public contract without needing the backend/binaries.

## Docker & Deploy

- CI job gated in infra feature 05.

## Acceptance Criteria

- [ ] `npm test` green (no network); schemas match the integration contract
- [ ] Inspector smoke passes; verified in at least one external client (Claude Desktop or Cursor)


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Cross-tenant negative tool tests: an org A principal calling any tool with org B ids (workspaces, projects, reports, feedback) must get empty results or a clean MCP authorization error — never org B rows, counts or metadata (no count leakage).
- Suspended-org fixtures: writes rejected with `403 org_suspended` (backend 32); reads stay scoped.
- Role-tier matrix per backend 25: Client-tier sessions see portal-scope results only; raw-log tier denied for non-SuperAdmin/Dev principals.
- Manifest-per-role assertion: the tool allow-list served to a session matches the backend 25 capability manifest for that principal's tier (mcp 07 v3 roster).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
