# Feature 05 — Secure Authentication & State Management

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Secure authentication & state management"**: the full auth flow (register/login/logout/silent refresh) against the backend, access token in memory (Zustand), refresh token httpOnly cookie, and `RequireAuth` route guards; Zustand holds only client state.

## Dependencies

- Backend feature 07 (auth endpoints).
- Web features 03 (REST hooks) + 04 (Apollo guard).

## Context To Read First

- `web/project-kit/context/{state-and-data,api-integration}.md`

## Agent Skills To Use

- `web/.agents/skills/auth-and-zustand/SKILL.md`

## Files Owned

- `web/src/stores/authStore.ts`
- `web/src/lib/auth.ts` (register/login/logout/refreshSession)
- `web/src/routes/RequireAuth.tsx`, `public.tsx`, `protected.tsx`

## Implementation Notes

- Access token in memory only; refresh in `httpOnly; Secure; SameSite=Lax` cookie (set by the backend).
- Silent refresh on boot + 401-once-retry. Refresh failure → clear + `/login`.
- Zustand: `accessToken`, `user`, plus UI state. NEVER server entities.

## Separation of Concerns

- Auth transport in `lib/`; session state in `stores/`; guard in routes. Components call `useAuthStore`.

## Docker & Deploy

- No change; cookie flags require HTTPS in prod (Vercel provides it).

## Out of Scope

SSO, MFA (v2).

## Acceptance Criteria

- [ ] Auth flow end-to-end vs the backend; silent refresh works; session cleared on refresh failure
- [ ] No `localStorage` tokens anywhere
- [ ] `RequireAuth` redirects unauthenticated users


## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

The current REST transport uses JSON refresh tokens for Postman/mobile. Web
HttpOnly cookie transport in the design remains a backend prerequisite for web
Feature 05; do not treat the cookie diagrams as live behavior or store tokens in
localStorage. SQL Server owns refresh rows; Redis currently owns login limits.
## Multi-Tenant Update (2026-09-11 — PLANNED)

- JWT v2 claims handling: on boot/refresh, decode the access token's added claims — `name` (DisplayName), `org` (active OrganizationId; absent for platform-only sessions), `role` (`super_admin`, the organization role, or `custom:{roleId}`), `perms` (space-separated permission keys) — into `authStore` client state (memory only, never `localStorage`).
- Org switching UX: `POST /api/auth/select-organization` re-issues the whole token pair (access + opaque refresh rotation); on success update the store, reset the Apollo cache (web 04) and invalidate org-scoped TanStack queries. A multi-org user without an active org lands on the org-selection screen; a SuperAdmin platform session (no `org`) lands on the platform console.
- Opaque refresh note (unchanged, by design): the refresh token stays a rotating opaque 64-hex token — never a JWT; jwt.io showing it blank is correct behavior and the web must never attempt to decode it.
- Role-based route guards: `RequireAuth` is joined by `RequirePerm`/`RequireRole` guards reading `role`/`perms` — company admin console (web 13) gated on `org.members.manage`/`org.roles.manage`, platform console (web 14) on `role == super_admin`, client portal (web 15) on the `Client` org role, handoff/maintenance (web 16) on PM/Admin perms.
- Visibility mirrors, never enforces: Owner/Admin see ALL projects inside their company only, PM only assigned projects, Member only their memberships, Client only attached projects — the server is the boundary (backend 29 query filters).
- Suspended-org handling: `403 org_suspended` surfaces a read-only banner and disables write affordances client-side while the server enforces.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
