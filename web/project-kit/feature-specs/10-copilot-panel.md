# Feature 10 — Copilot Panel (Streaming + Approve-Before-Write)

## Type

NEW FEATURE (`[own-stack]` — the AI boundary in `research/ai-integration.md`)

## What This Delivers

The in-app Copilot: a collapsible right-rail chat panel in the App shell that streams answers from the `ai/` agent via Trigger realtime and renders **mutation approval cards** — the user approves, then the app performs the write through REST.

## Dependencies

- Web features 05 + 07 (auth + app shell).
- ai feature 02 (agent + realtime streaming exposed).

## Context To Read First

- `research/ai-integration.md` (Copilot UX, security)
- `ui-rules.md` (propose-before-write rule)

## Agent Skills To Use

- `ai/.agents/skills/trigger-dev-tasks/SKILL.md` (streaming + hooks contract)

## Files Owned

- `web/src/features/copilot/**` (panel, thread, approval cards)

## Implementation Notes

- `RealtimeProvider` with a Trigger access token; `useRealtimeStream` for parts.
- Streaming must never block board interactivity — render on stream only.
- Approval cards: "Create task …?" → on Approve the app calls `POST /tasks` (REST) itself.
- MSW-stubbed copilot for tests (no LLM in CI).

## Separation of Concerns

- Panel = web concern; agent logic = ai system; mutations always ride the normal REST path (backend owns writes).

## Docker & Deploy

- Realtime requires the deployed ai system + Vercel; no new infrastructure from web side.

## Out of Scope

Agent scheduling, tool definitions (ai/mcp systems).

## Acceptance Criteria

- [ ] Panel streams Copilot answers from the ai agent
- [ ] Proposed mutations require approval; approved mutations write via REST and update caches
- [ ] Stubbed copilot E2E test green in CI
