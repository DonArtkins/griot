# Feature 04 - Propose-Before-Write Workflow

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The approval gate: the Copilot proposes mutations (`create_task`, `update_task_status`, `add_comment`) as structured proposals; the web panel renders approval cards; the APP executes the mutation through the normal REST path.

## Dependencies

- Feature 02. Web feature 10 (approval card UI).

## Context To Read First

- `ai/project-kit/context/security-guardrails.md`

## Files Owned

- `ai/tools/write.ts` (proposal tools), shared proposal type in `ai/lib/proposals.ts`

## Files

CREATE: proposal schema (type, payload, targetRef); tools return proposals (never execute); expiry/conflict handling (re-validate before the app writes).

## Implementation Notes

- The agent NEVER performs the write - the app does. Deterministic writes (digest/reminders) skip approval by design.
- Window: proposal valid N minutes; stale proposals rejected by the UI.

## Separation of Concerns

- Autority stays with the backend + human; the agent only suggests.

## Docker & Deploy

- No change.

## Acceptance Criteria

- [ ] Proposed mutations appear as approval cards and write only after approval
- [ ] Stale/duplicate proposals handled
