# Observability & Hardening — Old State vs New State (2026-09-10)

> The single document explaining **everything that changed and got added** in the 2026-09-10 observability/hardening audit: what the system looked like before, what it looks like now, **why each change was made**, and **what it means for every layer**. Evidence: `LOGGING-AUDIT-REPORT.md` · Decision: `ADR-004` · Specs: backend 18–22.

## 1. Why this happened

A source-level audit (2026-09-10) verified the observability tables designed into the schema (`ApiLogs`, `ErrorLogs`, `AuditLogs`, `ActivityLogs` — ADR-002) had **zero writers anywhere in the codebase**, and several protection layers existed only as prose. The user directive: every error, log, crash from every layer must be persisted (what/where/why/when/trigger/spillover/affected users/mitigation), the database must have triggers + backup copies that a crash or attack cannot destroy, and search/pagination/caching/rate-limiting/error-handling/notifications must all be specified, documented, and testable.

## 2. Old state vs new state

| # | Concern | OLD state (before 2026-09-10) | NEW state (this branch → implementation) | Why |
|---|---|---|---|---|
| 1 | Log tables | Entities + DbSets + indexes existed; **0 rows ever written** (30+ `SaveChanges` sites, none log) | **Spec 20**: `ApiLoggingMiddleware` → `ApiLogs`; exception handler → `ErrorLogs`; `IAuditService` → `AuditLogs` before/after JSON; activity-feed writer; retention proc | "If anything happens, I will know" — logs must be real-time, not designed |
| 2 | Error handling | Global handler returned `text/plain` 500, persisted **nothing** | **Spec 20**: RFC 7807 `application/problem+json` (`traceId`/`requestId`) + persisted `ErrorLogs` row with same `RequestId` | Crashes were invisible to the DB; clients got unparseable errors |
| 3 | Incident forensics | Impossible — no data | **Spec 20** incident answer matrix + SQL recipes (spillover via shared `RequestId`; affected users via `COUNT(DISTINCT UserId)`) | The what/where/why/when/trigger/spillover/how-many-users/mitigation requirement |
| 4 | Activity feed | Permanently empty (reader, no writer) | **Spec 20** writes `ActivityLogs` on every mutation | First time `GET /api/workspaces/{id}/activity` returns data |
| 5 | DB triggers | **Zero** `CREATE TRIGGER` in repo | **Spec 21**: `AFTER` triggers on TaskItems/WorkspaceMembers/Invites/Attachments → `AuditLogs` (`DB.*` actions, low-fidelity catch-all) | Audit survives app bugs, raw SQL, migrations — not just app code |
| 6 | Backups | **None for SQL Server** (only the Railway *PostgreSQL* PITR runbook) | **Spec 21**: FULL nightly + DIFF 15 min + LOG 10 min on a dedicated volume, opt-in compose sidecar, rehearsed restore drill, `docs/database/BACKUP-RESTORE-DRILL.md` | "A crash or attack doesn't render our system useless — we need copies" |
| 7 | Rate limiting | One global fixed window 100/min | **Spec 19**: partitions for auth (10/min), OTP (existing 3/15min), webhook (60/min), GraphQL mutations (30/min) + GraphQL depth/complexity caps | Brute-force/abuse protection per surface, not one shared budget |
| 8 | Caching | Documented only (architecture.md Phase 1) | **Spec 19**: Redis cache-aside (dashboard 60s, board 30s, unread 15s), `X-Cache` HIT/MISS, explicit invalidation, fail-open | "My database and backend is faster" — with real invalidation rules |
| 9 | Search/filter/pagination/sort | Ad-hoc: fixed page size, no sort contract, no search | **Spec 18**: `PagedResult<T>` envelope, hard cap 100, sort whitelists, escaped `q` search, per-endpoint filter matrix, REST+GraphQL parity | Predictable performance + no injection surface; 19's cache keys depend on it |
| 10 | Notifications | Read-only routes; **no producer, no email** | **Spec 22**: fan-out on assignment/mention/due-date/invite/role-change → in-app always + Brevo email per new `NotificationPreferences`; service-token `fanout` endpoint for scheduled agents | Email + in-app notifications requirement, reusing spec-12 sender identities |
| 11 | Postman coverage | No observability tests | Folder 14 "Observability & audit" (log-route guard tests + audit-trail assertion), tolerant now → strict when specs land | "Modify the Postman files to capture all these" |
| 12 | Docs/trackers/AGENTS | Pre-audit state; P0 = 09 → 11 → 10 | P0 reordered **09 → 20 → 18 → 19 → 22 → 21 → 11 → 10 (snapshot as of 2026-09-10; the 2026-09-11 AI-superpowers/2FA wave inserted 23 → 11 → 24 → 10 and ai 06–08 / web 11 / mcp 06 — see IMPLEMENTATION-ROADMAP.md)**; roadmap, DEPENDENCY-AUDIT, root+backend AGENTS, api-surface, integration-contracts, MONITORING, DATABASE-DESIGN, infra spec 03, web/qa trackers, CHANGELOG all synced | "Bump up context files and spec files and agents files across every layer" |

## 3. Flow & order of implementation between all layers

**Build order (P0 backend close-out):** `09` (AI gateway — unblocks `ai/`+`mcp/`) → `20` (fills the tables — every later layer reads them) → `18` (read plane 20's data is queried through) → `19` (protects 20+18) → `22` (consumes 20's event hooks + 19's limits) → `21` (triggers land after 20 so `AuditLogs` dedupe conventions exist; backups) → `11` (blob) → `10` (freeze the hardened surface as API docs).

## 4. What it means for every layer

| Layer | Impact | Action required there |
|---|---|---|
| **backend** (owner) | Writes the tables, shapes errors, enforces limits, owns the migration (`NotificationPreferences` only) | Implement 20 → 18 → 19 → 22 → 21, one spec per branch, gates: `dotnet build` + `dotnet test` green |
| **web** | Error toasts gain parseable ProblemDetails + `X-Request-Id` (support pins incidents); notifications bell + activity feed become live; unread-count polling respects `Cache-Control` | Web 09 consumes the fan-out; no code change until P1 |
| **mobile** | Same REST contract; notifications + feed parity | Mobile 07 unchanged in plan |
| **ai** | Every tool call is attributed via `ApiLogs.UserId` = the real OBO user id (post spec 09 — `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of`, role `ai-on-behalf-of`); "audit rows per tool call" gate becomes testable | No new deps; benefits from 20 + 09 |
| **mcp** | Same service-token attribution; rate-limit partition (30/min mutations) applies to its GraphQL writes | Contract tests updated post 19 |
| **infra** | Backup sidecar + backup volume in compose (profile `backup`); Netdata alerts (spec 07) read real `ErrorLogs` counts; release step applies `usp_PruneObservabilityLogs` + `audit-triggers.sql` | Spec 03/06 sync notes already merged |
| **qa** | Newman folder 14 asserts trails; xUnit failure-isolation + pagination-cap tests; k6 baselines correlate with `ApiLogs.DurationMs` for honest p95 evidence | qa 04/05 update after 20–22 land |
| **ops/incident flow** | The incident answer matrix (what/where/why/when/trigger/spillover/how-many-users/mitigation) is executable SQL against real tables; restore drill gives the attack/crash recovery path | RUNBOOK-ROLLBACK + BACKUP-RESTORE-DRILL are the runbooks |

## 5. Acceptance status discipline

All five specs are **PLANNED**: acceptance checkboxes are unchecked by definition, planned routes in `api-surface.md` are labeled PLANNED, and planned behavior does not count as implemented acceptance evidence (hard gate). The Postman folder 14 uses tolerant assertions until each spec lands, then they tighten to strict — the collection never goes red on a green pipeline.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.