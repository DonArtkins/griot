# Feature 09 — AI Service Token + Webhooks (own-stack)

## Type

NEW FEATURE (`[own-stack]` — the AI boundary defined in `research/ai-integration.md`)

## What This Delivers

The trusted AI-onboarding surface, in **both directions** (orchestration contract: `research/ai-integration.md` §2a):

- **Trigger direction (.NET → Trigger.dev):** the backend enqueues AI tasks via Trigger.dev's REST API (server-to-server `TRIGGER_SECRET_KEY`) whenever something async or AI-related is needed — after validating/persisting the request. The backend is the only trigger source for request-originated work; the one exception is Trigger.dev's own cron scheduler, which starts the scheduled agents (`dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`) with no backend involvement (orchestration contract §2a).
- **Callback direction (Trigger.dev → .NET):** `POST /api/webhooks/trigger` verifies Trigger.dev webhooks via HMAC (`X-Trigger-Signature`) so background runs write results back through the API.
- **Service token (AI → .NET data plane):** `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of: {real User.Id}` resolves to a **real-user On-Behalf-Of (OBO) principal** with four restricted scope claims (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes, no invites, no member management). Implemented model: NOT a virtual `ai-agent` workspace member.

.NET remains the **only writer of source-of-truth data**; Trigger.dev is a compute/orchestration adapter, never a data owner. Web/mobile never trigger or poll Trigger.dev for task triggering or status — they call this API. Sole exception: the web Copilot panel consumes Trigger.dev's realtime WS as a scoped, read-only streaming delivery channel (no triggering, no status polling); mobile has no such exception.

## Dependencies

- Feature 07 (Bearer auth middleware exists to extend).

## Context To Read First

- `research/ai-integration.md` §2a (orchestration contract) + §7 (security & guardrails)
- `backend/project-kit/context/api-surface.md` (webhook route)

## Agent Skills To Use

- `ai/.agents/skills/ai-agent-security/SKILL.md` (scope/guardrail patterns)
- `backend/.agents/skills/jwt-argon2-auth/SKILL.md`

## Files Owned

- `backend/src/Griot.Api/Auth/ServiceTokenHandler.cs` (converts token + `X-On-Behalf-Of` header → real-user OBO principal)
- `backend/src/Griot.Api/Middleware/WebhookHmacMiddleware.cs`
- `backend/src/Griot.Api/Controllers/WebhookController.cs`
- `backend/src/Griot.Infrastructure/Integrations/TriggerDevClient.cs` (enqueue tasks by ID via Trigger.dev REST)

## Files

CREATE: service-token auth handler (constant-time compare of `GRIOT_SERVICE_TOKEN`; requires `X-On-Behalf-Of`, resolves a real user, and issues role `ai-on-behalf-of` with exactly four scope claims; no `wid` claim).
CREATE: HMAC middleware verifying `X-Trigger-Signature` against the Trigger webhook secret.
CREATE: `WebhookController` — `POST /api/webhooks/trigger` (relay to the job/routing logic).
MODIFY: `Program.cs` — register both.
RUN: `dotnet build`; test with a signed webhook fixture.

## Setup / Initialization

```bash
# env additions:
#   GRIOT_SERVICE_TOKEN=<long-random>       (shared with ai/mcp env)
#   TRIGGER_SECRET_KEY=<server-to-server>   (backend → Trigger.dev REST; NEVER exposed to web/mobile)
#   WEBHOOK_SECRET=<from Trigger.dev>       (Trigger.dev → backend HMAC callbacks; read by
#                                           WebhookHmacMiddleware as Webhook:Secret ?? WEBHOOK_SECRET)
```

## Implementation Notes

- The `ai-on-behalf-of` principal uses the real user's workspace memberships and the four scope claims; no synthetic workspace member is created.
- HMAC verified in middleware before any handler runs; failures return 401.
- The activity feed is the AI layer's audit + `summarize_project` source (no new tables).
- Enqueue-after-persist: a request that needs AI work is validated/persisted first, then `TriggerDevClient` enqueues the task by ID. If the enqueue fails, the domain write stands (AI is a post-processing adapter, not part of the transaction).
- Task results come back as HMAC-verified webhook calls or service-token REST writes — the backend stays the single writer of source-of-truth data.

## Separation of Concerns

- Auth policy = `Griot.Api` concern; the token is validated at the boundary, policy enforced by the same services any member uses. AI code stays out of the backend solution entirely.

## Docker & Deploy

- Env keys added to compose (infra) + Railway (infra spec 05) + `ai/mcp` environments. No new container.

## Out of Scope

AI scheduling, tool execution, LLM calls (all in `ai/`/`mcp/` systems).

## Future Modifications

- ai spec 03 + mcp spec 02 consume this surface; qa spec 11 audits the AI principal scope.

## Acceptance Criteria

- [x] Service token resolves to the restricted real-user OBO principal; deletes/invites rejected
  - `backend/src/Griot.Api/Auth/ServiceTokenHandler.cs` — constant-time token comparison, builds ClaimsPrincipal with role=`ai-on-behalf-of` + 4 scope claims (ReadWorkspace/CreateTask/AddComment/CreateNotification). Registered as `ServiceToken` auth scheme alongside JWT.
- [x] Webhook HMAC verified; bad signatures 401
  - `backend/src/Griot.Api/Middleware/WebhookHmacMiddleware.cs` — runs before UseAuthentication on POST /api/webhooks/trigger; constant-time HMAC-SHA256 comparison; 401 on mismatch, 503 if secret unconfigured.
  - `backend/src/Griot.Api/Controllers/WebhookController.cs` — refactored to clean thin handler (HMAC now in middleware).
- [x] `TriggerDevClient` enqueues a task by ID with `TRIGGER_SECRET_KEY`; enqueue failure does not roll back the domain write
  - `backend/src/Griot.Infrastructure/Integrations/TriggerDevClient.cs` — encapsulates REST calls to Trigger.dev; handles failure gracefully without triggering transaction rollbacks.
- [x] Web/mobile never receive any Trigger.dev credential (no `TRIGGER_SECRET_KEY` outside backend env)
  - Deployment configuration verification: secret strictly bound to backend service env; no client-side exposure.
- [ ] All AI tool calls are traceable to an ActivityLog row
  - **PLANNED** — ActivityLog writers arrive in backend spec 20 (observability pipeline). The surface is open (no new tables needed per spec); controllers log via `ILogger<T>` which correlates to `X-Request-Id`. Full ActivityLog persistence comes in spec 20.

## Pre-push SQL fixture repair (2026-09-10)

The full SQL gate initially failed all seven integration tests because `SqlAuthFixture`
used the current EF `User` mapping before applying the migration that adds
`EmailVerified`. The fixture now seeds the historical `Users` columns through
parameterized `ExecuteSqlInterpolatedAsync`, then migrates normally. No production
schema, migration, or API contract changed. Context7's EF Core documentation confirms
that interpolated values are passed as SQL parameters, not concatenated into SQL.

`Migration_PreservesExistingChain_AndIndependentActiveSession` now also verifies that
the migrated user has `EmailVerified = false` and retains `TwoFactorMethod.None`.
The focused regression passed; the full SQL-enabled suite passed **71/71**, with
zero skipped tests. Build: zero warnings/errors. `/health`: `Healthy`.
The fix and reproduction command are recorded in
`docs/api/ai-service-token-contract.md` under verification evidence.

## Pre-push watcher contention (2026-09-10)

The first push was blocked by `MSB4018` while writing `Griot.Api.deps.json`;
repeated builds also reported `MSB3026` for `Griot.Application.dll`. An existing
`dotnet watch run` process was rebuilding the same outputs. A single-worker build
still encountered contention, so no hook flags were changed.

With operator approval, the existing watcher processes were temporarily paused
while the unchanged pre-push hook ran, then automatically resumed. Verification:
build 0 warnings/errors, all 71 SQL-enabled tests passed, and contract sync passed.
No hook or verification gate was bypassed. Stop or pause the repository's development
watcher during clean pre-push rebuilds, then restore it afterward.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
