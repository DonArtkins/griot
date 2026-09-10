# ADR-004 — Real-Time Observability Pipeline, DB Resilience & API Protection

**Status:** Accepted (planning) · **Date:** 2026-09-10 · **Supersedes:** nothing · **Extends:** ADR-002

## Context

ADR-002 added the observability tables before implementation, assuming "the normal pipeline" would fill them. The 2026-09-10 audit (`docs/observability/LOGGING-AUDIT-REPORT.md`) proved that assumption false: zero writers, a persistence-free exception handler, no triggers, no SQL Server backups, one global rate-limit window, ad-hoc pagination, and no notification producer. The user's hard requirement: every layer's errors/logs/crashes must be recorded so any incident answers what happened, where, why, when, what triggered it, the spillover, how many users, and the mitigation — with DB copies that an attack or crash cannot destroy.

## Decision

1. **Backend spec 20** implements the write pipeline: `ApiLoggingMiddleware` → `ApiLogs`; global handler → `ErrorLogs` + RFC 7807; `IAuditService` → `AuditLogs` (before/after JSON) + `ActivityLogs`; retention proc; failure-isolated (log writes never fail requests). No schema change (tables already exist).
2. **Backend spec 21** adds T-SQL audit triggers on operational tables (lower-fidelity catch-all, `DB.`-prefixed actions), a FULL/DIFF/LOG backup chain on a dedicated volume + opt-in compose sidecar, a rehearsed restore drill, and an evidence-gated scale-out posture (read replica stays Phase-2 per DATABASE-DESIGN.md §5).
3. **Backend specs 18/19** standardize pagination/sorting/search (hard caps, whitelists, `PagedResult<T>`) and add Redis cache-aside + per-route rate-limit partitions + GraphQL cost limits.
4. **Backend spec 22** fans product events out to in-app `Notifications` (always) + Brevo email (per new `NotificationPreferences`), reusing spec-12 sender identities, best-effort.
5. **Order:** backend 09 (unblocks AI/MCP) → **20 → 18 → 19 → 22 → 21** → 11 → 10 close P0.

## Options considered

| Option | Pros | Cons |
|---|---|---|
| Middleware+service pipeline + triggers + backups (chosen) | every layer covered; DB safety net survives app bugs; incident matrix answerable | triggers need discipline (no recursion, best-effort) |
| App-only logging, no triggers | simpler | raw SQL/migrations bypass audit silently — the exact "crash renders system useless" risk |
| External APM only (no tables) | quick | loses per-user/entity traceability; violates ADR-002; cost |
| Log-backup-less nightly full only | simplest | up to 24h data loss; unacceptable per runbook |

## Consequences

- MONITORING.md's alert queries become executable; qa's Newman suite can assert trails; `ai/`'s "audit rows per tool call" gate becomes testable (post spec 09).
- `usp_PruneObservabilityLogs` is mandatory or the tables grow unbounded (90/365/180-day policy).
- Spec 16's "implemented" acceptance is amended by spec 20 (logs endpoints must return *persisted* rows, not just 200s).
- Postman gains folder 14 (tolerant assertions today → strict after specs land).

## References

- `docs/observability/LOGGING-AUDIT-REPORT.md` (audit + query recipes + layer flow)
- ADR-002, `docs/database/DATABASE-DESIGN.md`, `docs/planning/RUNBOOK-ROLLBACK.md`, `docs/planning/CACHING-REFRESH-SYNC-STRATEGY.md`
- Specs: `backend/project-kit/feature-specs/{18,19,20,21,22}-*.md`

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.