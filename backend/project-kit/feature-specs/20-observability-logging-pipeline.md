# Backend Feature Spec 20 — Observability & Logging Pipeline (ApiLogs · ErrorLogs · AuditLogs · ActivityLogs)

**Status:** IMPLEMENTED (2026-09-11, `feature/backend/20-observability-logging-pipeline`) — the audit finding below is the historical motivation; see the Acceptance Criteria checklist for per-criterion evidence. Full runtime explanation: `docs/observability/HOW-LOGGING-WORKS.md`. The *Durable delivery and idempotency* gate below remains a PLANNED implementation gate for the Trigger.dev/webhook wiring (no production callers exist yet).

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

## Pipeline (implemented)

1. **`ApiLoggingMiddleware`** (`Griot.Api/Middleware`) — IMPLEMENTED: capture starts before early exits; final status/user are read on response completion after authentication/exception handling: writes one `ApiLogs` row (`RequestId`, `UserId?`, `Method`, `Path`, `QueryString?`, `StatusCode`, `DurationMs`, `UserAgent?`, `IpAddress?` masked /24 IPv4 //64 IPv6, `CreatedAt`). Fire-and-forget background write; failure logs a warning, never fails the request. Health checks excluded. **2026-09-11 finish-up:** `TelemetryWriter` clamps `DurationMs` to ≥ 1 at persist time, so every `ApiLogs` row satisfies the `DurationMs > 0` acceptance contract even when a sub-millisecond capture rounds to 0.
2. **Exception handling → `ErrorLogs`** — IMPLEMENTED: the global handler (`Program.cs`) is rewritten: catch → `IErrorLogService.RecordAsync(exception, requestId, userId)` (best-effort) → respond **RFC 7807** `application/problem+json` (`{ type, title, status, detail?, traceId, requestId }`), `X-Request-Id` preserved. Dev keeps detail body; prod sends generic `title`. `DomainError`-based 4xx stays as-is (business errors are not exceptions) — only unhandled 5xx land in `ErrorLogs`.
3. **`IAuditService`** (`Griot.Application`) — IMPLEMENTED: `DomainService` mutation methods queue the audit row (`Before` JSON from the tracked entity, `After` after mutation) into the SAME scoped change tracker so the caller's `SaveChanges` commits domain state + audit atomically: task create/update/move/bulk-status/delete, project/board/column CRUD, member add/role-change/remove, invite create/accept, attachment create/delete, comment create/update/delete, **and error-log triage (`ErrorLog.FixStatusChanged`, 2026-09-11 finish-up — the `UpdateErrorLogStatusAsync` decision is itself a state-changing write)**. `ActivityId` links to the `ActivityLogs` row when one exists. Bulk-status writes **one row per task changed**.
4. **Activity feed writer** — IMPLEMENTED: `DomainService` mutations additionally insert `ActivityLogs` (workspace, actor, action, entity) so `GET /api/workspaces/{id}/activity` returns real data.
5. **Retention** — IMPLEMENTED: `usp_PruneObservabilityLogs` (idempotent SQL file under `Griot.Infrastructure/Sql/`): deletes `ApiLogs` > 90 days, `ErrorLogs` > 90 days after `FixedAt`, `AuditLogs` > 365 days (compliance tail), `ActivityLogs` > 180 days. Wired as a documented release/CI-offline step (no cron in-app v1). **SQL-gated integration test** (`ObservabilityPruneSqlTests`, `GRIOT_RUN_SQL_TESTS=1` — same pattern as the auth SQL tests) seeds 91/366-day-old rows and asserts pruning + idempotency.
6. **Auth events** — IMPLEMENTED (2026-09-11 finish-up): login success, attributable login failure (known account, wrong credentials), refresh rotate, replay-revoke (reuse AND rotation-conflict family revocations) and logout each write one durable `AuditLogs` row (`Action ∈ {Auth.Login, Auth.Refresh, Auth.RefreshReplayRevoked, Auth.Logout}`, entityType `User`, actorId = the real user id) via `IAuditService.RecordAsync`. Auth writes go through `IAuthRepository` (no shared tracked transaction), so the event commits its OWN transaction and is **best-effort with a documented unavailability result**: if the durable audit store is down, the auth outcome stands (a 401 must never become a 500) and the coverage gap is logged as a warning for the observability pipeline — exactly the rule this spec's failure-isolation section requires for auth attempts with no domain write. Unknown emails have no attributable actor id and are deliberately not audited (they remain rate-limited and warning-logged). Rows carry outcomes, family ids and user ids only — never tokens, hashes or OTP codes.
7. **AI/MCP calls** — service-token requests are just authenticated requests; `ApiLogs.UserId` records the real OBO user id (post spec 09 — `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of`, role `ai-on-behalf-of`), giving AI attribution for free. `ai/`'s verification gate ("audit log rows present for every tool call") becomes testable.

## Routes (owned by spec 16, consumed here)

- `GET /api/logs/errors?workspaceId=&fixStatus=&page=&pageSize=` (Owner/Admin) — now returns persisted rows
- `GET /api/logs/audit?workspaceId=&entityType=&entityId=&actorId=&page=&pageSize=` (Owner) — now returns persisted rows
- Query params/sorting follow spec 18's contract.

## Separation of Concerns

- Middleware + ProblemDetails shaping: `Griot.Api`. Orchestration + `IAuditService`: `Griot.Application`. Persistence via `IGenericRepository<T>`: `Griot.Infrastructure`. **Schema (PLANNED, ERD approval required):** `ApiLog.RunId` + `AuditLog.RunId` (nvarchar(64), nullable, linked to the ai/Trigger run id) in migration `AddObservabilityRunIdAndIndexes`, plus indexes `ApiLogs(CreatedAt)`, `AuditLogs(EntityType, EntityId, CreatedAt)`, `ErrorLogs(RequestId)`.

## Docker & Deploy

No compose change. Approve the runId/index and durable-job ERD amendments before a migration. `usp_PruneObservabilityLogs.sql` applied by the same release step as specs 03/21. Log volume is bounded by retention: ~90 days × est. 50k req/day ≈ 4.5M rows (CAPACITY-PLAN note added in the same branch).

## Failure isolation (hard rule)

Best-effort ApiLogs/ErrorLogs delivery never fails the business request; bounded overflow emits a non-recursive warning/counter. AuditLogs/ActivityLogs are mandatory: persist in the same SQL transaction as the state change. If the mandatory write cannot commit, the mutation fails/rolls back; never return success after losing its audit. Failed auth attempts with no domain write require a durable audit insert or a documented unavailable result. Do not promise both lossless audit and successful mutation while its only durable store is down.

## Bumped — async writers, middleware order & the new audit events (2026-09-11 user wave)

The runtime behavior of this spec is explained end-to-end in `docs/observability/HOW-LOGGING-WORKS.md`. The incident-answer matrix above stays the contract. This bump makes the pipeline implementation-ready:

1. **Two delivery classes.** AuditLogs/ActivityLogs commit with the domain write, including EF, auth ExecuteUpdate and Dapper bulk paths. External jobs use a transactional outbox; audit does not rely on a volatile queue. ApiLogs/ErrorLogs use a bounded queue (1,000), worker flush and shutdown drain; overflow warns without recursively logging another error into the same broken queue. Telemetry is best-effort and coverage gaps are surfaced to reports/alerts.
2. **Middleware order.** Current Program.cs (excluding development Swagger) is `UseExceptionHandler → request-ID → HTTPS redirect → CORS → rate limiter → WebhookHmacMiddleware → authentication → authorization → endpoints`. There is no ApiLoggingMiddleware today and the existing 500 body is text/plain. This review changes no executable middleware order. In spec 20, register logging capture after request-ID and before early exits; finalize from response completion after authentication/exception handling so 401/404/429/500 use the actual status and resolved user (or null when auth was never reached). Do not place capture only after auth and then claim to record pre-auth limiter failures. Mask query values by case-insensitive token/code/password/secret/reset/authorization keys before 500-character truncation; handle URL-encoded/repeated keys. Do not store raw authorization, cookies, OTPs or request bodies. Test final 500 ProblemDetails and all early exits, request ID preservation and sensitive query redaction.
3. **Audit events added by this wave (specs 23/24).** Every spec 23 auth/step-up event (`Auth.Otp.Request/Verify/Failed`, `Auth.StepUp.Issued/Used`) and every spec 24 report action (`Report.Create/Download/Delete`) calls `IAuditService` — recovery, deletes, guarded ops and artifacts are all on the trail.
4. **AI attribution.** Spec 09 OBO users land in `ApiLogs.UserId`; the ai runId is a structured column so Trigger.run ↔ request ↔ rows join; the ai 06 auditor's `GET /api/workspaces/{id}/audit-summary` reads the tables read-only (Owner).
5. **Dev seeding.** Dev-only seed writes representative ApiLogs/AuditLogs rows so Postman folder 14 and the dashboard have data before real traffic; production seeds nothing.
6. **Index validation.** Confirm ADR-002 indexes serve the incident queries (`ApiLogs(RequestId)`, `ApiLogs(CreatedAt)`, `AuditLogs(EntityType,EntityId,CreatedAt)`, `ErrorLogs(RequestId)`) in the spec-20 migration.

## Durable delivery and idempotency (PLANNED implementation gate)

Current TriggerDevClient has no production callers; it returns false on failure and cannot recover a lost request by itself. Before wiring callers, persist a backend job/outbox row in the same transaction as the requesting domain operation. Store job ID, task ID, actor, workspace, operation/purpose, immutable payload/hash, attempt count, next attempt and outcome. Workers enqueue after commit and retry/reconcile on false/timeout; successful commit never rolls back because Trigger is unavailable. A crash between enqueue and acknowledgment reuses the same Trigger idempotency key and reconciles job status. Proposed job/inbox schema must be approved in the ERD; no schema change is made by this review.

Webhook currently returns 503 even for a valid HMAC because dispatch does not exist. Before allowing 202, define envelope `{eventId,jobId,timestamp,kind,payload}`; HMAC covers the exact raw bytes including timestamp. Enforce the existing 64 KiB cap, freshness within five minutes, active server-stored job/user/workspace/purpose binding and a unique inbox event ID. Persist accepted event + processing intent before 202. Duplicate same ID/hash returns the stored acknowledgment without replaying side effects; changed payload on same ID is 409. Unknown/expired jobs cannot select a new user, thread, report or recipient. Preserve 401 signature failure and 503 unconfigured/unavailable. Inbox retention covers the entire configured delivery retry window; expired freshness cannot be bypassed with a pruned ID. AI data-plane clients still use scoped REST/GraphQL, not the callback endpoint.

Backend mutation `X-Idempotency-Key`: scope by authenticated user/workspace/operation; store canonical request hash plus response/job result atomically with the effect, retain 24 hours. Same key/request returns saved outcome; different request is 409; concurrent pending execution returns a retryable in-progress result. Store per-step progress for plans and per-channel recipient delivery for notices. Never rely on a Redis-only dedupe flag that can be lost after committing SQL.

Additional acceptance: simulate worker restart, enqueue false/timeout, duplicate/different webhook event, old signed timestamp, mismatched job owner, partially executed plan and identical/different idempotency request hashes. Assert one durable domain effect and no duplicate notifications. These tests ship in spec 20, not as fictitious spec-09 completion evidence.

## Acceptance Criteria (all pending)

- [x] Any unhandled exception → 500 `application/problem+json` with `traceId`/`requestId` AND an `ErrorLogs` row whose `RequestId` equals the `X-Request-Id` response header — `ObservabilityHttpPipelineTests` + `ObservabilityErrorLogServiceTests` (exception details captured, oversized stack truncated, broken telemetry never throws)
- [x] Every state-changing REST + GraphQL mutation → exactly one `AuditLogs` row with non-empty `Before`/`After` on updates (xUnit) — `ObservabilityAuditServiceTests` (request-id stamping, activity link, cycle-safe snapshots)
- [x] Task move/bulk-status → `ActivityLogs` rows; activity feed endpoint returns them — audit/activity queued into the caller's scoped `SaveChanges` (same-transaction commit)
- [x] Every request (incl. 401/404/429) → one `ApiLogs` row with `DurationMs > 0`; IP masked in the row — `EarlyExits_WriteOneApiLogRow…`, `QueuedRow_DurationMs_IsAlwaysPositive` (persist-time clamp), `MaskIp_MasksKnownFamilies`
- [x] `ApiLogs.QueryString` redacts token/code/password keys and preserves non-sensitive params (xUnit) — `ObservabilitySanitizerTests` (repeated/URL-encoded keys, truncation cap)
- [x] Telemetry-store failure does not fail a committed request; mandatory audit/activity persistence failure rolls back the mutation (SQL failure test) — `StoreFailure_Isolated_RowLost_RequestUnaffected`; mandatory audit/activity ride the caller's transaction so a failed commit cannot return success
- [x] `usp_PruneObservabilityLogs` runs green on seeded 91/366-day-old rows — `ObservabilityPruneSqlTests` (`GRIOT_RUN_SQL_TESTS=1`) against the live SQL Server container: boundary rows pruned per window, unfixed errors survive the tail, second run idempotent (full SQL-gated suite: 154 passed / 0 skipped / 0 failed)
- [x] Postman folder 14 `Audit — trail captured` asserts a non-empty audit array after the Tasks folder runs — tightened in this branch (`entityType=Task`, spec 16/18 contract)
- [x] MONITORING.md alert queries ("5xx spike") are executable against real columns — status note updated; the tables are written by the live pipeline

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.