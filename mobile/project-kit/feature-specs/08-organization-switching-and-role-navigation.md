# Feature 08 — Organization Switching & Role-Aware Navigation

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

The mobile companion to web's org switcher and role-aware shells: a switcher for users who belong to multiple companies (`POST /api/auth/select-organization` re-issues the token pair), navigation that adapts to the effective role claim (SuperAdmin / Admin / ProjectManager / Member / Client / `custom:{roleId}`), a suspended-organization state (`403 org_suspended`), and Admin/PM/Member dashboard-view parity with the web reference UIs.

## Dependencies

- Mobile features 02–06 (auth, dio, GraphQL, Riverpod, responsive shells).
- Backend spec 30 (JWT v2 + `select-organization`), spec 29 (tenancy foundation), spec 32/33 (company lifecycle — suspend/reactivate).
- Web specs 13–16 (org surface reference UIs — parity is feature-complete, not pixel-identical).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§3 roles, §4 JWT v2, §5 lifecycle)
- `mobile/project-kit/context/integration-contracts.md` (Multi-Tenant contract, PLANNED)
- `mobile/project-kit/context/{state-and-data,architecture}.md`

## Agent Skills To Use

- `mobile/.agents/skills/riverpod-state/SKILL.md`
- `mobile/.agents/skills/dio-rest/SKILL.md`
- `.agents/skills/jwt-decode/SKILL.md` (verify claim parsing against real token shapes)

## Files Owned

- `mobile/lib/features/organizations/**` (switcher screen + providers)
- `mobile/lib/features/auth/**` (org-selection step after login)
- `mobile/lib/features/*/providers/` navigation guards (`effectiveRoleProvider` consumers)

## Implementation Notes

- Org switch calls `POST /api/auth/select-organization`; the response re-issues the token pair — atomically replace memory access token + secure-storage refresh, cancel in-flight dio/GraphQL requests, invalidate provider state, and refetch per-org data (notifications, dashboard).
- Role-aware navigation: parse the `role` claim on session start and after every switch — route to the dashboard shell (Admin/PM/Member: org-context header + project lists scoped per role) or the client-portal shell (`Client`).
- Suspended-org state: on `403 org_suspended`, render a read-only banner + switch-org CTA; writes are blocked client-side but the server remains authoritative.
- Admin sees ALL projects inside their company only; PM sees assigned projects; Member sees memberships — all enforced server-side; the app never filters org/role data locally.

## Separation of Concerns

- Switching/transport stays in `core/` + auth providers; feature screens consume tenant providers; no screen computes role logic itself.

## Docker & Deploy

- Local run on emulator against compose backend. CI APK artifact unchanged (infra 05).

## Out of Scope

- Company onboarding/offboarding admin UI (SuperAdmin console is web-only).
- Client-portal screens (feature 09).
- Push notifications (v2).

## Acceptance Criteria

- [ ] A multi-org user can list organizations and switch; the token pair is replaced atomically and per-org data refetches
- [ ] Navigation renders the correct shell per `role` claim (dashboard vs client portal) on login and after switch
- [ ] `403 org_suspended` shows the suspended state with a working switch-org path
- [ ] Admin/PM/Member project lists match server-side scoping (no local filtering); parity with web 13–16 shells
- [ ] Refresh token is never parsed client-side (opaque by design) and stays in secure storage

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.