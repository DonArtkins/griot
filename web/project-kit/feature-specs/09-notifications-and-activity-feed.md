# Feature 09 — Notifications & Activity Feed UI

## Type

NEW FEATURE

## What This Delivers

`NotificationBell` in the top bar (unread badge + popover + mark-all-read) and the dashboard `ActivityFeed` — both driven by backend notifications/activity endpoints, with optimistic read-state.

## Dependencies

- Web feature 07 (top bar + dashboard).
- Backend features 04 + 07 (routes).

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


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly.
