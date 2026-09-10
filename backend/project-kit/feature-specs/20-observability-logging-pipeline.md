# Backend Feature Spec 20 — Observability & Logging Pipeline (ApiLogs · ErrorLogs · AuditLogs · ActivityLogs)

**Status:** PLANNED — this spec exists because the 2026-09-10 audit found the observability tables are dead (see `docs/observability/LOGGING-AUDIT-REPORT.md`).

## Audit finding (verified against source)

Entities, `DbSet`s, indexes and the ADR-002 decision exist — but **zero code writes them**. The write-path scan (30+ `SaveChangesAsync` sites across `DomainService`, `AuthService`, `AuthRepository` incl. `ExecuteUpdateAsync`, 11 in `GriotMutation`) found no insert into any log entity. `GetErrorLogsAsync`/`GetAuditLogsAsync` read empty tables. The global exception handler returns `text/plain` 500 and persists nothing. The activity feed has no writer. `docs/observability/MONITORING.md` §2–3 describes a pipeline that does not exist yet.

## What This Delivers

Real-time persistence of **every layer's signals** so any incident answers: what happened, where, why, at what time, what triggered it, the affected spillover, how many users, and the mitigation path.

### The incident answer matrix (contract)

| Question | Answered by |
|---|---|
| What happened | `ErrorLogs.ExceptionType/Message` · `AuditLogs.Action` |
| Where | `ErrorLogs.Source` · `ApiLogs.Method`+`Path` |
| Why | `ErrorLogs.StackTrace` · `AuditLogs.Before`→`After` JSON diff |
| When | `CreatedAt` UTC on all four tables |
| What triggered it | `ApiLogs.RequestId` chain (same `X-Request-Id` the client saw) + `ApiLogs.UserId` |
| Affected spillover | rows sharing that `RequestId` · `AuditLogs(EntityType, EntityId)` fan-out |
| How many users | `COUNT(DISTINCT UserId)` on `ApiLogs` over the incident window |
| How to mitigate | `ErrorLogs.FixStatus` lifecycle + `docs/planning/RUNBOOK-ROLLBACK.md` |

Query recipes: `LOGGING-AUDIT-REPORT.md` §5.

## Pipeline (planned)

1. **`ApiLoggingMiddleware`** (`Griot.Api/Middleware`) — after auth, before MVC: on completion writes one `ApiLogs` row (`RequestId`, `UserId?`, `Method`, `Path`, `QueryString?`, `StatusCode`, `DurationMs`, `UserAgent?`, `IpAddress?` masked /24 IPv4 //64 IPv6, `CreatedAt`). Fire-and-forget background write; failure logs a warning, never fails the request. Health checks excluded.
2. **Exception handling → `ErrorLogs`** — the global handler (`Program.cs`) is rewritten: catch → `IErrorLogService.RecordAsync(exception, requestId, userId)` (best-effort) → respond **RFC 7807** `application/problem+json` (`{ type, title, status, detail?, traceId, requestId }`), `X-Request-Id` preserved. Dev keeps detail body; prod sends generic `title`. `DomainError`-based 4xx stays as-is (business errors are not exceptions) — only unhandled 5xx land in `ErrorLogs`.
3. **`IAuditService`** (`Griot.Application`) — called inside `DomainService`/`AuthService` mutation methods *before* the change (`Before` JSON from the tracked entity) and *after* (`After`), one `AuditLogs` row per state-changing write: task create/update/move/bulk-status/delete, project/board/column CRUD, member add/role-change/remove, invite create/accept, attachment create/delete, comment create/update/delete. `ActivityId` links to the `ActivityLogs` row when one exists. Bulk-status writes **one row per task changed**.
4. **Activity feed writer** — `DomainService` mutations additionally insert `ActivityLogs` (workspace, actor, action, entity) so `GET /api/workspaces/{id}/activity` returns real data for the first time.
5. **Retention** — `usp_PruneObservabilityLogs` (idempotent SQL file under `Griot.Infrastructure/Sql/`): deletes `ApiLogs` > 90 days, `ErrorLogs` > 90 days after `FixedAt`, `AuditLogs` > 365 days (compliance tail), `ActivityLogs` > 180 days. Wired as a documented release/CI-offline step (no cron in-app v1).
6. **Auth events** — login success/fail, refresh rotate, replay-revoke, logout write `AuditLogs` (`Action ∈ {Auth.Login, Auth.Refresh, Auth.RefreshReplayRevoked, Auth.Logout}`) so security incidents are reconstructable.
7. **AI/MCP calls** — service-token requests are just authenticated requests; `ApiLogs.UserId` records the real OBO user id (post spec 09 — `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of`, role `ai-on-behalf-of`), giving AI attribution for free. `ai/`'s verification gate ("audit log rows present for every tool call") becomes testable.

## Routes (owned by spec 16, consumed here)

- `GET /api/logs/errors?workspaceId=&fixStatus=&page=&pageSize=` (Owner/Admin) — now returns persisted rows
- `GET /api/logs/audit?workspaceId=&entityType=&entityId=&actorId=&page=&pageSize=` (Owner) — now returns persisted rows
- Query params/sorting follow spec 18's contract.

## Separation of Concerns

- Middleware + ProblemDetails shaping: `Griot.Api`. Orchestration + `IAuditService`: `Griot.Application`. Persistence via `IGenericRepository<T>`: `Griot.Infrastructure`. `Griot.Domain` unchanged (entities already exist — **no migration required**).

## Docker & Deploy

No compose change. `usp_PruneObservabilityLogs.sql` applied by the same release step as specs 03/21. Log volume is bounded by retention: ~90 days × est. 50k req/day ≈ 4.5M rows (CAPACITY-PLAN note added in the same branch).

## Failure isolation (hard rule)

A failing log write must **never** break the user request: every log write is wrapped, swallowed-with-log, and itself surfaced as an `ErrorLogs` row when possible. Tested by unit test.

## Acceptance Criteria (all pending)

- [ ] Any unhandled exception → 500 `application/problem+json` with `traceId`/`requestId` AND an `ErrorLogs` row whose `RequestId` equals the `X-Request-Id` response header
- [ ] Every state-changing REST + GraphQL mutation → exactly one `AuditLogs` row with non-empty `Before`/`After` on updates (xUnit)
- [ ] Task move/bulk-status → `ActivityLogs` rows; activity feed endpoint returns them
- [ ] Every request (incl. 401/404/429) → one `ApiLogs` row with `DurationMs > 0`; IP masked in the row
- [ ] Killing the DB connection during a log write does not fail the primary request (unit test)
- [ ] `usp_PruneObservabilityLogs` runs green on seeded 91/366-day-old rows
- [ ] Postman folder 14 `Audit — trail captured` asserts a non-empty audit array after the Tasks folder runs
- [ ] MONITORING.md alert queries ("5xx spike") are executable against real columns

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.