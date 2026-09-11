# Feature 04 — GraphQL Endpoint Integration

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"GraphQL endpoint integration"**: `graphql_flutter` pointed at the same HotChocolate endpoint as web — dashboard, boards, and task detail reads with explicit refetch discipline.

## Dependencies

- Mobile features 01 + 02.
- Backend feature 05 (GraphQL).

## Context To Read First

- `mobile/project-kit/context/{api-integration,state-and-data}.md`

## Agent Skills To Use

- `mobile/.agents/skills/graphql-flutter/SKILL.md`

## Files Owned

- `mobile/lib/core/network/graphql_client.dart`
- `mobile/lib/features/{dashboard,boards,tasks}/**` GraphQL queries + models

## Implementation Notes

- Wrap with `GraphQLProvider`; link middleware attaches Bearer.
- After mutations → explicit refetch (no Apollo-equivalent cache assumptions).
- Queries match the backend GraphQL surface (`me`, `board(id)`, `tasks`, `dashboardSummary`).

## Separation of Concerns

- Reads via GraphQL; writes via REST (per architecture, same as web).

## Docker & Deploy

- No change.

## Out of Scope

Mutations through GraphQL (use REST).

## Acceptance Criteria

- [ ] Dashboard + boards load via GraphQL from the backend
- [ ] Task detail + comments load; refetch after any local write


## Multi-Tenant Update (2026-09-11 — PLANNED)

- GraphQL requests carry no tenant parameter: org context is derived server-side from the access token's `org` claim (HotChocolate tenant middleware, backend 29) — the mobile client sends only the Bearer token.
- After an organization switch, rebuild the GraphQL client/link so the new token pair is used and previously fetched reads are invalidated (explicit-refetch discipline already forbids stale cache assumptions).
- Server-side cross-tenant isolation (EF Core global query filters, backend 29) means the client never filters by org manually — queries (`me`, `board(id)`, `tasks`, `dashboardSummary`) return only active-organization data.
- A `Client`-role session gets client-portal-shaped reads only (progress view fields); board internals are simply absent from the authorized GraphQL surface (backend 34).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
