# AI Service Token and Webhook Contract — Feature 09 [own-stack]

**Status:** backend boundary implemented and review-hardened on `feature/backend/09-ai-service-token-and-webhooks`, 2026-09-11. AI/MCP clients, durable callback dispatch and persistent log writers remain planned. Owner: backend 09; orchestration authority: `research/ai-integration.md` §2a.

## 1. AI data plane: identity, grant and membership

```http
Authorization: Bearer {GRIOT_SERVICE_TOKEN}
X-On-Behalf-Of: {real User.Id}
```

The opaque bearer identifies the configured service. The header selects a real user only when the backend has an active delegation for that service/user. Users-table existence alone does not authorize impersonation.

Operator-controlled configuration (never model/tool arguments):

```text
ServiceToken:Delegations:{userId}:ExpiresAtUtc = <future ISO-8601 UTC timestamp>
ServiceToken:Delegations:{userId}:WorkspaceIds:0 = <permitted workspace Guid>
ServiceToken:Delegations:{userId}:Scopes:0 = ReadWorkspace
ServiceToken:Delegations:{userId}:Scopes:1 = CreateTask
ServiceToken:Delegations:{userId}:Scopes:2 = AddComment
ServiceToken:Delegations:{userId}:Scopes:3 = CreateNotification
```

Environment equivalents use double underscores, for example `ServiceToken__Delegations__<userId>__WorkspaceIds__0`. This is a server-side allowlist bound to the one configured service identity. Granting access is an operator action; no grant is provisioned automatically. Empty, malformed, expired or unknown-scope grants fail authentication. Grants can narrow the four-scope vocabulary; they cannot introduce a fifth scope. Removing/changing configuration affects subsequent authentications. Normal user JWTs still carry no global workspace claim.

The principal retains the real user's ID/name/email, role `ai-on-behalf-of`, `obo`, `auth_method=service_token_obo`, the granted `scope` claims and `ai_workspace` claims. Every resource operation also verifies current workspace ownership/membership. The allowed workspace list cannot create membership. The service does not gain a user's human-only powers.

AI jobs obtain user/workspace from the stored backend request or authorized schedule. MCP stdio uses a single operator-bound local profile; HTTPS Streamable HTTP binds an authenticated per-user session server-side. A shared MCP transport key alone is not identity. Client/model-supplied identity overrides fail before dispatch; mcp 03 owns both cross-user transport tests.

## 2. Scheme routing and configuration

| Setting | Resolution |
|---|---|
| Service bearer | first nonblank ServiceToken:Key, then GRIOT_SERVICE_TOKEN |
| Callback key | first nonblank Webhook:Secret, then WEBHOOK_SECRET |
| Trigger key | first nonblank Trigger:SecretKey, then TRIGGER_SECRET_KEY |
| Trigger API | nonblank Trigger:ApiUrl, otherwise https://api.trigger.dev |

MultiAuth first compares the bearer with the resolved configured service token using constant-time equality. A matching dotted token stays on ServiceToken. Only a nonmatching bearer with two dots is routed to JwtBearer; other bearers route to ServiceToken. Shape chooses a validator, never establishes identity. Missing/non-Bearer headers default to the JWT challenge. Blank primary settings allow environment fallback.

Missing/invalid bearer, missing/malformed OBO identity, missing/expired grant or unknown user cannot authenticate protected resources (401). No credentials or grant values are logged. Service token comparison is opaque constant-time equality, not HS256/JWT signature verification.

## 3. Default-deny operations

Global MVC AiAccessFilter rejects an unmapped operation or missing scope before model binding/domain lookup. Workspace reads require ReadWorkspace plus the grant and membership; returned workspace lists are grant-filtered. CreateTask and AddComment are the existing allowed write routes. CreateNotification is a reserved scope with no generic public OBO create endpoint yet; backend 22 must implement the route before exposing a tool. Unscoped personal notifications/profile reads are not currently in the AI allowlist.

REST AI denials return 403: auth register/login/refresh/logout/OTP request/verify, all updates/moves/deletes, workspace/project/board/column creation, invites, member management, attachments writes, notification read-state writes, bulk status and raw logs. A granted operation aimed at another workspace returns a scoped 404. Existing human behavior is preserved; platform-tier log restrictions are planned in backend 25.

GraphQL AiFieldMiddleware applies a default-deny root-field allowlist and scope check; resolvers also enforce scope/workspace grant and current membership. createTask/CreateTask and addComment/AddComment are allowed. deleteWorkspace/deleteProject/deleteTask and other unlisted mutations are denied before lookup. Profile/2FA fields are not AI-readable, including nested twoFactorMethod. For Accept: application/json, execution denials return HTTP 200 with GraphQL errors (FORBIDDEN from the field gate). Clients must inspect errors rather than HTTP success alone. Contract tests cover the three delete mutations and unlisted workspace creation; no HTTP 403 serializer is claimed.

CreateReport remains a planned fifth vocabulary entry in backend 24. It must be added to grant validation, operation allowlists and capability resolution together; never grant it to all agents implicitly. Backend 26 memory writes and backend 27 notices do not borrow notification/report scopes.

## 4. Trigger callbacks: bounded authentication, retryable stub

POST /api/webhooks/trigger uses X-Trigger-Signature: sha256=<hex>, HMAC-SHA256 of exact raw bytes. WebhookHmacMiddleware runs before authentication. It sets IHttpMaxRequestBodySizeFeature before buffering where writable and independently reads at most 64 KiB + one sentinel byte. Both declared-length and chunked/unknown-length oversize bodies return 413. The accepted body is rewound without text decoding/re-encoding.

| Condition | Status |
|---|---|
| Missing/malformed/bad signature | 401 |
| Unconfigured callback key | 503 |
| Body larger than 64 KiB | 413 |
| Valid signed callback while dispatch is absent | 503, retryable; no accepted work or side effect |

There is no configurable Webhook:MaxBodyBytes setting. There is no current 202 success acknowledgment or replay-sensitive callback side effect. Before enabling 202, backend 20 must persist a durable inbox/job intent and implement signed timestamp freshness, event-ID/hash dedupe and binding to an existing job/user/workspace/purpose. This is a release gate, not implemented evidence.

## 5. Backend enqueue adapter and durable recovery gate

TriggerDevClient sends POST {Trigger:ApiUrl}/api/v1/tasks/{taskId}/trigger with Bearer TRIGGER_SECRET_KEY and JSON `{ "payload": ... }`. Task IDs are URL-escaped. The default is Trigger cloud. Overrides must be valid absolute HTTPS URLs without user-info, query or fragment. Invalid configuration is rejected before attaching credentials. The registered HttpClientHandler disables redirects; 3xx is returned as failure and no redirected host receives the credential. Responses are disposed.

The adapter returns true for 2xx and false for configuration/network/API failure. It has no production call sites today and does not persist recovery state. Backend 20 must commit a job/outbox row with the requesting domain operation before a worker invokes it; failure leaves durable retry/reconciliation state without rolling back committed work. Do not wire fire-and-forget production callers before that gate.

## 6. Review evidence — 2026-09-11

108 tests passed, zero failed/skipped with GRIOT_RUN_SQL_TESTS=1 during this review; build passed without warnings/errors. Tests cover grant binding/expiry/cross-user/privileged scopes, exact four-scope grant, default-deny REST/auth and GraphQL, workspace-list filtering, raw-log denial, callback byte limits/retryable replay, blank configuration fallback, HTTPS validation and Trigger payload shape/failure.

GitGuardian incident 37156152 points to the old selector fixture in commit 0436e87. Local decoding showed a synthetic HS256 header, test subject and the literal non-cryptographic signature bytes `signature`. It was not a working service/user token and needs no credential rotation. JWT-shaped fixtures now use runtime-random segments. No live JWT was printed or committed. The historical occurrence remains until GitGuardian classifies/rescans it; no suppression or history rewrite was performed.

API references checked using Context7 ctx7 0.5.9 and installed ASP.NET Core/HotChocolate package documentation: [Microsoft request limits](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.features.ihttpmaxrequestbodysizefeature.maxrequestbodysize?view=aspnetcore-8.0), [HotChocolate field middleware](https://chillicream.com/docs/hotchocolate/resolvers/field-middleware), [Trigger task REST envelope](https://trigger.dev/docs/management/tasks/trigger).

## Historical verification notes

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
