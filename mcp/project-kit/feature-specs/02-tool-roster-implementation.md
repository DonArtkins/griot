# Feature 02 - Tool Roster Implementation

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The full v1 tool roster from `mcp/project-kit/context/tool-roster.md`: all nine tools as pure, zod-validated functions.

## Dependencies

- Feature 01 (server boots).

## Context To Read First

- `mcp/project-kit/context/tool-roster.md`

## Agent Skills To Use

- `mcp/.agents/skills/mcp-sdk-tools/SKILL.md`

## Files Owned

- `mcp/src/tools/**`

## Files

CREATE: `tools/projects.ts`, `boards.ts`, `tasks.ts`, `comments.ts`, `activity.ts`, `summarize.ts` - pure `(graphqlClient, input) -> output` functions registered on the server via zod schemas.

## Implementation Notes

- `summarize_project` calls the ai agent/LLM endpoint (or the backend summarize) - keep as a separate dependency to avoid blocking tool tests.
- Output shapes mirror the backend GraphQL types exactly.

## Separation of Concerns

- Tool logic is testable without the server or network.

## Docker & Deploy

- No change.

## Acceptance Criteria

- [ ] All nine tools registered; zod input validation rejects malformed calls


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
