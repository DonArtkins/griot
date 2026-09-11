# Feature 02 - Tool Roster Implementation

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The full v1 tool roster from `mcp/project-kit/context/tool-roster.md`: all eight tools as pure, zod-validated functions.

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

- `summarize_project` produces a deterministic summary of permitted backend data. Model-generated formal reports use backend 24 through mcp 06; no direct agent/LLM endpoint is callable from MCP.
- Output shapes mirror the backend GraphQL types exactly.

## Separation of Concerns

- Tool logic is testable without the server or network.

## Docker & Deploy

- No change.

## Acceptance Criteria

- [ ] Read tools registered; create_task/add_comment register only when mcp 03 supplies verified human approval provenance. All eight tool schemas contract-tested; zod input validation rejects malformed calls


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
