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

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Apollo attaches the JWT v2 access token; the backend scopes every GraphQL read to the active organization from the `org` claim — no org argument is passed client-side.
- Org-switch cache hygiene: on `select-organization` success the web performs a full `client.clearStore()` reset so cached entities from the previous company can never leak into the new one; typePolicies/fragments repopulate from the new org's queries.
- Fragments gain `OrganizationId` on tenant-owned types (Workspace, Project, Board, TaskItem, Comment, Notification) once the ERD amendment ships; `ClientProjectViewDto`-shaped selections power the client progress view reads.
- New read hooks for org surfaces (organizations, members, roles, client progress, handoff status) colocate fragments in their feature folders per the existing convention.
- Tests assert the cache reset on org switch (no cross-org stale reads) alongside the existing reorder-safety tests.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
