# Feature 07 - Unit & Integration Testing: Jest + RTL

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 5 deliverable for the frontend: Jest + React Testing Library component/interaction tests for the key web surfaces.

## Dependencies

- Web features 05-09 (UI exists).

## Context To Read First

- `qa/project-kit/context/{test-pyramid,code-standards}.md`

## Agent Skills To Use

- `qa/.agents/skills/jest-rtl/SKILL.md`

## Files Owned

- `web/src/**/*.test.{ts,tsx}`

## Files

CREATE: tests for `cn()`, theme tokens, TaskCard, BoardView (reorder + rollback), TaskModal, forms, NotificationBell, RequireAuth, auth interceptor.

## Implementation Notes

- Mock Apollo/TanStack at hook boundaries; MSW for the copilot panel.
- Assert behavior; minimal snapshots.

## Acceptance Criteria

- [ ] `npm test` green; interaction risk covered


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
