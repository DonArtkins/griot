# Monitoring & Observability — Griot

> Minimal viable monitoring for the capstone: health, uptime, errors, logs. Uses the pre-designed `ApiLogs`/`ErrorLogs`/`AuditLogs` tables + Railway/Vercel native tools.

**STATUS (2026-09-11):** §2–§3 are **IMPLEMENTED** by backend spec 20 (`20-observability-logging-pipeline.md`) on `feature/backend/20-observability-logging-pipeline` — middleware → `ApiLogs`, exception handler → `ErrorLogs` (+ RFC 7807), `IAuditService` → `AuditLogs`/`ActivityLogs`, retention proc (SQL-gated test green). §1 health checks are implemented (backend feature 02). §4 alert rows are config targets for infra 07 (Netdata), also pending. The spec-20 durable outbox/webhook-inbox/idempotency store remains PLANNED until Trigger.dev callers ship.

## 1. Health checks

- **API `/health`** — liveness + readiness (checks DB ping + Redis ping). Railway uses it as the deployment health check.
- **Uptime ping** — Better Uptime / UptimeRobot → `https://<api>/health` every 60 s; alert on non-200 (catches cold starts + outages).

## 2. Logging

- Structured logs with `RequestId` (correlate web/mobile/AI/MCP ↔ `ApiLogs`).
- Railway logs streamed to its dashboard; Vercel logs for web.
- Error handling: exceptions → `ErrorLogs` row (type, message, stack, source, fix status) + console/structured log.

## 3. Error tracking + audit

- `ErrorLogs` lifecycle `Open → Investigating → Fixed → Verified`; every fix tested + documented (CHANGE-MANAGEMENT).
- `AuditLogs` append-only before/after snapshots for every state change; the OWASP + privacy trail.
- `ApiLogs` gives per-route latency (`DurationMs`) → feeds the k6 baseline and p95 dashboards.

> **Status (2026-09-11):** the writers for §2–3 are **implemented** (backend spec 20). Owning spec: `backend/project-kit/feature-specs/20-observability-logging-pipeline.md`; runtime guide: `docs/observability/HOW-LOGGING-WORKS.md`; historical gap analysis: `docs/observability/LOGGING-AUDIT-REPORT.md`. DB-level trigger safety net + backup chain: spec 21 (next in its slot). Incident answer matrix + executable query recipes: `docs/observability/LOGGING-AUDIT-REPORT.md` — the queries below now run against real persisted rows.

## 4. Alerts (minimal)

| Alert | Trigger | Channel |
|---|---|---|
| Uptime down | `/health` non-200 for > 1 min | Email |
| 5xx spike | ErrorLogs count > threshold /h | Email |
| p95 board regression | k6 run in CI fails threshold | CI red |
| LLM budget | Redis over budget alarm | Email |

## 5. Dashboards (optional)

- Railway metrics (CPU/mem) · Vercel insights (LCP/CLS) · ad hoc k6 reports in qa.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**