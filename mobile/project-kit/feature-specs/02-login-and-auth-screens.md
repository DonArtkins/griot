# Feature 02 — Login & Authentication Screens

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Complete login & authentication screens"**: signup/login/logout screens against the backend REST auth endpoints, with the secure token flow (memory access token + `flutter_secure_storage` refresh + silent refresh).

## Dependencies

- Mobile feature 01.
- Backend feature 07 (auth).

## Context To Read First

- `mobile/project-kit/context/{state-and-data,api-integration}.md`

## Agent Skills To Use

- `mobile/.agents/skills/dio-rest/SKILL.md`
- `mobile/.agents/skills/riverpod-state/SKILL.md`

## Files Owned

- `mobile/lib/features/auth/**`
- `mobile/lib/core/storage/secure_storage.dart`

## Implementation Notes

- Login/signup forms (email + password), validation, inline errors.
- Access token → memory (Riverpod authProvider); refresh token → secure storage; silent refresh on boot.
- Logout clears memory + storage.

## Separation of Concerns

- Screens = presentation; authProvider = session state; dio interceptor = refresh transport.

## Docker & Deploy

- Local device run against compose backend.

## Out of Scope

Registration UX beyond a basic form (v2).

## Acceptance Criteria

- [ ] Login/signup work against the backend; logout clears all tokens
- [ ] Silent refresh on boot; 401 → refresh → retry once works


## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Parse JWT v2 access-token claims on login: `name`, `org`, `role`, `perms` (contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §4; owner: backend spec 30). Lifetimes unchanged (access 15 min).
- After login, if the user holds memberships in more than one organization, show the organization-selection screen backed by `POST /api/auth/select-organization` (re-issues the token pair; backend 30).
- Role-based navigation: route to the dashboard shell or client-portal shell based on the effective `role` claim (`super_admin`, `Admin`/`Owner`, `ProjectManager`, `Member`, `Client`, `custom:{roleId}`).
- Refresh tokens stay opaque by design — they are NOT JWTs and are never parsed client-side; they remain in `flutter_secure_storage` exactly as implemented. jwt.io decoding them blank is correct behavior, not a defect.
- The org-selection screen and role routing are PLANNED (2026-09-11 wave) — the implemented status of the existing login/refresh contract is unchanged.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
