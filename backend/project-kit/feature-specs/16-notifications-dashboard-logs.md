# Backend Feature Spec 16 — Notifications, Dashboard & Logs

## Goal
Notifications (list/unread-count/read-all), dashboard summary (one-round-trip via
`usp_GetDashboardSummary`), activity feed, error logs (Owner/Admin), audit logs (Owner).

## Depends
- Spec 13 membership; Spec 03 stored procedures

## Routes
- `GET /api/notifications` · `GET /api/notifications/unread-count` · `POST /api/notifications/read-all`
- `GET /api/dashboard/summary` · `GET /api/workspaces/{id}/activity`
- `GET /api/logs/errors` · `GET /api/logs/audit`

## Acceptance (implemented)
- [x] Notifications scoped to the authenticated user
- [x] Dashboard summary + activity scoped to workspace membership
- [x] Error/audit logs role-gated (Owner/Admin, Owner-only)
- [x] No 501 across notifications/dashboard/logs
