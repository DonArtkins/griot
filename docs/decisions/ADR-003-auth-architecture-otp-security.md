# ADR-003 — Griot Auth Architecture: JWT + Argon2 + Rotating Refresh Tokens + Family Revoke + Redis Rate Limiting + Brevo OTP 2FA [own-stack]

**Status:** Accepted  
**Date:** 2026-09-08  
**Deciders:** Don Artkins (Griot)  
**Owning system:** Backend / API (Spec 07 — Auth)  
**Research reference:** `research/ai-features-research.md §1 (OTP 2FA)`  
**Contract:** `docs/api/auth-contract.md`

---

## 1. Context

### 1.1 Problem statement

The GTP 2026 Bootcamp contract (Week 2, Backend/API) specifies a project-management web app with workspaces, projects, boards, tasks, comments, notifications, dashboard, invites, attachments, and webhooks. The bootcamp PDF/week-02 document is **silent on authentication** — it does not prescribe OAuth, auth providers, sessions, JWT, password hashing algorithms, token lifetimes, 2FA, or password-reset flows. This silence creates a security vacuum:

- **No auth → CWE-862 (Missing Authorization):** Any caller can perform CRUD on any entity across workspace boundaries (data exfiltration, ransom, corruption).
- **Bcrypt-only hashing + opaque sessions →** inadequate for a bootcamp-deliverable production surface:
  - Bcrypt (max 72 bytes, truncates beyond) is inferior to Argon2id (winner of PHCH 2015, memory-hard, configurable cost).
  - Server-sticky cookie sessions break behind multiple backends / Vercel serverless.
  - No refresh rotation → single token theft = indefinite impersonation.
  - No rate limiting → credential stuffing with common password lists (CWE-799).
  - No 2FA → any leaked password = full account takeover (CWE-308).

### 1.2 Constraints

1. **`[own-stack]` marker required:** Per AGENTS.md rule 3, any deviation from "the PDF stack is never substituted silently" must carry `[own-stack]` and a written rationale. Since the PDF specifies NO auth stack, the ENTIRE auth system is self-owned `[own-stack]`. Rationale: no auth would violate every OWASP Top 10 A01/A02/A07 and the bootcamp's own Week 6 QE deliverable.
2. **Cross-system contract sync (hard gate 0):** Every route, status code, JWT claim shape, env var, entity, enum, auth header, and rate-limit response must be reflected in: feature spec 07, `docs/api/auth-contract.md`, backend `api-surface.md`, root+backend/web `AGENTS.md` "Implemented authentication contract" paragraphs, Web spec 03/05, Mobile spec 02, QA spec 05/06, progress trackers, DEPENDENCY-AUDIT, GRAPHQL-STATUS (authenticated GraphQL uses the bearer).
3. **AI never writes to SQL (hard rule 2):** The AI principal (Spec 09 `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of`) reads/writes ONLY through the .NET API — the auth stack must support a service-token claim mode WITHOUT a password login path. **Status (2026-09-11): implemented as a real-user On-Behalf-Of (OBO) principal** (`ServiceTokenHandler`, role `ai-on-behalf-of`, 4 scope claims) — NOT the reserved `sub=ai-agent:trigger-dev` shape below, which was superseded (no virtual AI member exists).
4. **Existing compose services (hard rule 1, separation physical):** Only Redis and SQL Server are already in the compose file. No new containers for auth (no Keycloak, no IdentityServer) — deploy surface is kept minimal for Railway/Render single-container prod target.
5. **Dev UX zero-config local:** `dotnet run` from `backend/` must work for local dev with ONLY the git-ignored `appsettings.Local.json` for ConnectionStrings+JWT+Pepper; Redis auto-falls back to `localhost:6380` if config missing; Brevo requires a verified sender email (`Brevo:FromEmail`) but the app still starts and register still returns 201 when the key is missing; no hardcoded secrets.
6. **Future-proof TOTP + Passkey:** TwoFactorMethod enum (None/EmailOtp/Totp) is on User; current implementation only acts on EmailOtp. ADR decision: switch over `user.TwoFactorMethod` in `LoginAsync` response path, where `EmailOtp` returns a short-lived step-up session, and future `Totp` calls the same verify-path with a different HMAC key.
7. **Postman deliverable (Spec 08) depends on this working:** The Postman collection must exercise register→login→refresh→reuse→logout→otp/request→otp/verify end-to-end with real HTTP status codes and body shapes, not scaffold stubs.

### 1.3 Non-goals (Explicitly out of scope for ADR-003)

- OAuth / Social SSO (Google, GitHub, Microsoft) — v2.
- Passkey / WebAuthn — v2 (TwoFactorMethod enum reserved).
- Session revocation admin UI — v2 (API already supports family revoke; UI = Web spec).
- AI service token principal — Spec 09 (`GRIOT_SERVICE_TOKEN`).
- Password-reset email flow scaffolding (purpose `password_reset` exists in OTP, but the actual email with reset link sent to user = v1.1 polish).
- SPF/DKIM/DMARC DNS records for a Griot custom email sending domain (`research/ai-features-research.md §1.8` ops item; separate).
- Bounce / complaint webhooks for Brevo (§1.9 ops item; webhook controller scaffold → 501 currently).

---

## 2. Decision

The `[own-stack]` authentication system for Griot consists of **seven cooperating components**, all deployed in the single .NET 8 backend container:

```
┌───────────────────────────────────────────────────────────────────────────────┐
│                             Griot HTTP Pipeline                                │
│                                                                               │
│  Client (Postman / Web / Mobile)                                              │
│       │                                                                       │
│       ▼                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐             │
│  │  Controllers / GraphQL (Feature 04 REST + Feature 05 GQL)    │             │
│  │  AuthController + [Authorize] on every other controller      │             │
│  └───────────────┬──────────────────────────────────────────────┘             │
│                  │ bearer extraction, CORS, rate-limit, JWT validation       │
│                  ▼                                                            │
│  ┌──────────────────────────────────────────────────────────────┐             │
│  │  Program.cs middleware pipeline                              │             │
│  │  Cors → RedisRateLimiter → Authentication(JwtBearer)        │             │
│  │    → Authorization → Controllers → GraphQL → Health         │             │
│  └───────┬──────────────────────┬───────────────────────────────┘             │
│          │ rate-limit lookups   │ JWT signing + OTP pepper                    │
│          ▼                      ▼                                             │
│  ┌───────────────────┐   ┌──────────────────────────────┐                     │
│  │ StackExchange.Redis│   │ Application Layer            │                     │
│  │ (Singleton)        │   │   AuthService (business)     │                     │
│  │ 6380 compose       │   │   • Argon2 VerifyPassword    │                     │
│  │                    │   │   • JwtSecurityTokenHandler  │                     │
│  │ Sliding window     │   │   • RefreshToken rotation    │                     │
│  │ Lua script (60s)   │   │   • OTP 6-digit HMAC-pepper  │                     │
│  │                    │   │   • Email render (branded)   │                     │
│  │ Keys:                                              │                     │
│  │   login_limit:{ip}                                  │   └──────────┬──────────┘                     │
│  │   otp_req_limit:{email}                           │              │ persistence + email                     │
│  └───────────────────┘                               │              ▼                                        │
│                                                ┌──────────────────────────────┐                    │
│                                                │ Infrastructure Layer         │                    │
│                                                │  • AuthRepository (EF Core)  │                    │
│                                                │    - atomic rotation tx      │                    │
│                                                │    - conditional ExecuteUpdate│                    │
│                                                │    - FamilyId scoped revoke  │                    │
│                                                │                              │                    │
│                                                │  • BrevoEmailService (HTTP) │                    │
│                                                │    POST api.brevo.com/v3/smtp/email│                    │
│                                                │    - never throws, logs 429  │                    │
│                                                └──────┬────────────────┬───────┘                    │
│                                                       │ SQL Server     │ Brevo SaaS               │
│                                                       ▼                ▼                          │
│                                              ┌──────────────┐   ┌──────────────┐                    │
│                                              │ Griot DB     │   │ api.brevo.com│                    │
│                                              │ • Users      │   │              │                    │
│                                              │ • RefreshTkns│   │ SMTP relay + │                    │
│                                              │ • OtpChallenges│ │ deliverability │                    │
│                                              │ • Reports    │   │              │                    │
│                                              └──────────────┘   └──────────────┘                    │
└───────────────────────────────────────────────────────────────────────────────┘
```

Component-by-component decision list (binary, testable):

| # | Component | Decision |
|---|---|---|
| D1 | Password hashing | **Argon2id** — t=3 iterations, m=64 MiB memory cost, p=4 parallelism, 16 B crypt-salt, 32 B hash. Stored as `hexSalt:hexHash` in `Users.PasswordHash` (UTF-8 `:` separator, length 96 chars fixed). NOT bcrypt (72 B truncation, not memory-hard). NOT PBKDF2 (CPU-only, GPU-vulnerable). |
| D2 | Access token | **JWT (JWS), HS256 symmetric** — 15-minute lifetime. Claims: `sub = Guid UserId.ToString("D")`, `email = user.Email`, `jti = crypto Guid` (opaque id; if future logout-by-jti is needed, Redis SET with TTL 15m; currently unused — jti present for audit). `iss = Configuration["JWT:Issuer"]`, `aud = Configuration["JWT:Audience"]`. Issuer signing key `JWT:Key` HS256 ≥ 32 bytes (256-bit); UTF-8 byte length ≥32 validated at startup in Program.cs JwtBearer setup AND in AuthService.IssueJwt before constructing SymmetricSecurityKey — InvalidOperationException aborts launch for missing/short keys. Refresh NOT embedded in access (small token, no DB on every request). No RS256 asymmetric — single backend service; key rotation via env redeploy. |
| D3 | Refresh token | **Opaque 32 random bytes → 64 hex chars client-visible**, **hashed at rest in DB with SHA-256 → 64 hex**. RefreshToken row: `Id (Guid PK), UserId (FK), TokenHash (SHA256-hex 64 UNIQUE, NOT NULL), FamilyId (Guid, NON-NULL index), CreatedAt, ExpiresAt (30 days from creation), RevokedAt (nullable), RevocationReason (nullable enum: explicit_logout / reuse / family_reuse / rotation_race / admin), ReplacedByTokenId (nullable FK self)`. Rotation = EVERY `/refresh` call revokes old + inserts new → attack window of refresh theft = single use only, NOT 30 days. |
| D4 | Reuse detection + scope | **Family-scoped revocation (NOT user-wide).** Each new refresh row (first login, first register) gets a new `FamilyId`. Rotation `ReplacedByTokenId` chains preserve the same `FamilyId`. When token reuse is detected (§2, conditional `ExecuteUpdateAsync` WHERE `RevokedAt IS NULL` → 0 rows affected), revoke ALL rows with same `(UserId, FamilyId, RevokedAt IS NULL)`. Independent login families (e.g. browser + mobile app) have different FamilyIds → browser token reuse **does not** log out the mobile session. Rationale: user-wide revocation punishes honest second sessions for a single compromised one; family scoping minimizes blast radius. Migration backfills legacy rows: each pre-existing refresh row gets a fresh `FamilyId` (independent families post-migration). |
| D5 | Atomic rotation + concurrent refresh race | **Provider-default transaction (parameterless BeginTransactionAsync) + conditional UPDATE+INSERT order.** `RotateRefreshTokenAsync`: (1) open `BeginTransactionAsync()` (SQL Server provider-default isolation, Read Committed); (2) `ExecuteUpdateAsync` WHERE `Id == toRevoke.Id && UserId == toRevoke.UserId && FamilyId == toRevoke.FamilyId && RevokedAt == null` SET `RevokedAt = now, ReplacedByTokenId = newTokenId, RevocationReason = "rotated"`; (3) if `affected != 1` → `Rollback` + `RevokeFamilyAsync(user, family)` + `return null` (reuse detected); (4) else INSERT new RefreshToken row with same FamilyId + new SHA256 TokenHash + ExpiresAt=now+30d + SaveChanges; (5) Commit. `ExecuteUpdateAsync` returns affected rows for concurrency handling. Winner = exactly 1 concurrent caller sees `affected == 1`; losers = 0 → rollback + family revoke. Two concurrent HTTP callers carrying the SAME refresh token cannot both succeed → the very act of racing = reuse evidence → family is revoked (defensive, but cannot tell which caller is legitimate without mutual TLS). |
| D6 | Malformed token defense (garbage in / no DB touch + no 500) | **`HashToken(string? token)` guard function BEFORE `Convert.FromHexString`:** `if (string.IsNullOrEmpty(token) || token.Length != RefreshTokenBytes * 2 == 64) return null;` + `if (!token.All(Uri.IsHexDigit)) return null;` Returns null → `/refresh` returns **401 Unauthorized** (never 500). `/logout` idempotent → null hash returns **204 No Content immediately** (no DB hit for garbage tokens; no info leak whether a token existed). |
| D7 | Timing oracle prevention (unknown user enumeration via wall-clock) | **Static `DUMMY_PASSWORD_HASH` valid Argon2id record at same cost (t=3/m=64MB/p=4).** `LoginAsync`: always calls `VerifyPassword(user?.PasswordHash ?? DUMMY_PASSWORD_HASH, password)` with **identical Argon2 cost** BEFORE checking `if (user == null)`. Wall-clock time for "unknown email + any password" equals "known email + wrong password". Tests: `AuthServiceTests.Login_UnknownUser_RejectsEvenWhenDummyPasswordMatches` (2 Theory cases). |
| D8 | Duplicate-email race + 409 mapping precision | **Two-layer defense.** (1) Pre-check `UserExistsByEmailAsync` in `RegisterAsync` for fast-path 409. (2) Post-Save `CreateUserAsync` wraps `SaveChangesAsync` in try/catch `DbUpdateException` → unwrap `InnerException as SqlException` → only if `Number is 2601 (duplicate key row) or 2627 (unique constraint violation)` AND `.Message` contains both `IX_Users_Email` AND `dbo.Users` → throw `DuplicateEmailException` (409 at controller). Any other UNIQUE violation (e.g. `RefreshTokens.TokenHash` collision, Workspace member UNIQUE FK) rethrows as `DbUpdateException` (generic 500 / middleware) — never mislabeled as duplicate email. Tests: `AuthServiceTests.Register_DuplicateEmail_ThrowsDuplicateEmailException`. |
| D9 | Rate limiting (prevent credential stuffing / OTP spam) | **Redis-backed sliding window via Lua script** (`backend/src/Griot.Infrastructure/Redis/RedisRateLimiter.cs`). Lua atomically: KEYS[1] = `login_limit:{ip}` or `otp_req_limit:{email}`; ARGV[1]=window_sec (900s = 15 min); ARGV[2]=max_count (10 for login / 3 for OTP). Removes expired entries, adds current timestamp, checks count, returns `{allowed:0|1, retryAfterSec:int, remaining:int}`. Controller returns HTTP 429 with `Retry-After: <seconds>` header + body `{message:"Too many requests", retryAfterSec:int}`. Rate limiter DI scope = scoped (per request); underlying `IConnectionMultiplexer` = **singleton** (stackexchange-redis shared connection; scoped disposes it after first request → all subsequent calls fail). Tests: `RedisRateLimiterTests` (2). Scope NOT configurable by caller; hard limits per purpose to match password-security NIST SP 800-63B §5.2.2 (online guessing ≤100/hour; 10/15min is ~40/hour conservative). |
| D10 | Email OTP 2FA architecture (research §1) | Three purposes: `email_verify`, `login_2fa`, `password_reset`. (1) Challenge creation `CreateOtpChallengeAndEmailAsync`: 6-digit code via `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")` (NOT modulo-biased `Next`). Code hashed at rest with HMACSHA256(server `Otp:Pepper`, UTF8(code)) → `OtpChallenge.CodeHash` (NOT plain; pepper rotation on server wipes all stored codes = all active 2FA challenges expire; acceptable tradeoff 10-min window). `ExpiresAt = now + 10 min`, `AttemptCount = 0`, `Consumed = false`, `Purpose`, `RequestIp`, `CreatedAt`. Old challenges for same (email, purpose) NOT revoked on new create (overlapping windows OK; brute-force per-challenge). (2) Verify path: lookup latest active challenge, check `!Consumed && ExpiresAt>now && Purpose==request.Purpose`, HMAC hash incoming code SAME pepper, `CryptographicOperations.FixedTimeEquals` (constant time) compare hashes, `AttemptCount++`. If FAIL 5th attempt → `Consumed=true`, `LockedOut=true` in OtpVerifyResult (HTTP 429). If SUCCESS: `Consumed=true`. If `Purpose == "email_verify"`: `user.EmailVerified = true` + `SaveChanges`. Email sent via Brevo with per-purpose branded template. Admin notice: when a user FIRST registers, `SendNewAccountAdminEmailAsync` renders `BrandedEmailTemplate.RenderNewAccountAdminEmail(user.DisplayName, user.Email, siteUrl)` and sends to `Brevo:ContactToEmail` (CONTACT_TO_EMAIL env fallback; canonical fallback `info.donartkins.ke@gmail.com` used when both unset — notice never skipped). |
| D11 | Email rendering | C# port of the Griot branded TypeScript email shell (bug.txt L~1200–1400). File: `BrandedEmailTemplate.cs`. Brand tokens match bug.txt TS constants: Bg=#EFEFEF, Card=#FFFFFF, Ink=#111111, Muted=#666666, Accent=#FF5A36, Border=#EEEEEE, Soft=#FAFAFA, FontSans=`font-family: Inter, ui-sans-serif, system-ui, -apple-system, sans-serif`, FontDisplay=`font-family: 'DM Serif Display', ui-serif, Georgia, serif`. No external images (deliverability risk — spam filters block image-only HTML). Logo = inline `<svg>` letter "G" in Accent color with a swervy underline stroke (replaces TS CID-embedded underline PNG). 3 OTP purposes use one method `RenderOtpEmail(purpose)` with switch: eyebrow/title/accent-word/preheader/body/footer different for each. Admin notice uses separate `RenderNewAccountAdminEmail` with key-value rows and detail section card. Text/plain body generated by stripping HTML tags (minimal fallback for plaintext MIME part; Brevo supports `textContent` field alongside `htmlContent`). |
| D12 | Email transport (Brevo, not Resend) | Replaced Resend (sandbox: only sends own-account/verified-domain recipients). Class: `BrevoEmailService : IEmailService`. Constructor takes `HttpClient` (typed client configured with `ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })`) + `IConfiguration` + `ILogger` — SSRF redirects disabled; request URI restricted to `ApiUrl = "https://api.brevo.com/v3/smtp/email"` fixed constant. `SendAsync` performs 3 `IConfiguration` lookups for the api-key in this order: `Brevo:ApiKey`, `BREVO_API_KEY`, `Brevo__ApiKey` (double-underscore for Docker env). If all are null/whitespace, logs warning and returns `false`. Brevo auth header is `api-key: <key>` (NOT Bearer). Request: `POST /v3/smtp/email`; body `{ sender: {name, email}, to: [{email}], subject, htmlContent, textContent: PlainText(html) }`. Sender resolution (amended 2026-09-11 — single sender; `Brevo:Senders:<Key>` profile map and per-call `From` parsing removed): `Brevo:FromEmail`/`BREVO_FROM_EMAIL` (REQUIRED — Brevo only sends from the one dashboard-verified sender; local config = `info.donartkins.ke@gmail.com`); name `Brevo:FromName`/`BREVO_FROM_NAME` → `Griot`; optional per-call `message.ReplyTo`. Never throws: catch `Exception` (including `OperationCanceledException` — cancellation is treated as a failed delivery) → `_logger.LogError(...)` + return `false`. Brevo 429/daily-quota-exceeded → false (no retry; manual retry via user re-clicking /otp/request). 502 from `/api/auth/otp/request` controller ONLY if Brevo returns non-success; register NEVER fails on Brevo error (best-effort OTP send after SQL commit). |
| D13 | CORS | Policy `AllowSpecificOrigins`: `builder.Configuration["Cors:AllowedOrigins"]?.Split(',', ...)` plus always-added `http://localhost:5173` (Vite web dev) + `http://localhost:5064` / `https://localhost:7198` (self). Production Vercel origin(s) added via config. No wildcard in prod. Policy applied early in pipeline before auth. |
| D14 | Cookie vs Authorization header | Bearer-only via `Authorization: Bearer <access_token>` header. No HTTP-only Cookie auto-sent. Rationale: Web app (React + Vite) stores in-memory + secure sessionStorage behind TLS-only strict HSTS. Mobile app stores in Flutter flutter_secure_storage. CORS preflight sends `Access-Control-Allow-Headers: Authorization`. Avoids CSRF entirely (Cookie auto-send = CSRF; Bearer requires explicit header = CSRF-safe per OWASP CSRF prevention cheatsheet). |
| D15 | Logout behavior | `POST /api/auth/logout` requires `[Authorize]`. Reads JWT jti (currently unused — no blacklist SET Redis for access tokens because 15min short-lived access + server restart wipes). Accepts body `refreshToken` (optional? No: current controller expects `LogoutRequest.RefreshToken`). Validates via `HashToken` guard; malformed → 204 anyway. Valid → lookup row; set `RevokedAt=now, RevocationReason="explicit_logout"`; save. Returns 204 No Content ALWAYS (no info leak). |
| D16 | Environment variable matrix (keys, fallbacks, secrets) | Contract in `docs/api/auth-contract.md` configuration table + `backend/appsettings.Local.json` (git-ignored dev). Canonical keys (Docker separator `__` supported): `ConnectionStrings__Default`, `JWT__Key`, `JWT__Issuer`, `JWT__Audience`, `Redis__Connection` (default `localhost:6380` fallback), `Otp__Pepper`, `Brevo__ApiKey` / `BREVO_API_KEY`, `Brevo__FromEmail` / `BREVO_FROM_EMAIL` (REQUIRED verified sender), `Brevo__FromName` / `BREVO_FROM_NAME` (default `Griot`), `Brevo__ContactToEmail` / `CONTACT_TO_EMAIL` (canonical address `info.donartkins.ke@gmail.com`; used as hardcoded fallback when both config keys unset so admin notice is never skipped), `Cors__AllowedOrigins`. All values read via `IConfiguration`; zero secrets hardcoded. |
| D17 | GraphQL auth (Feature 05 ↔ Feature 07) | HotChocolate `AddAuthorization()` at schema; `[Authorize]` on every Query and Mutation (except introspection). Requires SAME `Authorization: Bearer <access_token>` as REST. `IHttpContextAccessor` → `ClaimsPrincipal` resolved inside resolvers for `sub` / `email` workspace membership checks. Unauthenticated → HotChocolate error `AUTH_NOT_AUTHENTICATED`. No separate GraphQL-only auth; single token single principal. |

---

## 3. Options considered

| Option | Proposal | Pros | Cons | Verdict |
|---|---|---|---|---|
| A (Status-quo-antisec) | No auth at all — scaffold stubs returned for every entity endpoint | Bootcamp PDF silent, zero lines code | OWASP A01 Broken Access Control, A07 Identification failures, A08 Software Integrity. Fails Week-6 QE (OWASP) and Week-7 k6 (no auth → anyone can hammer). **Cannot ship.** | ❌ Rejected |
| B | IdentityServer / OpenIddict + cookie auth + DataProtection stack | Spec-compliant, audited, token server, grants, refresh lifetime policy, signing key rollover, TOTP built-in | Adds 6 NuGets + DataProtection key ring persistence (Redis or SQL); breaks "no new containers/infra" constraint (D12/D16); learning curve for bootcamp team; cookie-only default breaks CSRF assumptions of web/mobile REST teams. Overkill for 7 systems × 1 bootcamp. | ❌ Rejected |
| C | Keycloak / Auth0 in separate container via compose + JWT from external IdP | Enterprise SSO-ready; RBAC + login page + password reset UI | Violates hard rule 1 (no auth code separation: auth in infra, not backend); adds one more compose service (memory on Railway free tier = ~512MB too tight for Keycloak+SQLServer+Redis+.NET simultaneously); Postman dev cannot test register/login locally without booting 2 GB IdP. | ❌ Rejected |
| D | bcrypt + long-lived JWT (30d) + no refresh rotation | Simpler, fewer DB writes, no Redis sliding-window logic | bcrypt 72 B truncation (long passwords silently drop entropy); GPU-crackable (no memory-hard cost); 30d JWT = theft → 30d impersonation (no way to revoke without blacklist which = Redis anyway). | ❌ Rejected |
| E | Cookie session (server-sticky) + bcrypt + in-memory rate limiter | No JWT signing config; "works on localhost" | Server-sticky = fails on multiple backends / Vercel serverless. In-memory limiter = per-server count, not global behind load balancer. | ❌ Rejected |
| F | Pure Redis session JTI → access + refresh linked to Redis key | Every API call requires Redis roundtrip (lookup `session:{jti}` → get claims + TTL); immediate logout/global revoke works | 1–2 ms latency added to EVERY REST/GraphQL resolver; Redis = SPOF → full outage on Redis restart; harder cache/cost scale for dashboard paginated 100+ task queries. | ❌ Rejected |
| G | **THIS ADR (Argon2id + 15m HS256 JWT ≥32-byte key validated at startup + 30d SHA256 at-rest opaque rotated refresh + FamilyId revoke + provider-default tx conditional ExecuteUpdate affected rows + Redis sliding limit + Brevo OTP HMAC-peppered + branded template emails + Bearer-only no cookies + Brevo client redirects disabled + admin inbox canonical fallback never skipped)** | Single .NET container; no extra services; memory-hard password hash; startup rejects short/missing JWT keys; zero-info-leak status codes for malformed tokens/logout; rotation window of theft = single use; family scope preserves honest second sessions; Redis singleton correct lifetime; OTP never plaintext; constant-time compares; SSRF redirects blocked on Brevo HTTP client; admin notice always delivered (canonical fallback); Postman collection can exercise all codes; scalable to 2x backend replicas behind LB (Redis = global rate limit + shared multiplexer); CSRF-safe; OWASP A01/A02/A04/A05/A07/A10 largely closed; no auth-hardware cost for devs | More code than (A) or (D); Brevo emails require a verified sender + env key for deliverability (local Postman tests can hit 502 without one); PasswordReset purpose scaffolded but no reset-send-link v1.1; TOTP future requires QR-code library NuGet. All tradeoffs acceptable for bootcamp MVP. | ✅ **ACCEPTED** |

---

## 4. Consequences

### 4.1 Positive

- **Single .NET 8 container deploys to Railway/Render.** No new compose services beyond existing SQL Server + Redis (Spec 03 already added Redis for caching).
- **OWASP Top 10 2025 coverage:** A01 Broken Access Control closed (per-workspace checks in GraphQL + REST), A02 Cryptographic Failures closed (Argon2id, HS256 ≥256-bit, SHA-256 refresh at rest, HMAC OTP at rest), A04 Insecure Design closed (family-scoped blast radius, timing oracle, malformed input, duplicate-email precision), A07 Identification and Authentication Failures closed (rate limit 10/15min login / 3/15min OTP, 2FA email OTP, brute-force lockout at 5 OTP attempts, Argon2 cost at NIST memory-hard recommendation).
- **Postman + Newman (Spec 08 QA 05) can exercise every meaningful HTTP status:** 200 login, 201 register, 202 OTP request, 204 logout, 400 bad body, 401 unknown/malformed/replay/expired, 409 duplicate email, 429 rate limit + OTP lockout, 501 scaffold other routes, 502 Brevo down. This makes the Postman collection NON-trivial (not all 200).
- **GraphQL Feature 05 reuses the EXACT SAME bearer token.** No GraphQL-only session. No auth N+1; resolver workspace checks use `ClaimsPrincipal` directly.
- **Family-scoped revocation is correct for real mobile + browser multi-device use case.** Honest user logging in from laptop + phone has 2 families; laptop token leaked/reused → laptop family revoked, phone stays logged in.
- **Scaffold controllers upgraded to 501 Not Implemented** → prevents Postman-test confusion where 200 NI looks like a success.

### 4.2 Negative / Trade-offs

1. **15-minute access tokens mean Web/Mobile apps MUST call `/refresh` before 15m.** If the app holds an access token past expiry, REST/GraphQL returns 401 and the client MUST run refresh→retry the original request. Web (TanStack Query 5) + Mobile (GraphQL Flutter + Riverpod) both must implement token-refresh retry interceptor. Tracked in Web spec 05 and Mobile spec 02.
2. **`GRIOT_SERVICE_TOKEN` for Trigger.dev AI (Spec 09) was NOT implemented in this ADR's reserved shape.** **Status (2026-09-11): spec 09 implemented a real-user OBO principal instead** — `ServiceToken` scheme + `X-On-Behalf-Of` header resolving a real `Users.Id`; the reserved `sub=ai-agent:*` / `griot.role=service` virtual-principal shape is **superseded** and must not be reintroduced. `/login` still only accepts real user credentials.
3. **Brevo sender deliverability:** Dev emails from a Gmail sender often go to spam. For real users / production, register a real sending domain in Brevo, add SPF/DKIM/DMARC DNS records, update `Brevo:FromEmail` → `noreply@griot.<tld>`.
4. **OTP pepper rotation:** If `Otp:Pepper` env value changes between restarts, all in-flight OTP challenges (up to 10 minutes) instantly fail — users would need to request a new code. Acceptable (10m window) because pepper rotation is infrequent.
5. **Access token logout = 15m wait (no blacklist SET).** Decision: no Redis SET per access token issued (would multiply Redis keys by ~user count × 1/day). To force a user logout before 15 min, revoke their refresh family (next refresh attempt = 401). Honest access-token lifetime of 15 min is the maximum unauthorized access window after explicit logout. Tradeoff accepted per NIST SP 800-63B §7.1 (short-lived access + longer-lived refresh = recommended balance).
6. **PasswordReset OTP purpose scaffolded, but the actual password-reset email that carries a link to `/reset?code=...` is NOT implemented yet.** v1.1 UI work in Web spec 06 / 07. Purpose exists; controller accepts it; verify marks a reset-step-up flag (not yet; today verify returns `{verified:true}` and sets `EmailVerified` only for purpose `email_verify`); future reset controller logic will consume the reset-purpose verified flag and allow `Users.PasswordHash` update.

### 4.3 Follow-up actions (sequenced behind current Spec progression)

- **Spec 08 (Postman):** Create `feature/backend/08-api-testing-postman`; import auth routes with real examples (register 201 → login 200 → refresh 200 → reuse refresh 401 → logout 204 → otp/request 202 → otp/verify 200 → 5 wrong OTP 429).
- **Spec 09 (AI service token):** ✅ **DELIVERED (2026-09-11)** as `ServiceTokenHandler` (scheme `ServiceToken`, config `ServiceToken:Key` ?? `GRIOT_SERVICE_TOKEN`, constant-time compare) + `MultiAuth` policy forward-selector + `X-On-Behalf-Of: {real User.Id}` → real-user OBO principal (role `ai-on-behalf-of`, 4 scope claims: ReadWorkspace/CreateTask/AddComment/CreateNotification; deletes/invites → 403). Webhook: `WebhookHmacMiddleware` (`Webhook:Secret` ?? `WEBHOOK_SECRET`) verifies `X-Trigger-Signature` before auth on `POST /api/webhooks/trigger` (401/503). Trigger → .NET REST: `TriggerDevClient` (`Trigger:SecretKey` ?? `TRIGGER_SECRET_KEY`, enqueue-after-persist). No reserved `sub=ai-agent:*` virtual principal was created.
- **Spec 11 (Blob storage):** Not related to auth.
- **Operational follow-up (any time):**
  1. Brevo custom sending domain + SPF/DKIM/DMARC DNS records → update `Brevo:FromEmail`.
  2. Add Brevo bounce/complaint webhook handler at `POST /api/webhooks/brevo` (currently scaffold 501). Mark consumed refresh / disabled OTP / user disabled on hard bounces.
  3. Add Redis password (`requirepass`) in compose + env `Redis:Connection` = `localhost:6380,password=...` (currently no auth on dev Redis; fine for localhost; production Railway Redis must enable AUTH).
  4. TwoFactorMethod `Totp` implementation: NuGet `OTP.Net` or equivalent; render QR code in Web; verify controller path = reuse `/otp/verify` with purpose `login_2fa_totp` (or new verify-totp endpoint; decide in v2 ADR).
  5. Password-reset link email: send branded reset-link email with short-lived signed purpose=password_reset step-up token consumed by Web reset page; backend `POST /api/auth/password/reset` validates step-up token, accepts new password, Argon2ids it, invalidates ALL refresh families for user (password change = log out everywhere).

---

## 5. References

1. Research: `research/ai-features-research.md` §1 (OTP 2FA own-stack)
2. Feature spec 07: `backend/project-kit/feature-specs/07-auth-jwt-argon2-redis.md`
3. Auth contract (canonical routes + statuses + claims + env): `docs/api/auth-contract.md`
4. API surface (auth routes table): `backend/project-kit/context/api-surface.md`
5. Integration contracts (port 5064 / 7198 + CORS + service-token shape): `project-kit/context/integration-contracts.md`
6. Implementation tests: `backend/tests/Griot.Tests/Auth/` (AuthServiceTests — 18 unit tests; AuthSqlTests — SQL integration with opt-in `GRIOT_RUN_SQL_TESTS=1`)
7. Implementation source: `backend/src/Griot.Api/Controllers/AuthController.cs` + `backend/src/Griot.Application/Services/AuthService.cs` + `backend/src/Griot.Infrastructure/Repositories/AuthRepository.cs`
8. Planning context: `backend/project-kit/feature-specs/07-auth-jwt-argon2-redis.md`
9. PHCH 2015 Argon2 specification: https://password-hashing.net/
10. NIST SP 800-63B §5.2.2 (online guessing throttling) + §7.1 (token lifetime)
11. OWASP CSRF Prevention Cheat Sheet (Bearer vs Cookie rationale): https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html
12. StackExchange.Redis best practices (singleton multiplexer, `host:port` format NOT `redis://`): https://stackexchange.github.io/StackExchange.Redis/Basics
13. RFC 7231 §6.6.2 (501 Not Implemented for scaffold routes): https://www.rfc-editor.org/rfc/rfc7231#section-6.6.2

## Feature 09 review hardening — 2026-09-11

REST AiAccessFilter and GraphQL AiFieldMiddleware default-deny every unmapped operation. Reads require ReadWorkspace; createTask/CreateTask, addComment/AddComment are the current writes. CreateNotification is reserved until its backend route ships. Configured delegations bind the service to a real user, allowed workspaces, a subset of the four scopes and UTC expiry; membership is checked independently. Auth/OTP, updates, deletes, invites, member management and raw-log AI access are denied. GraphQL execution denials return errors (FORBIDDEN for the field gate) with HTTP 200 for application/json; REST returns 403. No fifth scope exists until backend 24.
