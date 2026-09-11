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


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Notifications are org-scoped: list/mark-read operate within the active organization (the server filters by the token's `org` claim; backend 22 bump keys fan-out per org — `notification:org:{orgId}:…`).
- Client feedback routing: when the user holds the `Client` role, the notifications surface also routes `ClientFeedback` items (`New`/`Acknowledged`/`Resolved`/`Rejected`) for their attached projects into the client-portal thread (backend 34).
- PM users see client-feedback triage items routed to them; AI-assisted triage summaries (ai 13) arrive through backend notifications only — mobile never touches Trigger.dev or MCP.
- Mark-all-read and unread counts are per active organization; switching orgs refetches the notification list for the new org.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
