# Authentication contract — Feature 07

Owner: `backend/project-kit/feature-specs/07-auth-jwt-argon2-redis.md`.
This is the implemented REST contract. Auth is implemented in REST only;
GraphQL uses the resulting bearer access token.

## Requests and responses

Local base URL: `http://localhost:5064`. The HTTPS launch profile also exposes
`https://localhost:7198`. The planned container API port is 8080.

| POST route | JSON body | Success | Failure |
|---|---|---|---|
| `/api/auth/register` | `email`, `displayName`, `password` (at least 8 characters) | 201, token pair and user | 400 validation; 409 duplicate email, including concurrent registration |
| `/api/auth/login` | `email`, `password` | 200, token pair and user | 400 validation; 401 invalid credentials; 429 with `Retry-After` |
| `/api/auth/refresh` | `refreshToken`, optional `organizationId` | 200, replacement pair and user; the active organization is re-resolved from `organizationId` (spec 30) | 400 missing/empty required field; 401 malformed, unknown, expired or reused token |
| `/api/auth/logout` | `refreshToken`; bearer access token required | 204, including malformed, unknown or already revoked tokens | 400 missing/empty required field; 401 absent/invalid bearer token |
| `/api/auth/otp/request` | `email`, `purpose` (`email_verify`/`login_2fa`/`password_reset`) | 202, code sent via Brevo branded email | 400 invalid purpose; 401 unknown email; 429 (3/15min/email; with `Retry-After`); 502 Brevo delivery failed |
| `/api/auth/otp/verify` | `email`, `code`, `purpose` | 200 `{verified:true,message,emailVerified}` (marks `Users.EmailVerified` for `email_verify`) | 400 invalid; 401 unknown/expired/invalid code; 429 lockout after 5 failed attempts |
| `/api/auth/organizations` (GET) | — (bearer access token required) | 200, active memberships `[{organizationId, organizationName, role, joinedAt, isActive}]` (spec 30: only Active memberships of Active organizations; `isActive` flags the session's active org) | 401 absent/invalid bearer token or unresolvable caller |
| `/api/auth/select-organization` | `organizationId`, `refreshToken` (required — the session being switched) | 200, new token pair carrying the new `org`/`role`/`perms` claims (spec 30); the presented token's family is revoked atomically | 400 validation (missing/invalid fields, incl. missing `refreshToken`); 401 unresolvable caller, malformed/unknown/expired/revoked/foreign refresh token; 403 not an Active member of an Active organization; 404 unknown organization |

Token response: `{accessToken, refreshToken, expiresAt, user}`;
`user` contains `{id, email, displayName, avatarUrl}`. `expiresAt` is the access
token expiry in UTC. No password hash, token hash or `FamilyId` is returned.
Registration succeeds only after the user and refresh token are persisted in
the configured SQL Server database. `200` with `Not implemented yet` is an old
scaffold response and must fail acceptance checks.

## Security and persistence

- Passwords use Argon2id (3 iterations, 65536 KiB, 4 lanes, 16-byte salt,
  32-byte output), stored as hexadecimal `salt:hash`. Unknown-account login
  verifies against a fixed valid dummy record at the same cost before rejecting.
- JWT (spec 30, **JWT v2**): HS256, 15 minutes; claims `sub`, `email`, `name`,
  `jti`, `org` (active OrganizationId — omitted for platform-only sessions),
  `role` (effective role: `super_admin`/`owner`/`admin`/`project_manager`/`member`/
  `client`/`custom:{roleId}`) and `perms` (space-separated permission keys from the
  fixed catalogue in `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §3 — stamped by
  `Griot.Application/Authorization/PermissionCatalogue` + `RoleSelection`), plus
  issuer, audience and lifetime claims. Issuance lives only in
  `Griot.Application/Services/TokenService.cs` (spec 30 claim builder). Workspace
  context is selected per request and checked against membership; a global workspace
  claim is not issued.
  - **Active-organization persistence (spec 30, stateless):** the client re-sends the active `organizationId` on every `/api/auth/refresh` (see `RefreshRequest.OrganizationId`); the session is re-resolved at issue time so role changes land within one refresh. Absent: a user with exactly one active membership gets it auto-picked, otherwise the platform view applies (`org` omitted) and the client re-selects via `POST /api/auth/select-organization`. The rotation family id is the replay-detection chain only — it never encodes tenant state (`RefreshTokens` has no org column by design).
  - **Org switch (`select-organization`):** the `refreshToken` of the session being
    switched is REQUIRED and validated FIRST (404 unknown org / 403 not Active /
    401 unknown-revoked-expired-foreign token), then a fresh pair is minted in a NEW
    rotation family seeded with the new org, and the presented session's family is
    revoked ATOMICALLY in the same SQL transaction (single active session per family; the stale-org token can never be refreshed again).
- Refresh tokens: 32 cryptographically random bytes represented by exactly
  64 hexadecimal characters, expiring 30 days after issuance/rotation.
  SQL stores SHA-256 of the decoded bytes, never the opaque token itself.
- Each register/login creates a new `FamilyId`; every replacement preserves
  it. Rotation conditionally revokes the old row using `RevokedAt IS NULL`
  and inserts the new row in one transaction. Only one concurrent caller
  receives a pair; an insertion failure rolls back the revocation.
- Reuse, including losing a rotation race, revokes active rows matching both
  `UserId` and `FamilyId`. Other login sessions survive. The winner's newly
  issued refresh token may therefore already be revoked after concurrent reuse.
  Existing JWT access tokens remain valid until their normal expiry.
- SQL exceptions are translated in `AuthRepository`: only SQL Server
  2601/2627 identifying `dbo.Users` and `IX_Users_Email` become
  `DuplicateEmailException` / HTTP 409. Unrelated persistence errors propagate.
- Redis login limit: 10 attempts / 900 seconds per IP, key
  `ratelimit:login:{ip}`. One singleton `IConnectionMultiplexer` serves all
  request scopes. SQL Server owns refresh-token persistence.
- Email-OTP 2FA (`research/ai-features-research.md` §1): codes are crypto-random 6-digit, HMAC-SHA256 hashed
  at rest with a server pepper, 10-minute expiry (`ExpiresAt`), `AttemptCount` lockout at 5. Request
  limit: 3 / 900 s per email via Redis (`ratelimit:otp:request:{email}`; returns 429 + `Retry-After`).
  `email_verify` sets `Users.EmailVerified = true`. OTP codes are never logged or returned in responses;
  the brand token + exact code only travel via Brevo (customized template per purpose).
- Brevo email: API key `Brevo:ApiKey` (fallbacks `BREVO_API_KEY`, `Brevo__ApiKey`); THE single sender
  address `Brevo:FromEmail` (fallback `BREVO_FROM_EMAIL`; 2026-09-11: the `Brevo:Senders:<Key>`
  profile map was removed — every email sends from this one dashboard-verified sender); sender
  name `Brevo:FromName` (fallback `BREVO_FROM_NAME`; default `Griot`); admin inbox
  `Brevo:ContactToEmail` (fallback `CONTACT_TO_EMAIL`;
  canonical fallback `info.donartkins.ke@gmail.com`
  used when both configuration keys are unset — notice is always delivered, never skipped). Register
  also sends a branded admin "new user" notice to the ops inbox.

## Configuration

| Setting | Meaning |
|---|---|
| `ConnectionStrings__Default` | SQL Server connection string; local host `localhost,14333`; database is the configured project database |
| `JWT__Key` | HS256 signing key. Required. Minimum 32 UTF-8 bytes (HS256 floor) everywhere; minimum **64 UTF-8 bytes (512 bits)** in production — enforced fail-fast at startup by `TokenService.ValidateKeyPolicy` (spec 30). CSPRNG-generated, platform secret store only, never committed |
| `JWT__Key_Previous` | Optional (spec 30 rotation): the previous signing key, accepted **during a rotation window only** so unexpired access tokens keep validating; every re-issue signs with `JWT__Key`, which retires the old key. Unset in steady state |
| `SUPERADMIN__Email` / `SUPERADMIN__Password` | Optional (spec 30 bootstrap): when set at startup, the matching user is created (Argon2id) or upgraded to `PlatformRole=SuperAdmin` idempotently and audit-logged (`Auth.SuperAdminBootstrap`); no-op and silent when unset |
| `JWT__Issuer` / `JWT__Audience` | Defaults `Griot` / `GriotClients` |
| `Redis__Connection` | StackExchange.Redis endpoint; default `localhost:6380`, compose `sababisha-redis:6379` |
| `Otp__Pepper` | HMAC-SHA256 pepper for OTP code hashes; dev default only in ignored `appsettings.Local.json` |
| `Brevo__ApiKey` (`BREVO_API_KEY`) | Brevo SMTP/API key; when unset OTP/email delivery returns 502 (otp/request) bzw. register continues (201) with the code persisted for later manual resend |
| `Brevo__FromEmail` (`BREVO_FROM_EMAIL`) | THE single Brevo-verified sender email (REQUIRED — Brevo only delivers from a sender you verified in the dashboard; 2026-09-11: the `Brevo:Senders:<Key>` profile map was removed, so this one sender carries every purpose; local config uses the dashboard-verified `info.donartkins.ke@gmail.com`) |
| `Brevo__FromName` (`BREVO_FROM_NAME`) | Sender display name; default `Griot` |
| `Brevo__ContactToEmail` (`CONTACT_TO_EMAIL`) | Admin inbox for new-user registration notices; canonical fallback `info.donartkins.ke@gmail.com` used when both keys are unset — notice is always delivered, never skipped |

Runtime reads the ignored `appsettings.Local.json`; environment variables and
command-line arguments override it. Secrets never belong in committed files.

## Client planning boundaries

Feature 07 currently accepts and returns refresh tokens in JSON (Postman and
mobile transport). Mobile persists them in platform secure storage. The web
plan requires a secure HttpOnly cookie transport and must not put these tokens
in localStorage. Cookie issuance/refresh support is a pending backend prerequisite
for web Feature 05, not implemented behavior. **Email-OTP 2FA is implemented** (Feature 07):
`POST /api/auth/otp/request` + `POST /api/auth/otp/verify`, purposes `email_verify` (auto-sent on
register + sets `Users.EmailVerified`), `login_2fa`, `password_reset`; branded Brevo template per purpose
(see `Griot.Application/Email/BrandedEmailTemplate.cs`), admin new-account notice → `Brevo:ContactToEmail`.
Service-token flows remain separate planned work (spec 09).

## PLANNED — critical-action OTP & step-up (backend spec 23, NOT yet implemented)

The following is the PLANNED extension of the email-OTP contract (owner: `backend/project-kit/feature-specs/23-critical-action-otp-step-up.md`; canonical policy: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §8–§9). It will move up into the implemented tables when spec 23 ships:

### PLANNED — Auth & account-deletion route table

| POST route | JSON body | Success | Failure |
|---|---|---|---|
| `/api/auth/login` (modified) | `{email, password}` | `TwoFactorMethod=EmailOtp` → **202** `{twoFactorRequired:true, purpose:"login_2fa"}` without tokens; else **200** token pair as today | 400 validation; 401 invalid credentials; 429 with `Retry-After` |
| `/api/auth/otp/request` (extended) | `{email, purpose, action?}` | **202** code sent via Brevo branded email. New purposes: `delete_account`, `step_up` (action = one of `Security:StepUpActions` allow-list keys: `Account.Delete`, `Account.ExportData`, `AccountDeletion.Decide`, `Workspace.Delete`, `Project.Delete`, `Organization.TransferOwnership`, `Organization.OffboardStart`, …) | 400 invalid purpose/missing action; 401 unknown email; 429 3/15min/email + `Retry-After`; 502 Brevo delivery failed |
| `/api/auth/otp/verify` (extended) | `{email, code, purpose}` | `login_2fa` success → **200** `{verified:true, accessToken, refreshToken, user}` (single round-trip login); `step_up`/`delete_account` success → **200** `{verified:true, stepUpAccessToken}` (5-min JWT, claims: `stepup=true, purpose, action, challengeId, org?`) | 400 invalid; 401 unknown/expired/invalid code (constant-time compare); 429 lockout after 5 failed attempts |
| `/api/auth/forgot-password` (new) | `{email}` | **202** fires `password_reset` OTP (ALWAYS 202 even for unknown email — NO user enumeration); OTP request rate-limited same as other purposes | 400 invalid email format; 429 rate-limited |
| `/api/auth/reset-password` (new) | `{email, code, newPassword}` | verify `password_reset` code → Argon2 re-hash → **revoke ALL refresh families** (every session everywhere) → **200** `{reset:true, allSessionsRevoked:true}` | 400 invalid/too-short password; 401 bad code; 429 lockout |
| **`DELETE /api/auth/account` (v1 — SUPERSEDED, REMOVED)** | — | Old behavior: bearer + step-up → 204 `Users.DeletedAt` set. **REMOVED from code on spec-23 branch. Returns HTTP 410 Gone.** Any caller must use the request workflow below instead. | 410 Gone (permanent redirect to new request workflow; response body: `{error: "instant_delete_superseded", message: "Use POST /api/auth/account/deletion-request instead.", docs: "/docs/multi-tenancy#9-account-deletion"}` |
| `/api/auth/account/deletion-request` (new) | `{reason (10–2000 chars), stepUpAccessToken}` | validates `delete_account` step-up claim + reason length → creates `AccountDeletionRequests(Requested)` → **202** `{requestId, status:"DeletionRequested", requestedAtUtc, scheduledPurgeAtUtc}` | 400 reason too short/long; 401 step-up invalid/expired; 409 open request already exists (partial unique index); 403 `account_deletion_pending` if already in pending state |
| `/api/auth/account/deletion-request/{id}/cancel` (new) | `{cancelReason? (optional, 0–2000)}` | requester cancels while `Status = Requested` (before SuperAdmin decides) → **200** `{cancelled:true, status:"Cancelled"}` | 403 not requester; 404 unknown requestId; 409 `{error:"cancel_window_closed", status:"Approved"}` (already decided — only SuperAdmin can cancel during PurgeScheduled) |
| `/api/admin/account-deletion-requests` (new — query params `{page, size, status?}`) | — (bearer SuperAdmin + pagination per spec 18) | **200** paginated list `{items:[{id, userId, userEmail, reason, status, requestedAtUtc, scheduledPurgeAtUtc, decidedBy?, decidedAt?, decisionNote?, exportAvailable?, cancelPastDeadline:bool}], total, page, size}` | 403 not SuperAdmin |
| `/api/admin/account-deletion-requests/{id}/approve` (new) | `{note? (optional, 0–2000), scheduledPurgeAtUtc? (optional override, must be ≥ MinPurgeWindowDays, ≤90d from approval)}` | SuperAdmin + `RequireStepUp("AccountDeletion.Decide")` → builds export bundle → fires blast-radius fan-out (§9e) → sets `ExportAvailable` → `PurgeScheduled` with §9b formula → **200** `{approved:true, exportAvailable:true, scheduledPurgeAtUtc}` | 403 SuperAdmin+step-up missing; 400 scheduledPurgeAtUtc outside [7,90] day window from approval; 409 already decided (Approved/Rejected/Cancelled/Purged) |
| `/api/admin/account-deletion-requests/{id}/reject` (new) | `{note (required, 10–2000 chars, reason for rejection communicated to user)}` | SuperAdmin + `RequireStepUp("AccountDeletion.Decide")` → sets `Status=Rejected`, fires user email+in-app notification ("Your account deletion request was reviewed and rejected: {note}"), 7-day cooling-off before new request → **200** `{rejected:true}` | 403 SuperAdmin+step-up missing; 400 note too short/too long; 409 already decided |
| `/api/admin/account-deletion-requests/{id}/lift-readonly` (new) | `{note (required, reason for lifting read-mostly)}` | SuperAdmin + step-up → temporarily disables §9f `account_deletion_pending` 403 so user can make changes while request is Pending → **200** | 403; 400 note; 404; 409 not in Pending state |
| `/api/admin/account-deletion-requests/{id}/cancel-purge` (new) | `{note (required, reason for cancelling scheduled purge)}` | SuperAdmin + step-up, while `Status=PurgeScheduled` AND BEFORE `ScheduledPurgeAtUtc` → sets back to None, fires user notification "Your account deletion has been cancelled by a platform administrator.", kills background purge job → **200** `{purgeCancelled:true, accountReactivated:true}` | 403; 409 past purge date or not PurgeScheduled; 400 note |
| `/api/auth/account/export` (new) | — (bearer + `RequireStepUp("Account.ExportData")` via step-up OTP) | streams zip bundle (§9d manifest) as `application/octet-stream`; filename `griot-user-export-{userId}-{utcNow}.zip`; valid only while `Status ∈ {ExportAvailable, PurgeScheduled}` | 403 no step-up claim; 404 `{error:"export_not_available", currentStatus}` if not in right state or bundle not built yet (retry-After header if building in progress) |

### PLANNED — Step-up guarded action list (v1 default allow-list, configurable via `Security:StepUpActions`)

`Workspace.Delete` · `Workspace.TransferOwnership` · `Member.RoleChange` · `Member.Remove` · `Invite.Create` · `Project.Delete` · `Board.Delete` · `Column.Delete` · `Task.BulkStatus` · `Task.Delete` · `Comment.Delete` · `Attachment.Delete` · `Account.Delete` · `Account.ExportData` · `AccountDeletion.Decide` · `Organization.TransferOwnership` · `Organization.OffboardStart`

Each maps to its existing handler plus a `RequireStepUp(action)` guard: validates the 5-minute `stepup` JWT claim `action` against the allow-list → **403** without claim; **409** `{error:"step_up_action_mismatch", required:action, received:claim.action}` on mismatch. Claim also binds `org`: a step-up issued while active org=A cannot authorize an action on org=B. **AI OBO callers (spec 09) are permanently 403 on this entire surface.** `RejectAiOnAuthSurface()` runs BEFORE the rate limiter on every route above — xUnit asserts 403 per route; GraphQL mirror of every guarded mutation rejects with same rules.

### PLANNED — Destructive-op intent/confirm/cancel pattern (all systems, §10a–10f)

All destructive REST/GraphQL delete endpoints gain intent/confirm/cancel variants (§10 two-phase protocol with full rollback, §10c cancel-disable-at-countdown=1, §10d audit+trigger matrix). Concrete pattern:

| REST route pattern (per entity {e.g. workspaces, projects, tasks, …}) | Body | Success | Failure |
|---|---|---|---|
| `POST /api/{entity}/{id}/delete-intent` | `{stepUpAccessToken?}` (step-up required for guarded actions per allow-list) | Validates auth/membership/step-up, computes blast-radius, writes intent audit row → **200** `{intentId, expiresAtUtc: now+10min, confirmAfterUtc: now+10s, blastRadius:{workspaces:N, projects:M, tasks:K, comments:J, attachments:L, affectedUsers:X}}`. Zero domain change. | 401/403 auth/step-up; 409 `{error:"entity_already_deleting"}` if another active intent exists on this id |
| `POST /api/{entity}/{id}/delete-confirm` | `{intentId, stepUpAccessToken?}` | Re-validates everything, marks intent as `Committing`, opens SINGLE transaction → deletes + app-audit + activity → COMMIT → **200/204**. Any throw inside → **FULL ROLLBACK** to zero state. | 409 `{error:"confirm_too_early", retryAtUtc}` if sent before `confirmAfterUtc`; 409 `{error:"intent_cancelled"}`; 409 `{error:"intent_expired"}`; 409 `{error:"already_committing"}` → cancel now 409 too; 500 `{error:"delete_failed", traceId, requestId, retryable}` rollback happened |
| `POST /api/{entity}/{id}/delete-cancel` | `{intentId, cancelReason?}` | Validates intent not expired/not committing, writes cancel audit row → **200** `{cancelled:true}`. | 409 `{error:"already_committing"}` if confirm transaction is open (too late); 404 intent unknown |

GraphQL mirror: `delete{Entity}Intent(input:{id, stepUpAccessToken?}) → {intentId, confirmAfterUtc, blastRadius}` · `delete{Entity}Confirm(input:{intentId, stepUpAccessToken?}) → {success}` · `delete{Entity}Cancel(input:{intentId, cancelReason?}) → {cancelled:true}`. Same server semantics, same rollback, same cancel-disable policy.

**Client enforcement (§10e):** Every destructive button opens the standard `DestructiveConfirmDialog` (web) / native equivalent (mobile). Countdown in button label `[Confirm Delete in 10… 9… 8…]`. **Cancel button PERMANENTLY DISABLED when countdown display shows `1` (one) second remaining.** Confirm button disabled (grayed) until countdown=0 → becomes `Confirm Delete`. No optimistic delete: UI never removes the row before server 2xx/204. qa spec 14 has 4 race-condition tests asserting correct 409 behavior.

Brevo sends for all OTP purposes come from the single verified sender `Brevo:FromEmail` (2026-09-11: the `Brevo:Senders:<Key>` profile map was removed; see `docs/communication/COMMUNICATION-GUIDE.md` §2/§7b).

## Implemented — JWT v2: role + tenant claims (multi-tenant wave, backend spec 30)

Owner: `backend/project-kit/feature-specs/30-auth-jwt-v2-role-tenant-claims.md`; canonical rationale `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §4. **Implemented on `feature/backend/30-auth-jwt-v2` (2026-09-11)** — the extension of the implemented Feature-07 contract above:

- **Access token carries claims** (existing `sub`, `email`, `jti`, `iss=Griot`, `aud=GriotClients`, 15-min HS256 unchanged):
  - `name` — `Users.DisplayName`
  - `org` — the active `OrganizationId` (multi-tenant; absent for platform-only sessions)
  - `role` — effective role in the active org (`super_admin`, `owner`, `admin`, `project_manager`, `member`, `client`, or `custom:{roleId}`); platform role resolves before any org role
  - `perms` — space-separated permission keys for the effective role (system-role map in `Griot.Application/Authorization/PermissionCatalogue`; custom roles stamp from the persisted `Roles.Permissions` column; `log.read_tier` is reserved and never stamped — spec 25/31)
- **`GET /api/auth/organizations`** → 200, caller's Active memberships of Active organizations. **`POST /api/auth/select-organization`** `{organizationId, refreshToken}` (`refreshToken` required — CodeRabbit CWE-613 fix: the presented token is validated FIRST — unknown/revoked/expired/foreign → 401 — so a stolen access token alone can never create a renewable session; the presented family + the new pair are then swapped in ONE atomic transaction) → 200 new token pair with the new `org` claim; 403 not an active member (SuperAdmin may select any Active company); 404 unknown organization. Claims re-derive on refresh (role changes take effect within one refresh); the client re-sends the active `organizationId` on every refresh so the selection persists across rotation (absent: a single active membership is auto-picked, otherwise the platform view applies and the client re-selects).
- **Refresh tokens remain opaque** (64-hex, SHA-256 at rest, rotation + family revoke — unchanged). They are **not JWTs and must never encode user data**; jwt.io decoding them blank is correct behavior.
- **Signing key policy:** production `JWT__Key` ≥ 64 chars (512 bits), CSPRNG-generated, platform secret store only, never committed; enforced fail-fast at startup — **every configured signing key, including `JWT__Key_Previous`, is validated against the applicable environment minimum and an invalid value aborts startup**. Verification at jwt.io succeeds **only with the real configured key** (jwt.io's `a-string-secret-at-least-256-bits-long` string is a placeholder). **Key-rotation runbook (dual-key validation window):** set the new `JWT__Key` and move the old key to `JWT__Key_Previous` in the same deploy — old access tokens keep validating until their ≤15-min expiry while every re-issue signs with the new key; **retain `JWT__Key_Previous` for at least one full access-token TTL (15 min) after the last token signed with it, plus the configured clock-skew margin, then remove it**. No re-issue storm, no forced logouts.
- **SuperAdmin bootstrap:** `SUPERADMIN__Email` (+ optional `SUPERADMIN__Password` first-boot) upgraded/created idempotently on startup, audit-logged; SuperAdmin tokens carry `role=super_admin` and may omit `org` (platform-wide authority; selecting a company is allowed without a member row).

## Verification

From `backend/`, run `dotnet build --no-incremental` and
`GRIOT_RUN_SQL_TESTS=1 dotnet test`. SQL tests create and remove a uniquely named
`GriotAuthTests_*` database on the configured SQL Server; they do not clear the
Griot database. `GRIOT_TEST_SQL_CONNECTION` can override the test server setting.
Without the opt-in flag, SQL tests are explicitly skipped, never reported as
integration passes. Redis must also be reachable for HTTP login tests.

The schema amendment is [RefreshTokens](../../diagrams/erd/auth-family-amendment.md).
Auth implementation source: `backend/src/Griot.Api/Controllers/AuthController.cs`,
`backend/src/Griot.Application/Services/AuthService.cs`,
`backend/src/Griot.Infrastructure/Repositories/AuthRepository.cs`.
Unit tests: `backend/tests/Griot.Tests/Auth/AuthServiceTests.cs` (18 tests passing).
