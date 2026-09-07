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


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
