# Feature 09 — Notifications & Activity Feed UI

## Type

NEW FEATURE

## What This Delivers

`NotificationBell` in the top bar (unread badge + popover + mark-all-read) and the dashboard `ActivityFeed` — both driven by backend notifications/activity endpoints, with optimistic read-state.

## Dependencies

- Web feature 07 (top bar + dashboard).
- Backend features 04–06 (routes for notifications and activity endpoints).

## Context To Read First

- `project-kit/context/ui-registry.md` (NotificationBell, NotificationItem, ActivityFeedItem)
- `state-and-data.md`

## Files Owned

- `web/src/features/notifications/**` (bell + page + hooks)
- `web/src/features/dashboard/ActivityFeed.tsx`

## Implementation Notes

- Bell: badge with unread count (refetch on focus), popover list, per-item read toggle, mark-all-read.
- ActivityFeed: relative timestamps (`ActivityLogs`), actor→action→entity copy.
- Both use TanStack REST hooks; cache invalidation on mark-read.

## Separation of Concerns

- Notifications UI in its feature folder; activity feed inside dashboard; data via REST hooks.

## Docker & Deploy

- No change.

## Out of Scope

Push notifications, email digests (backend/AI scheduling).

## Acceptance Criteria

- [ ] Bell shows correct unread count; read/mark-all persists
- [ ] Activity feed renders recent actions with relative times


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Org-scoped feed: `Notifications` gain `OrganizationId`; the bell/popover and activity feed query only the active org, and switching orgs (web 05 `select-organization`) invalidates and refetches the feed — no cross-org unread leakage in badge counts (server partitions by `org` claim, backend 19/22 pattern).
- Client feedback notifications: `ClientFeedback` events (New/Acknowledged/Resolved/Rejected, backend 34) route to the assigned ProjectManager as notification items with feedback kind + project context; deep-links open the client portal thread (web 15).
- Handoff/maintenance notifications (backend 35): handoff-awaiting-acceptance and maintenance-request items appear in the same feed for Admin/PM; clients see only their own portal-scoped notifications.
- Notification fan-out jobs carry the tenant id server-side (`org` claim → `OrganizationId`); the web simply renders whatever the org-scoped feed returns.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
