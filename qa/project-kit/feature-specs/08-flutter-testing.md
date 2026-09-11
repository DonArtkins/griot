# Feature 08 - Flutter Widget & Integration Tests

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 5 deliverable for mobile: Flutter widget tests + `integration_test` flows run against the Android target.

## Dependencies

- Mobile features 02-06.

## Context To Read First

- `research/week-06-quality-engineering-foundations.md` sec on Flutter

## Agent Skills To Use

- `qa/.agents/skills/flutter-testing/SKILL.md`

## Files Owned

- `mobile/test/**`, `mobile/integration_test/**`

## Files

CREATE: widget tests (login screen, status picker, board/task screens with mocked GraphQL/dio); integration test for the auth -> dashboard -> board flow.

## Implementation Notes

- CI runs `flutter test` in the Docker-pinned Flutter image; integration_test on emulator locally + nightly.

## Acceptance Criteria

- [ ] `flutter test` + integration_test green


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Org switching tests (mobile 08): `select-organization` re-issues the token pair — atomic memory + secure-storage replacement, provider invalidation, per-org refetch.
- Role-aware navigation tests: dashboard shell vs client-portal shell rendered per the `role` claim; suspended-org read-only banner path.
- Server-authoritative scoping assertions: the app never filters org/role data locally (providers consume backend-scoped results only).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
