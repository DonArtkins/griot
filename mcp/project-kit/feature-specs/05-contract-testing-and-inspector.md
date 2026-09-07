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


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
