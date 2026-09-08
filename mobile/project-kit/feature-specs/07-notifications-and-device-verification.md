# Feature 07 — Notifications & Device Verification

## Type

NEW FEATURE

## What This Delivers

Notification list + read/unread on mobile and the Week-4 close-out: full manual verification on the Android emulator and one physical device, and the CI APK artifact.

## Dependencies

- Mobile features 02–06 complete.
- Backend notifications route (feature 04).

## Context To Read First

- `mobile/project-kit/context/api-integration.md`
- `research/week-04-mobile-development.md` §5–6

## Files Owned

- `mobile/lib/features/notifications/**`

## Implementation Notes

- Notifications screen: list, unread indicator, mark-read, mark-all-read.
- Refresh on focus; pull-to-refresh.
- Verification checklist: install + run on emulator and physical device; log `flutter doctor` output.

## Separation of Concerns

- Notifications UI in its feature; data via REST hooks.

## Docker & Deploy

- CI builds release APK/AAB (Docker-pinned Flutter image) and uploads the artifact (infra/qa).

## Out of Scope

Push notifications (v2).

## Acceptance Criteria

- [ ] Notifications render + read/unread persists
- [ ] Verified on emulator + physical device; `flutter doctor` Android green
- [ ] APK artifact produced in CI


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
