# Feature 02 - Copilot Agent (Streaming Answers)

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The `griotCopilot` agent that answers board/task questions with streamed responses to the web Copilot panel via Trigger realtime.

## Dependencies

- Feature 01. Backend feature 05 (GraphQL) + 09 (service token). Web feature 10 (panel surface).
- **Build-order note (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md` P2):** the "web feature 10" reference is a *contract-design* dependency — the Copilot panel is the consumer of this stream, and its data shape is agreed here. Implementation order is **ai 01 → ai 02 → web 10** (then ai 04 wraps the panel's approval cards). Do not block ai 02 on web 10 existing.

## Context To Read First

- `ai/project-kit/context/{architecture,roster}.md`

## Agent Skills To Use

- `ai/.agents/skills/trigger-dev-tasks/SKILL.md`

## Files Owned

- `ai/agents.ts` (griotCopilot), `ai/tools/board.ts`, `ai/lib/graphql.ts` (real client)

## Files

CREATE: agent definition; tools `get_board`, `get_task`, `list_boards`, `list_projects`, `get_activity_feed`, `summarize_project` (GraphQL-backed, service-token); streaming wiring contract consumed by web.

## Implementation Notes

- Tool I/O has Zod schemas; reads only in this feature (write proposals land in feature 04).
- Deterministic short-circuit for trivial lookups (cached snapshot) to cut cost.
- Orchestration boundary (research/ai-integration.md §2a): the agent run is enqueued by the .NET backend (server-to-server trigger), results/stream ride the realtime channel to the web panel, and any persisted output is written back via the .NET API — never via SQL, never via a Trigger-owned store.

## Separation of Concerns

- Reads via service token only; no user-identity impersonation.

## Docker & Deploy

- Trigger cloud; streams work against Vercel via the trigger public URL.

## Acceptance Criteria

- [ ] Copilot answers from real board data and streams to the web panel
- [ ] Every read tool call audited


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
