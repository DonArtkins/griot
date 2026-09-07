# Feature 03 — REST Integration (Axios + TanStack Query)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"REST integration (Axios + TanStack Query)"**: the typed Axios client with 401→refresh→retry interceptors, one `QueryClient`, and the first REST hooks (auth, workspaces, projects, tasks, uploads).

## Dependencies

- Web feature 01 (lib skeleton).
- Backend features 04 + 08 (routes + auth).

## Context To Read First

- `web/project-kit/context/{state-and-data,api-integration}.md`

## Agent Skills To Use

- `web/.agents/skills/tanstack-rest/SKILL.md`

## Files Owned

- `web/src/lib/apiClient.ts`, `web/src/lib/queryClient.ts`
- `web/src/features/*/hooks/use*Query.ts`, `use*Mutation.ts`

## Implementation Notes

- Axios instance: Bearer from `useAuthStore.getState()`; 401 → refresh once → retry once.
- `QueryClient` staleTime 30s; mutation invalidators per entity.
- Hooks: `useMe`, `useWorkspaces`, `useProjects`, `useBoard`, `useMoveTask`, `useBulkStatus`, `useInvite`, `useUploadAttachment`.

## Separation of Concerns

- `lib/` owns transport; feature hooks own data shaping; components stay ignorant of HTTP.

## Docker & Deploy

- No change.

## Out of Scope

GraphQL (feature 04), auth store UI flows (feature 05).

## Acceptance Criteria

- [ ] Axios interceptors work against the backend; refresh-then-retry verified
- [ ] Feature hooks typed; mutation invalidations correct; tests for the interceptor


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
