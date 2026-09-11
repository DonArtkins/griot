# Feature 03 — REST Integration (Axios + TanStack Query)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"REST integration (Axios + TanStack Query)"**: the typed Axios client with 401→refresh→retry interceptors, one `QueryClient`, and the first REST hooks (auth, workspaces, projects, tasks, uploads).

## Dependencies

- Web feature 01 (lib skeleton).
- Backend features 04 + 07 (routes + auth).

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

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Org-aware REST: every request carries the JWT v2 access token; the backend resolves the active organization from the `org` claim (`TenantResolutionMiddleware`) — clients never send `OrganizationId` in headers or bodies; the 401→refresh→retry interceptor flow is otherwise unchanged.
- New hooks (backend 29–35 routes): `useOrganizations`, `useOrganizationMembers`, `useCustomRoles`, `useClientProjects` (`ClientProjectViewDto`), `useClientFeedback`, `useProjectHandoff`, `useHandoffDocuments`, `useMaintenanceRequests`, `useOrganizationLifecycle`.
- `useSelectOrganization` mutation: `POST /api/auth/select-organization` re-issues the token pair; on success the auth store updates and all org-scoped TanStack queries invalidate.
- Error mapping extended: `403 org_suspended` (writes rejected while the company is suspended) → org-suspended read-only notice surface; existing 401/403/429 mapping untouched.
- New DTO types: `OrganizationDto`, `OrganizationMemberDto`, `ClientProjectViewDto`, `ClientFeedbackDto`, `ProjectHandoffDto`, `HandoffDocumentDto`, `OrganizationLifecycleEventDto`.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
