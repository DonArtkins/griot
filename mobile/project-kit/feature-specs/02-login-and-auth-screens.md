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

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
