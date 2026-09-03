---
name: jest-rtl
description: "Jest + React Testing Library for the Griot web app: component tests for TaskCard/BoardView/modals, mocked Apollo/React Query hooks, MSW for the copilot stub."
metadata:
  version: "0.1.0"
---

# Jest + RTL Skill

## Conventions

- `setupTests.ts` registers jest-dom; `npm test` = single non-watch run.
- Assert behavior, not implementation; minimal snapshots.
- Mock Apollo `useQuery`/`useMutation` and TanStack hooks at the hook boundary; MSW for the copilot.

## Must-cover

- TaskCard renders status/priority per the severity map.
- Drag-drop reorder optimistic + rollback path.
- Auth: redirect when unauthenticated; silent refresh failure -> logout.

## Rules

- Interaction tests where risk is high; empty/loading/error states asserted where async.
