# Non-Functional Requirements (NFR) — Griot

> The constraints the system must meet — performance, availability, security, operability, scalability, accessibility. Most "launch day crashes" trace back to skipping this file. Every number here has a denominator (see CAPACITY-PLAN.md).

---

## 1. Expected load

- **Users**: ~100–300 DAU at cohort/demo scale; up to ~1,500 concurrent supported by design.
- **Throughput**: peak ~10–30 req/s (cohort), sustained design goal ≥ 100 req/s across web+mobile+mcp.

## 2. Latency budget (per hot path)

| Endpoint | p95 target | Notes |
|---|---|---|
| `GET /api/dashboard/summary` | < 500 ms | proc single-round-trip |
| `GET /graphql { board(id) }` | < 500 ms | DataLoader; no N+1 |
| `POST /api/auth/login` | < 300 ms | Argon2 is intentionally slow; budget hash config |
| `POST /api/auth/refresh` | < 200 ms | rotation + Redis |
| `POST /api/tasks` | < 400 ms | transaction + audit |
| `PATCH /api/tasks/bulk-status` | < 800 ms | TVP proc, atomic |
| Web first paint (LCP) | < 2.5 s | Public shell; Lighthouse gate |
| Web CLS | < 0.1 | layout stability |
| Copilot first token (stream) | < 2 s | Trigger realtime; LLM latency not blocking UI |

## 3. Availability target

- **Target**: **99.5%** (≈ 3.6 h/month downtime) — appropriate for a bootcamp/demo tool. NOT 99.9% (don't over-engineer; stated explicitly).
- **Mitigations**: Railway restart policy on the API; Vercel static is effectively 100% edge; `/health` + uptime ping; documented rollback runbook.

## 4. Data retention & privacy

- **ActivityLogs / ApiLogs / AuditLogs**: retain **90 days** hot; archive/prune monthly (partitioned). Error logs: keep Open/Investigating indefinitely, prune Fixed after 90 days.
- **RefreshTokens**: auto-purge rows with `ExpiresAt < now - 7d`.
- **Personal data**: GDPR-aware — user can export (v2) and delete account; IP masked in `ApiLogs`; no PII in logs/errors.
- Audit log is **append-only** — no edits/deletes (compliance line).

## 5. Security NFRs

- Auth flows must pass the **OWASP review** in Week 6 — see `SECURITY.md`.
- All writes audited (AuditLogs) + activity feed (ActivityLogs).
- Rate limits: login 5/min per IP+email (sliding window, Redis); GraphQL query-cost guard.
- No secrets in code/artifacts; `.env.example` only.
- Dependency audit gates in CI (no critical/high CVEs).

## 6. Operability

- `/health` endpoint (liveness + readiness for Railway).
- Structured logs (request id) — see `MONITORING.md`.
- **Change management**: any error/fix is tested + documented before shipping (CHANGE-MANAGEMENT.md).
- Rollback: one-page runbook (one command per host).

## 7. Accessibility (a11y)

- WCAG AA; 44×44 touch targets; visible focus; axe-clean on both shells; keyboard-navigable board.

## 8. Scalability (headroom)

- Read path scales to 10× via read replica + Redis cache, additively (see CAPACITY-PLAN).
- API is stateless → horizontal scale-ready without code change.

## 9. Mobile

- Offline-tolerant: read from last cache; writes queue on retry (v1: minimal; clear on failure).
- Battery-aware: no always-on socket; refresh-on-focus + pull-to-refresh.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**