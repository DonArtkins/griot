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
| `/api/auth/refresh` | `refreshToken` | 200, replacement pair and user | 400 missing/empty required field; 401 malformed, unknown, expired or reused token |
| `/api/auth/logout` | `refreshToken`; bearer access token required | 204, including malformed, unknown or already revoked tokens | 400 missing/empty required field; 401 absent/invalid bearer token |
| `/api/auth/otp/request` | `email`, `purpose` (`email_verify`/`login_2fa`/`password_reset`) | 202, code sent via Brevo branded email | 400 invalid purpose; 401 unknown email; 429 (3/15min/email; with `Retry-After`); 502 Brevo delivery failed |
| `/api/auth/otp/verify` | `email`, `code`, `purpose` | 200 `{verified:true,message,emailVerified}` (marks `Users.EmailVerified` for `email_verify`) | 400 invalid; 401 unknown/expired/invalid code; 429 lockout after 5 failed attempts |

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
- JWT: HS256, 15 minutes; claims `sub`, `email`, `jti`, plus issuer, audience
  and lifetime claims. Workspace context is selected per request and checked
  against membership; a global workspace claim is not issued.
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
| `JWT__Key` | HS256 key, at least 32 bytes; required |
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

The following is the PLANNED extension of the email-OTP contract (owner: `backend/project-kit/feature-specs/23-critical-action-otp-step-up.md`). It will move up into the implemented tables when spec 23 ships:

- `POST /api/auth/login` with `Users.TwoFactorMethod = EmailOtp` → **202** `{twoFactorRequired:true, purpose:"login_2fa"}` (no tokens) instead of 200.
- New OTP purposes: `delete_account`, `step_up` (the latter with an optional `action` param).
- `POST /api/auth/otp/verify` with `login_2fa` success → issues the token pair (single round-trip login); with `step_up`/`delete_account` success → `200 {verified:true, stepUpAccessToken}` — a 5-minute JWT carrying `stepup=true`, `purpose`, `action`, `challengeId`.
- New routes: `POST /api/auth/forgot-password` (uniform 202), `POST /api/auth/reset-password` (verify code → Argon2 re-hash → revoke **all** refresh families → 200), `DELETE /api/auth/account` (bearer + step-up → 204; sets `Users.DeletedAt`; revokes all refresh tokens and memberships).
- Guarded routes (spec 23 action allow-list) require the step-up claim via `RequireStepUp(action)` — 403 without it, 409 on mismatch.
- **AI OBO callers (spec 09) are 403 on the entire surface** — auth/OTP is human-only, permanently.

Brevo sends for all OTP purposes come from the single verified sender `Brevo:FromEmail` (2026-09-11: the `Brevo:Senders:<Key>` profile map was removed; see `docs/communication/COMMUNICATION-GUIDE.md` §2/§7b).

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
