# Logging & Audit System Report — Griot

**Date:** 2026-09-10 · **Scope:** all seven systems · **Companion specs:** backend 20 (pipeline), 21 (DB triggers/backups), 18/19 (query + protection), 22 (fan-out) · **Decision:** ADR-004

## 1. Executive verdict

The observability **schema is designed-in but the pipeline was never implemented**. `ApiLogs`, `ErrorLogs`, `AuditLogs` (ADR-002, 16-table ERD) and `ActivityLogs` exist as entities, `DbSet`s, and indexes — and are read by real endpoints — but **nothing in the codebase writes a single row to any of them**. Every crash, error, and mutation today is invisible to the database.

## 2. Verified findings (source-level evidence)

| # | Finding | Evidence |
|---|---|---|
| F1 | **Zero log writes.** Repo-wide scan for `new ApiLog`, `Set<ApiLog>`, `Set<ErrorLog>`, `Set<AuditLog>`, `Set<ActivityLog>` (+`Add`/`AddRange` variants) → **no matches** outside entities/DbContext/migrations. | `backend/src` write-path scan 2026-09-10 |
| F2 | **30+ `SaveChangesAsync` call sites** (DomainService ×29, AuthService, AuthRepository ×6 incl. `ExecuteUpdateAsync`, GriotMutation ×11) — every one a missed audit hook. | write-path inventory |
| F3 | **Global exception handler persists nothing** — returns `text/plain` 500 with `X-Request-Id`; no `ErrorLogs` row, not RFC 7807. | `backend/src/Griot.Api/Program.cs:211-230` |
| F4 | **Activity feed is permanently empty** — `GetActivityAsync` reads `ActivityLogs`; no writer exists. | `DomainService.cs:725` |
| F5 | **No database triggers** — repo-wide `CREATE TRIGGER` search → 0 matches. DB-level audit safety net absent. | repo scan |
| F6 | **No automated backup of SQL Server** — only the Railway *PostgreSQL* PITR runbook exists; the primary store has no FULL/DIFF/LOG chain and no restore drill. | repo scan + RUNBOOK-ROLLBACK |
| F7 | **Rate limiting is one global fixed window** (100/min, IP-or-user) — no auth-route partition beyond the spec-12 Redis OTP gate, no webhook/GraphQL limits. | `Program.cs:183-197` |
| F8 | **Pagination/sorting ad-hoc** — fixed page size, no sort contract, no `totalCount` envelope, no caps per route. Search/filtering absent. | `api-surface.md` §Conventions |
| F9 | **Caching designed, not implemented** — Redis dashboard-cache documented (architecture.md Phase 1, CACHING-REFRESH-SYNC-STRATEGY.md) with no code. | specs + source |
| F10 | **Notifications are read-only** — in-app routes exist; no producer; no email fan-out despite spec-12 email infra. | `NotificationService` + controllers |
| F11 | Log **reads** are role-gated and implemented (`api/logs/errors` Owner/Admin, `api/logs/audit` Owner) — the consumption side is ready and waiting for data. | `DashboardController.cs:28-35` |

## 3. Flow & order of implementation between layers

The observability chain must be built **bottom-up, then inward-out**:

```
1. backend 20  → writes land in SQL Server (ApiLogs/ErrorLogs/AuditLogs/ActivityLogs)   [data plane]
2. backend 21  → DB triggers + backup chain (safety net under 1)                        [resilience]
3. backend 18  → capped, sorted, searchable queries over everything                     [read plane]
4. backend 19  → caching + rate limits protect 1-3                                      [protection]
5. backend 22  → events fan out in-app + email (consumes 20's event hooks)              [UX plane]
6. infra 07    → Netdata + alerts read the same tables (MONITORING.md becomes true)     [ops]
7. qa 04/05/10 → Postman/Newman assert trails; k6 proves the p95 numbers ApiLogs record [verification]
8. web 09 / mobile 07 → bell + feed + prefs UI surface 22's data                        [presentation]
```

Backend 20 comes **first** because every later layer (ops dashboards, QA assertions, AI audit gates, incident runbooks) reads the tables it fills. Backend 09 (AI gateway) stays ahead of the queue independently — it unblocks `ai/` + `mcp/` and is orthogonal to observability. Canonical order after 09: **20 → 18 → 19 → 22 → 21 → 11 → 10** (see `IMPLEMENTATION-ROADMAP.md` P0).

## 4. Per-layer contract sync (what each system must honor)

| Layer | Obligation |
|---|---|
| backend | owns all writes (AI never writes SQL directly); ProblemDetails; `X-Request-Id` on every response |
| web/mobile | consume `Notifications`/`activity`/`logs` endpoints; surface `X-Request-Id` in error toasts for support; never compute fan-out |
| ai/mcp | every tool call arrives with the service-token principal → attributed in `ApiLogs.UserId`; `ai/` gate "audit rows per tool call" becomes testable after backend 20 + 09 |
| infra | backup sidecar + volume snapshots (specs 03/06 sync), Netdata alerts on `ErrorLogs` counts (spec 07) |
| qa | folder-14 collection asserts trails; xUnit failure-isolation tests; k6 baselines correlated with `ApiLogs.DurationMs` |

## 5. Incident query recipes (executable once backend 20 ships)

```sql
-- What/where/why/when for one request the user reported:
SELECT * FROM ErrorLogs  WHERE RequestId = @req;
SELECT * FROM ApiLogs    WHERE RequestId = @req;
-- Spillover: everything that happened in the same incident window:
SELECT * FROM AuditLogs  WHERE CreatedAt BETWEEN @t1 AND @t2 ORDER BY CreatedAt;
-- How many users affected:
SELECT COUNT(DISTINCT UserId) FROM ApiLogs WHERE CreatedAt BETWEEN @t1 AND @t2 AND StatusCode >= 500;
-- What a mutation changed (the "why" diff):
SELECT ActorId, Action, Before, After, CreatedAt FROM AuditLogs
WHERE EntityType = @type AND EntityId = @id ORDER BY CreatedAt DESC;
```

## 6. Mitigations decided (this branch)

1. Backend spec **20** fills the tables (middleware + exception persistence + `IAuditService` + feed writer + retention proc).
2. Spec **21** adds DB triggers (catch-all) + FULL/DIFF/LOG backup chain + restore drill + scale-out posture.
3. Spec **18** caps/standardizes queries; spec **19** adds cache + rate-limit partitions.
4. Spec **22** wires events → in-app + email.
5. MONITORING.md, roadmap, dependency audit, all trackers, AGENTS files, context files and the Postman collection are contract-synced in this branch; folder 14 makes the trails testable from day one.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.