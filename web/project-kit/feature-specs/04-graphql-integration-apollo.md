# Feature 04 — GraphQL Integration (Apollo Client)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"GraphQL integration (Apollo Client)"**: the Apollo client pointed at `/graphql`, `InMemoryCache` with `typePolicies`, and GraphQL read hooks for dashboard/boards/task detail with colocated fragments.

## Dependencies

- Web feature 03 (lib conventions in place).
- Backend feature 05 (GraphQL live).

## Context To Read First

- `web/project-kit/context/{state-and-data,api-integration}.md`

## Agent Skills To Use

- `web/.agents/skills/apollo-graphql/SKILL.md`

## Files Owned

- `web/src/lib/apolloClient.ts`
- `web/src/features/{dashboard,board,taskDetail}/**/*.fragment.ts` + hooks

## Implementation Notes

- Headers attach Bearer from the auth store.
- `typePolicies`: `Task.order: { merge: false }`; entity cache keys by `__typename + id`.
- Fragments colocate: `BoardTaskCard.fragment.ts` etc.

## Separation of Concerns

- Apollo owns GraphQL read data; TanStack owns REST; Zustand owns client state. No overlap.

## Docker & Deploy

- No change.

## Out of Scope

Mutations through Apollo (web uses REST for writes per architecture) except where a mutation is cleaner — record the exception in `state-and-data.md`.

## Acceptance Criteria

- [ ] Dashboard/board/task queries resolve from the backend; cache reorder-safe
- [ ] No N+1 in feature reads (uses backend DataLoaders)
