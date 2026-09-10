# Backend Feature Spec 16 — Notifications, Dashboard & Logs

## Goal
Notifications (list/unread-count/read-all), dashboard summary (EF via `DomainService`; Phase-1 optimization swaps to `usp_GetDashboardSummary` + Redis 60s cache), activity feed, error logs (Owner/Admin), audit logs (Owner).

## Depends
- Spec 13 membership; Spec 03 stored procedures

## Routes
- `GET /api/notifications` · `GET /api/notifications/unread-count` · `POST /api/notifications/read-all` · `PATCH /api/notifications/{id}/read`
- `GET /api/dashboard/summary` · `GET /api/workspaces/{id}/activity`
- `GET /api/logs/errors` · `GET /api/logs/audit`

## Acceptance (implemented)
- [x] Notifications scoped to the authenticated user
- [x] Dashboard summary + activity scoped to workspace membership
- [x] Error/audit logs role-gated (Owner/Admin, Owner-only)
- [x] No 501 across notifications/dashboard/logs

## Amendment (spec 20 — observability pipeline, 2026-09-10)

The `GET /api/logs/*` routes are implemented and role-gated, but the 2026-09-10 audit
(`docs/observability/LOGGING-AUDIT-REPORT.md`) proved the underlying tables have
**zero writers** — today they can only return empty arrays, and the activity feed has
no producer either. Backend spec **20** (`20-observability-logging-pipeline.md`) is the
planned owner of the write side: `ApiLoggingMiddleware` → `ApiLogs`, exception handler →
`ErrorLogs` (RFC 7807), `IAuditService` → `AuditLogs` + `ActivityLogs` (which also feeds
this spec's activity endpoint with real rows). This spec's acceptance stays ✅ as
implemented (routes, gates, scoping); spec 20's acceptance extends it with "the
endpoints return persisted rows." Query extensions (`fixStatus`, `actorId`, sorting,
spec-18 caps) belong to specs 18/20.
