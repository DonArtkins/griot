# How Griot's Logging Works — ApiLogs · ErrorLogs · AuditLogs · ActivityLogs

**Owner:** `backend/project-kit/feature-specs/20-observability-logging-pipeline.md` · **The gap it closes (audit):** `docs/observability/LOGGING-AUDIT-REPORT.md` · **Status:** describes the PLANNED spec 20 pipeline — the four tables exist in the schema today; the writers ship with backend spec 20.

## The one-sentence story

Every HTTP request, every unhandled exception, every state-changing write and every user-facing activity event is persisted to one of four log tables in real time, linked by a `RequestId` that travels from the client's `X-Request-Id` header all the way to the exception handler — so any incident answers **what happened, where, why, when, what triggered it, who was affected, and how to fix it** (the "incident answer matrix").

## The request lifecycle (what writes where)

```
Client ─ X-Request-Id: abc123 ─▶ RequestIdMiddleware (assigns/normalizes)
        │
        ▼
   ApiLoggingMiddleware ─(records on completion)─▶ ApiLogs  (every request except /health)
        │
        ▼
   Auth → Controller / GraphQL → Griot.Application service
        │        │
        │        └─ state-changing mutation → IAuditService.RecordAsync(Before, After) ─▶ AuditLogs
        │        └─ user-facing event (task moved, status changed) ─▶ ActivityLogs ─▶ activity feed
        ▼
   Unhandled 5xx → GlobalExceptionHandler ─▶ ErrorLogs + RFC 7807 application/problem+json
        ▼
   Response carries X-Request-Id: abc123  ◀── the same id every table stored
```

**Delivery classes (PLANNED):** AuditLogs and ActivityLogs persist in the same SQL transaction as the domain mutation. A mandatory audit failure prevents success and rolls the mutation back. External jobs use a transactional outbox; post-commit delivery failures retry without undoing domain state. ApiLogs/ErrorLogs are best-effort bounded telemetry; overflow warns and coverage gaps remain visible. Telemetry isolation does not mean that lossless audit is possible while all durable storage is unavailable.

## The four tables (what each one is for)

| Table | Written by | Stores | Answers |
|---|---|---|---|
| `ApiLogs` | middleware, on request completion | requestId, userId, method, path, queryString, statusCode, durationMs, userAgent, masked IP, createdAt | what/when/how many users; latency and 4xx/5xx rates |
| `ErrorLogs` | global exception handler, unhandled 5xx only | requestId, exceptionType, message, stackTrace, source, fixStatus, createdAt | what happened and is it fixed |
| `AuditLogs` | `IAuditService` inside every state-changing write (REST + GraphQL + auth events) | actorId, action, entityType, entityId, before/after JSON diff, requestId, createdAt | who changed what, before/after |
| `ActivityLogs` | the same mutations, user-facing subset | workspaceId, actor, action, entity, createdAt | the activity feed |

One user click can produce **all four rows** — see the worked example below.

## Correlation (the chain that makes one incident reconstructable)

- `RequestIdMiddleware` guarantees every response carries `X-Request-Id`; ApiLogs/ErrorLogs/AuditLogs store the same value.
- Auth events (login, refresh rotation, replay-revoke, OTP verify, step-up issue/use — specs 20/23) write AuditLogs with that same request id.
- AI calls arrive with `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of` → `ApiLogs.UserId` records the **real user id**, so persisted request attribution becomes available when spec 20 ships; the ai layer's `runId` is carried as a structured field so Trigger run ↔ API request ↔ DB rows link up.

## Retention (bounded, compliance-aware)

`usp_PruneObservabilityLogs` (spec 20, offline release step) deletes: `ApiLogs` > 90 days, `ErrorLogs` > 90 days after `FixedAt`, `AuditLogs` > 365 days (compliance tail), `ActivityLogs` > 180 days. Idempotent SQL file under `Griot.Infrastructure/Sql/`; applied by the same release step as specs 03/21. Volume bound: ~90 days × ~50k req/day ≈ 4.5M `ApiLogs` rows (see `docs/planning/CAPACITY-PLAN.md`).

## Worked example — "move one task"

Alice drags a task to Done. One request produces, in order:

1. On completion, best-effort `ApiLogs` row — `PATCH /api/tasks/{id}/move`, 200, durationMs 42, userId=Alice, requestId=abc123.
2. `AuditLogs` row — action `Task.Move`, entityType TaskItem, entityId, Before `{status: InReview}`, After `{status: Done}`, actorId=Alice, requestId=abc123.
3. `ActivityLogs` row — "Alice moved task to Done" (feeds `GET /api/workspaces/{id}/activity`).
4. If the transaction fails, neither the domain change nor success audit/activity commits; best-effort `ErrorLogs` row (exceptionType, stack) + `application/problem+json` response; the response still carries `X-Request-Id`.

Later, support searches ErrorLogs by requestId, finds the AuditLogs Before/After diff, and sees the ApiLogs durationMs spike — one incident fully reconstructable.

## How the AI superpowers consume it

- The ai 06 system auditor reads backend 25 sanitized workspace audit/health; raw `/api/logs/*` only for SuperAdmin/Dev within an explicit delegation + the new `GET /api/workspaces/{id}/audit-summary` (spec 24) to answer "is my system healthy / what happened / who changed what", then persists findings as a Report row (spec 24).
- AI requests have a real OBO identity after spec 09, but the ApiLogs writer is still PLANNED in spec 20 — the "audit rows per tool call" gate becomes testable the day spec 20 ships.

## Middleware and recovery status

Current Program.cs keeps UseExceptionHandler first, then request ID, HTTPS/CORS, rate limiter, HMAC, authentication, authorization and endpoints. ApiLoggingMiddleware is not implemented. Spec 20 captures before early exits and finalizes after response status/auth are known; this is how 401/404/429 and handled 500s are included. Current 500 is text/plain; ProblemDetails is planned. Raw query secrets are redacted before truncation.

Spec 20 also owns durable enqueue recovery, the signed timestamp/event-ID/job-bound callback inbox and the 24-hour request-hash/response idempotency store. Until then verified callbacks return 503 and no production caller should rely on enqueue recovery. See its concrete acceptance tests.

## Reading the logs

`docs/observability/LOGGING-AUDIT-REPORT.md` §5 has the SQL recipes (find a request, spillover window, user counts, mutation diffs). `docs/observability/MONITORING.md` turns ErrorLogs counts into alerts (Netdata, infra 07).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.