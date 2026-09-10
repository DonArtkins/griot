# Monitoring & Observability — Griot

> Minimal viable monitoring for the capstone: health, uptime, errors, logs. Uses the pre-designed `ApiLogs`/`ErrorLogs`/`AuditLogs` tables + Railway/Vercel native tools.

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