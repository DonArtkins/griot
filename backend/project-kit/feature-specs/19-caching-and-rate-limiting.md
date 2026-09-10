# Backend Feature Spec 19 — Caching & Rate Limiting

**Status:** PLANNED (partially implemented: global 100/min fixed window + Redis OTP limiter exist — everything below extends them)

## What This Delivers

The performance-protection layer: Redis response caching with explicit invalidation, and per-route rate-limit partitions so auth/OTP/webhook/GraphQL surfaces cannot be brute-forced through the global window. Spec 18 gives the query contract; this spec makes it fast and abuse-resistant.

## Dependencies

- Feature 18 (query contract), Feature 07 (✅ Redis infrastructure), Feature 03 (✅ `usp_GetDashboardSummary`).

## Context To Read First

- `docs/planning/CACHING-REFRESH-SYNC-STRATEGY.md` (§1 backend tier, §8 metrics — this spec implements its backend rows)
- `backend/project-kit/context/architecture.md` (Phase 1 optimization bullets)

## Files Owned

- `backend/src/Griot.Api/Services/ResponseCacheService.cs` (+ interface in `Griot.Application.Interfaces`)
- `backend/src/Griot.Api/Program.cs` (limiter partitions + cache pipeline registration)
- `backend/src/Griot.Application/Services/DomainService.cs` (cache-aside around reads)
- `backend/Postman/Griot.postman_collection.json` (cache HIT/MISS + 429 tests)

## Rate-limit partitions (planned values — all labeled, tuned after k6)

| Partition | Scope | Limit | Notes |
|---|---|---|---|
| global (exists) | IP-or-user | 100/min fixed | unchanged |
| `auth` | `/api/auth/login`, `/refresh` per IP | 10/min, queue 0 | brute-force protection; 429 + `Retry-After` |
| `otp` (exists) | per email via Redis | 3/15min | unchanged, spec 12 |
| `webhook` | `/api/webhooks/trigger` per source | 60/min | HMAC verified first, limiter second |
| `graphql-mutation` | per user | 30/min | protects write amplification |
| GraphQL cost | query depth 10, complexity 1000 | — | HotChocolate `MaxAllowedComplexity`/`MaxAllowedDepth`; 400 `TOO_COMPLEX` |

**Implemented today (verified in `Program.cs`):** single global fixed window, 100/min, partition key = `User.Identity.Name` ?? IP. Everything else in this table is planned work.

## Caching (Redis, cache-aside, planned)

| Data | Key | TTL | Invalidated by |
|---|---|---|---|
| Dashboard summary (Phase 1, from architecture.md) | `cache:dashboard:{workspaceId}` | 60s | task/comment/member writes in that workspace |
| Board columns+tasks page | `cache:board:{boardId}:v{filterHash}` | 30s | task create/move/update/delete, column write |
| Notifications unread count | `cache:notif:unread:{userId}` | 15s | any notification write for the user |
| Unvalidated reads (errors/audit logs) | **never cached** | — | observability must be live (spec 20) |

- Miss path: `GET` → key lookup → DB → `SETEX` → `X-Cache: MISS`; hit → `X-Cache: HIT` header (asserted in Postman).
- Invalidation = key deletion in the same service method that performed the write (no pub/sub in v1; single API instance).
- Fail-open: Redis unavailable → log warning (spec 20 `ErrorLogs`), serve from DB. Cache errors never fail a request.
- All authed responses already must send `Cache-Control: no-store, private` (verified missing on some log routes — added here).

## Implementation Notes

- Cache service lives in `Griot.Api` composition only if it touches `HttpContext` headers; the pure Redis read/write lives in `Griot.Infrastructure.Redis` next to the existing connection — `Griot.Application` stays Redis-free (architecture.md layer table).
- Limiter options are read from configuration (`RateLimit__:Auth__PermitLimit` etc.) with the documented defaults; `.env.example` rows added.

## Separation of Concerns

Caching is a `Griot.Api`/`Griot.Infrastructure` concern wrapped around `Griot.Application` reads; services never know a cache exists.

## Docker & Deploy

No new service (Redis already in compose). Railway: same `Redis__Connection`.

## Out of Scope

GraphQL persisted queries, output caching middleware (ASP.NET `OutputCache`) — re-evaluate post-k6; CDN edge caching.

## Acceptance Criteria (all pending)

- [ ] Second identical dashboard call within TTL → `X-Cache: HIT` and no SQL round-trip (test via Dapper profiler/log)
- [ ] Task move invalidates `cache:board:{id}:*`; subsequent read reflects the move (integration test)
- [ ] 11th `/api/auth/login` in a minute from one IP → 429 with `Retry-After`; Postman asserts it
- [ ] GraphQL query of depth > 10 → 400 `TOO_COMPLEX`; normal board query passes
- [ ] Redis down → all reads still succeed (fail-open test), error surfaced in `ErrorLogs`
- [ ] `.env.example` + integration-contracts env table carry the `RateLimit__*` rows

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.