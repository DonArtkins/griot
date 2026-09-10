# AI Service-Token & Webhook Contract (Feature 09 — own-stack)

> **Status: IMPLEMENTED** on `feature/backend/09-ai-service-token-and-webhooks` (2026-09-11).
> Owning spec: `backend/project-kit/feature-specs/09-ai-service-token-and-webhooks.md`.
> Orchestration authority: `research/ai-integration.md` §2a.
> Cross-system env matrix: `project-kit/context/integration-contracts.md`.

This is the trusted **AI-onboarding surface in both directions**: the backend enqueues
Trigger.dev tasks (.NET → Trigger.dev) and Trigger.dev writes results back through the .NET
API (Trigger.dev → .NET). .NET remains the **only writer of source-of-truth data**; the
service token is the **only** way `ai/` + `mcp/` call the API on behalf of a user.

---

## 1. AI → .NET data plane: `GRIOT_SERVICE_TOKEN` (On-Behalf-Of)

**Implemented as a real-user On-Behalf-Of (OBO) principal — NOT a virtual "ai-agent"
workspace member.** The old model (a dedicated synthetic member with a well-known GUID)
was removed; every AI call impersonates a **real user** resolved from the `Users` table,
whose identity+RBAC *plus* the restricted scope claims govern what the call may do.

### Request shape

```
Authorization: Bearer {GRIOT_SERVICE_TOKEN}
X-On-Behalf-Of: {Guid}        # real User.Id from the Users table
```

- Missing/invalid `Authorization` → 401 (handler `AuthenticateResult.Fail`).
- Missing or non-GUID `X-On-Behalf-Of` → 401.
- Unknown OBO user id → 401 (`On-Behalf-Of user not found`).
- Token comparison is **constant-time** (`CryptographicOperations.FixedTimeEquals`).

### Auth scheme

| Item | Value |
|---|---|
| Scheme name | `ServiceToken` (`ServiceTokenHandler`, `Griot.Api.Auth`) |
| Config | `ServiceToken:Key` → env `GRIOT_SERVICE_TOKEN` |
| Routing | `MultiAuth` policy scheme — `ForwardDefaultSelector` picks `JwtBearer` for JWT-shaped bearers (exactly two dots), `ServiceToken` for any other bearer |

### Principal claims issued

| Claim | Value |
|---|---|
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | `User.Id` |
| `.../name` | `User.DisplayName` |
| `.../emailaddress` | `User.Email` |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | `ai-on-behalf-of` |
| `obo` | `User.Id` |
| `auth_method` | `service_token_obo` |
| `scope` × 4 | `ReadWorkspace`, `CreateTask`, `AddComment`, `CreateNotification` |

Exactly **four** scope claims — deletes, invites, member management and bulk writes are
**not** granted.

### Enforcement

- `DomainControllerBase.IsAiCall` = principal has role `ai-on-behalf-of` **or** claim
  `auth_method=service_token_obo`.
- `DomainControllerBase.ForbidIfAiCall()` → **403 Forbid** on every destructive endpoint
  when the caller is an AI OBO principal — enforced **on top of** the impersonated user's
  own RBAC, so an Owner-impersonated AI call still cannot delete/invite/manage members.
- `DomainControllerBase.RequireAiScope(scope)` → 403 Forbid when an AI OBO principal lacks
  the named `scope` claim (defense-in-depth; no-op for regular JWT callers).

| Endpoints returning 403 to AI OBO callers | |
|---|---|
| REST | `DELETE /api/workspaces/{id}`, `POST /api/workspaces/{id}/members`, `PATCH /api/workspaces/{id}/members/{userId}`, `DELETE /api/workspaces/{id}/members/{userId}`, `POST /api/workspaces/{id}/invites`, `DELETE /api/projects/{id}`, `DELETE /api/boards/{id}`, `DELETE /api/columns/{id}`, `DELETE /api/tasks/{id}`, `PATCH /api/tasks/bulk-status`, `DELETE /api/tasks/{id}/comments/{commentId}`, `DELETE /api/tasks/{id}/attachments/{attachmentId}`, `POST /api/invites/{token}/accept` |
| GraphQL | `deleteWorkspace`, `deleteProject`, `deleteTask` (plus admin/owner-only mutations throw `UnauthorizedAccessException`) |

Reading (`ReadWorkspace`), task creation (`CreateTask`), comments (`AddComment`) and
notifications (`CreateNotification`) are permitted via the same services any member uses.
## 2. Trigger.dev → .NET: webhook HMAC

`WebhookHmacMiddleware` (registered **before** `UseAuthentication`) guards
`POST /api/webhooks/trigger` — a request that reaches the controller was already verified.

| Item | Value |
|---|---|
| Header | `X-Trigger-Signature: sha256=<hex>` (HMAC-SHA256 of the raw body) |
| Secret config | `Webhook:Secret` → env `WEBHOOK_SECRET` |
| Verified | **before** auth middleware; body buffered + re-readable by the handler |
| 401 | missing header, malformed, or constant-time mismatch |
| 503 | secret not configured (misconfiguration = server error) |
| Success | controller returns **202 Accepted** |
| Auth on route | none (`[Authorize]` absent) — HMAC is the auth gate |

`WebhookController` is a thin relay (`POST /api/webhooks/trigger` → 202); payload routing
to domain services is a future-spec concern (spec 20+).

## 3. .NET → Trigger.dev: enqueue (enqueue-after-persist)

`TriggerDevClient` (typed `HttpClient`, `Griot.Infrastructure.Integrations`):

```
POST {Trigger:ApiUrl}/api/v1/tasks/{taskId}/trigger        # default https://api.trigger.dev
Authorization: Bearer {TRIGGER_SECRET_KEY}
```

| Item | Value |
|---|---|
| Config | `Trigger:SecretKey` → env `TRIGGER_SECRET_KEY`; `Trigger:ApiUrl` override |
| Pattern | **enqueue-after-persist** — call only after the domain write commits; on failure the write stands (AI is a post-processing adapter, not a transaction participant) |
| Failure handling | logs and returns `false` — never throws, never rolls back |

`TRIGGER_SECRET_KEY` is server-to-server only; web/mobile never receive any Trigger.dev
credential. The one exception to "backend-only triggering" is Trigger.dev's own cron
scheduler (`dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`).

## 4. Config summary (env)

| Env | Read by | Notes |
|---|---|---|
| `GRIOT_SERVICE_TOKEN` | `ServiceTokenHandler` (also `ServiceToken:Key`) | shared with ai/mcp env |
| `X-On-Behalf-Of` header | `ServiceTokenHandler` | real user Guid; sent by ai/mcp GraphQL/REST clients |
| `WEBHOOK_SECRET` | `WebhookHmacMiddleware` (also `Webhook:Secret`) | HMAC callbacks from Trigger.dev |
| `TRIGGER_SECRET_KEY` | `TriggerDevClient` (also `Trigger:SecretKey`) | backend → Trigger.dev REST; NEVER web/mobile |
| `Trigger:ApiUrl` | `TriggerDevClient` | optional self-host override |

## 5. Acceptance evidence (from spec 09)

- [x] Service token resolves to the restricted real-user OBO principal; deletes/invites rejected
- [x] Webhook HMAC verified; bad signatures 401
- [x] `TriggerDevClient` enqueues a task by ID with `TRIGGER_SECRET_KEY`; enqueue failure does not roll back the domain write
- [x] Web/mobile never receive any Trigger.dev credential (no `TRIGGER_SECRET_KEY` outside backend env)
- [ ] All AI tool calls are traceable to an ActivityLog row — **PLANNED**: writers arrive in backend spec 20; the surface is open (no new tables); callers log via `ILogger<T>` correlated to `X-Request-Id`.

## 5b. PLANNED evolution — the FIFTH scope `CreateReport` (backend spec 24, NOT implemented)

AI superpowers (user wave 2026-09-11, PLANNED): the report/audit capabilities (ai 06 knowledge+system auditor, ai 07 reports PDF+CSV, mcp 06 v2 tools, web 11 Reports & Audit Center) are carried by a **new, narrow, auditable FIFTH scope `CreateReport`** — a scoped capability added by backend spec 24, never a loosening of the existing grant. Today the OBO principal still issues exactly four scopes (ReadWorkspace/CreateTask/AddComment/CreateNotification); nothing in this file changes until spec 24 ships. Corollary of backend spec 23: **auth/OTP is human-only forever — service-token callers are 403 on all OTP/step-up/account routes, and `RequireStepUp` is never reachable by an AI principal.**

## 6. Verification and SQL fixture repair (2026-09-10)

The pre-push SQL run initially reported 64 passed and seven failed tests. All seven
failures came from `SqlAuthFixture.InitializeAsync`: the fixture stopped at
`AddOtpAndReports` but inserted a `User` through the current EF mapping, which includes
the later `EmailVerified` column. SQL Server rejected the insert before tests ran.

The fixture now inserts only historical columns using parameterized
`ExecuteSqlInterpolatedAsync`, then applies the remaining migrations. The migration
regression verifies refresh-token family preservation, the new email-verification
default, and the existing two-factor value. No production code or schema changed for
this repair.

Verification: focused regression **1 passed**; full suite **71 passed, 0 failed,
0 skipped**; build **0 warnings, 0 errors**; local API `/health` returned `Healthy`.
Shared SQL Server, PostgreSQL and Redis containers remained running.

Run from `backend/` (empty Brevo keys prevent outbound test email):

```bash
Brevo__ApiKey= BREVO_API_KEY= GRIOT_RUN_SQL_TESTS=1 dotnet test --no-restore --nologo -m:1
```

## 7. Pre-push output-file contention (2026-09-10)

The first push failed its build gate with `MSB4018` on `Griot.Api.deps.json`.
Rebuilds also reported `MSB3026` on `Griot.Application.dll`, including a
single-worker rebuild. A pre-existing `dotnet watch run` was rebuilding the same
outputs; no production defect or hook syntax change was involved.

With operator approval, temporarily pausing the existing watcher processes allowed
the unchanged hook to pass: build 0 warnings/errors, 71 SQL-enabled tests passed,
and contract sync passed. The watcher processes were automatically resumed after
the hook completed. For future clean builds, stop or pause only the relevant
development watcher, rerun every gate, and restore it afterward; do not skip hooks.
