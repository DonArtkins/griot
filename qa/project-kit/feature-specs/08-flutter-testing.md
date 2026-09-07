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


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
