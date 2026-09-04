# Non-Functional Requirements (NFR) — Griot

> The constraints the system must meet — performance, availability, security, operability, scalability, accessibility. Most "launch day crashes" trace back to skipping this file. Every number here has a denominator (see CAPACITY-PLAN.md).

---

## 1. Expected load

- **Users**: ~100–300 DAU at cohort/demo scale; **target**: up to ~1,500 concurrent by design (unverified until reproducible k6 load evidence exists — see CAPACITY-PLAN.md).
- **Throughput**: peak ~10–30 req/s (cohort), **target** sustained design goal ≥ 100 req/s across web+mobile+mcp (to be validated by k6 baseline in QE week).

## 2. Latency budget (per hot path)

| Endpoint | p95 target (baseline) | Phase 1 optimization | Phase 2 optimization | Notes |
|---|---|---|---|
| `GET /api/dashboard/summary` | < 500 ms | **< 100 ms** (Redis 60s cache) | — | proc single-round-trip → cached |
| `GET /graphql { board(id) }` | < 500 ms | **< 200 ms** (DataLoader) | **< 100 ms** (+ Redis cache + indexes) | DataLoader prevents N+1 (101 queries → 3) |
| `POST /api/auth/login` | < 300 ms | — | — | Argon2 is intentionally slow; budget hash config |
| `POST /api/auth/refresh` | < 200 ms | — | — | rotation + Redis |
| `POST /api/tasks` | < 400 ms | — | — | transaction + audit |
| `PATCH /api/tasks/bulk-status` | < 800 ms | — | — | TVP proc, atomic |
| `POST /api/tasks/{id}/attachments` | < 2 s | **< 1.5 s** (Vercel Blob) | — | 25 MB limit, Vercel CDN delivery |
| Web first paint (LCP) | < 2.5 s | — | **< 2 s** (code splitting) | Public shell; Lighthouse gate |
| Web CLS | < 0.1 | — | — | layout stability |
| Copilot first token (stream) | < 2 s | — | — | Trigger realtime; LLM latency not blocking UI |

**Impact summary:**
- **Phase 1 (production blockers):** Dashboard 80% faster (400ms → 100ms), board reads 33% faster (300ms → 200ms), attachments now production-ready.
- **Phase 2 (conditional):** Board reads 50% faster again (200ms → 100ms) via Redis response caching + covering indexes.

See `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` for full optimization roadmap and measurement strategy.

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
- Rate limits: login 5/min per IP+email (sliding window, Redis); GraphQL query-cost guard + depth limit + timeouts.
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

- **v1 Behavior (online-first):** Mobile app requires network connectivity for task creation/updates. If offline → show "No connection" error, block write actions, display cached read-only data (Apollo/GraphQL cache). No writes queued; user must retry when online.

- **Phase 3 Enhancement (post-bootcamp, deferred):** Offline-tolerant write queue
  - **Persistence**: queued writes persist (sqflite) until operation succeeds or user explicitly discards — writes never silently cleared
  - **Idempotency**: each queued write carries client-generated `idempotencyKey` (UUID v4); backend deduplicates so replayed requests are safe
  - **Conflict handling**: if server returns conflict (task moved by another user while offline), client surfaces resolution prompt — user chooses to keep their version, accept server version, or discard
  - **See:** `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` Phase 3 for implementation timeline (~6-10 days)

- **Battery-aware (v1):** no always-on socket; refresh-on-focus + pull-to-refresh

**Rationale for deferral:** v1 scope focuses on stable online experience. Offline-write queue requires mature conflict resolution UX + backend idempotency infrastructure (6-10 days implementation). Most project-management tools (Asana, Monday, Linear) also fail gracefully offline. Post-launch usage data will inform Phase 3 priority.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**