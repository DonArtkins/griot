# Griot Authentication System — Complete Guide

**Owner:** Backend / API (Feature 07)  
**Architecture decision:** [ADR-003](../decisions/ADR-003-auth-architecture-otp-security.md)  
**REST contract:** [auth-contract.md](../api/auth-contract.md)  
**Implementation source:**  
- `backend/src/Griot.Api/Controllers/AuthController.cs`  
- `backend/src/Griot.Application/Services/AuthService.cs`  
- `backend/src/Griot.Infrastructure/Repositories/AuthRepository.cs`  
**Tests:** `backend/tests/Griot.Tests/Auth/` (18 unit tests, all passing)

---

## 1. Overview

Griot uses a **custom [own-stack] authentication system** built on seven cooperating components in a single .NET 8 container. There are no external auth services, no IdentityServer, no Keycloak, no cookies — everything runs inside the backend API container for minimal deploy surface.

```
Client (Postman / Web / Mobile)
        │  POST /api/auth/*  (JSON body, Bearer header on logout)
        ▼
  AuthController (thin — validation, rate-limit, status mapping)
        │
        ▼
  AuthService (business — Argon2, JWT, refresh rotation, OTP)
        │           │
        ▼           ▼
  AuthRepository   IEmailService
  (EF Core SQL)    (Resend HTTP)
  │  │  │            │  │
  ▼  ▼  ▼            ▼  ▼
 Users RefreshTkns  resend.com
 OtpChallenges
        │
        ▼
  Redis (sliding-window rate limiting — singleton multiplexer)
```

---

## 2. All Use Cases & Endpoints

Local base URL: `http://localhost:5064` (HTTP) or `https://localhost:7198` (HTTPS).  
Container port: `8080`.

### 2.1 Register a new account

```
POST /api/auth/register
Content-Type: application/json

{
  "email": "alice@example.com",
  "displayName": "Alice Otieno",
  "password": "Correct-Horse-Battery-Staple-42!"
}
```

**Success:** `201 Created`
```json
{
  "accessToken": "<640-char JWT>",
  "refreshToken": "<exactly-64-hex-chars>",
  "expiresAt": "2026-09-09T12:15:00Z",
  "user": {
    "id": "guid",
    "email": "alice@example.com",
    "displayName": "Alice Otieno",
    "avatarUrl": null
  }
}
```

**Side effects on success:**
- User row persisted to SQL Server `Users` table with Argon2id password hash
- First refresh token row created with new `FamilyId`
- **Auto-sends branded `email_verify` OTP code** to the user via Resend
- **Auto-sends "New user registered" admin notice** to `Resend:ContactToEmail`

**Failure cases:**
| Status | Cause |
|--------|-------|
| `400 Bad Request` | Missing fields, invalid email format, password < 8 chars |
| `409 Conflict` | Email is already registered (pre-check + post-Save duplicate-key catch — race-safe) |

### 2.2 Login with email + password

```
POST /api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "Correct-Horse-Battery-Staple-42!"
}
```

**Success:** `200 OK` — same body shape as register. A NEW refresh token family is created (different `FamilyId` from any register or other login session).

**Failure cases:**
| Status | Cause |
|--------|-------|
| `400 Bad Request` | Missing or invalid fields |
| `401 Unauthorized` | Unknown email OR wrong password (same response shape — no user enumeration) |
| `429 Too Many Requests` | Rate limit exceeded (10 attempts / 15 min per IP). Response includes `Retry-After: <seconds>` header. |

### 2.3 Rotate refresh token

Refresh tokens MUST be rotated before the 15-minute access token expires.

```
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<exactly-64-hex-chars>"
}
```

**Success:** `200 OK` — new access + new refresh token. The OLD refresh token is atomically revoked (cannot be reused). The SAME `FamilyId` is preserved on the new token.

**Failure cases:**
| Status | Cause |
|--------|-------|
| `400 Bad Request` | Empty or non-string refresh token |
| `401 Unauthorized` | Malformed (not 64 hex), unknown, expired, OR ALREADY-USED token. **Reuse revokes the ENTIRE family.** |

### 2.4 Logout (revoke refresh token)

```
POST /api/auth/logout
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "refreshToken": "<exactly-64-hex-chars>"
}
```

**Success:** `204 No Content` — ALWAYS. Malformed, unknown, or already-revoked tokens still return 204 (no information leak).

**Failure cases:**
| Status | Cause |
|--------|-------|
| `400 Bad Request` | Missing required fields |
| `401 Unauthorized` | Missing or invalid Bearer access token |

### 2.5 Request an OTP code (email 2FA)

Three purposes supported: `email_verify`, `login_2fa`, `password_reset`.

```
POST /api/auth/otp/request
Content-Type: application/json

{
  "email": "alice@example.com",
  "purpose": "email_verify"
}
```

**Success:** `202 Accepted`
```json
{
  "message": "A 6-digit code has been sent to alice@example.com.",
  "expiresInMinutes": 10
}
```

**Failure cases:**
| Status | Cause |
|--------|-------|
| `400 Bad Request` | Invalid purpose, missing fields |
| `401 Unauthorized` | Unknown email address |
| `429 Too Many Requests` | 3 requests / 15 min per email. `Retry-After` header set. |
| `502 Bad Gateway` | Resend API returned non-success (no key, network issue, quota exceeded) |

### 2.6 Verify an OTP code

```
POST /api/auth/otp/verify
Content-Type: application/json

{
  "email": "alice@example.com",
  "code": "123456",
  "purpose": "email_verify"
}
```

**Success:** `200 OK`
```json
{
  "verified": true,
  "message": "Code verified.",
  "emailVerified": true
}
```

- For `purpose: email_verify` only: `Users.EmailVerified` is set to `true`.
- `emailVerified` field in response reflects the result of `email_verify` purpose.

**Failure cases:**
| Status | Cause |
|--------|-------|
| `400 Bad Request` | Invalid purpose |
| `401 Unauthorized` | Unknown email, no active challenge, wrong code, code expired (>10 min) |
| `429 Too Many Requests` | 5 failed attempts on the same challenge → challenge permanently invalidated. Must request a new code. |

---

## 3. Postman Quick Start (No More "Not Implemented")

If you see `501 Not Implemented` or `{"message":"Not implemented yet"}`, you are hitting the wrong route. Auth routes are fully implemented.

**Correct auth routes — all working:**
```
POST /api/auth/register    → 201 (not 501)
POST /api/auth/login       → 200 (not 501)
POST /api/auth/refresh     → 200 (not 501)
POST /api/auth/logout      → 204 (not 501)
POST /api/auth/otp/request → 202 (not 501)
POST /api/auth/otp/verify  → 200 (not 501)
```

**Routes that DO return 501 (expected — future specs):**  
All workspace/project/board/task/comment/notification/invite/dashboard/column/attachment routes. These are planned for specs 09–11 and intentionally return `501 Not Implemented` so you cannot confuse a 200 scaffold with real data.

### Step-by-step Postman flow:

1. **Build the backend first** (critical if you pulled new code):
   ```bash
   cd backend
   dotnet build --no-incremental
   ```

2. **Start the backend** (Docker stack up for SQL + Redis):
   ```bash
   cd .. && docker compose up -d
   cd backend && dotnet run --project src/Griot.Api
   ```

3. **POST Register** → capture `accessToken` and `refreshToken` from `201` body.
4. **POST Login** → capture new tokens from `200`.
5. **POST Refresh** with `refreshToken` → get a NEW pair; confirm the old refresh now returns `401` on reuse.
6. **POST Logout** with `Authorization: Bearer <accessToken>` and the latest `refreshToken` → `204`.
7. **POST OTP Request** → `202` (or `502` if no Resend API key; the code is still stored in `OtpChallenges` table for manual testing).
8. **POST OTP Verify** with the 6-digit code → `200` and `Users.EmailVerified=true`.

---

## 4. Safety Rails — Why This Is Secure

Each safety rail below is tested by unit tests and has a specific CWE/OWASP mapping.

### 4.1 Password Hashing: Argon2id (not bcrypt, not PBKDF2)

**Config:** t=3 iterations, m=65536 KB (64 MiB memory), p=4 parallel lanes. 16-byte crypt-salt, 32-byte hash. Stored as `hexSalt:hexHash`.

**Why secure:**
- Argon2id was the **winner of PHCH 2015** (Password Hashing Competition), selected by cryptographers over bcrypt/PBKDF2.
- Memory-hard cost means GPU/FPGA cracking rigs get NO speed advantage (bcrypt is CPU-only, GPUs crack bcrypt at 100,000+ hashes/sec).
- NIST SP 800-63B §5.1.1.2 recommends memory-hard functions.
- **Fixed dummy-hash on unknown email** (see §4.7) prevents timing oracle even when the Argon2 cost is high.

**What insecure alternatives this fixes:**
- ❌ **bcrypt** — truncates passwords beyond 72 bytes (silently drops entropy). NOT memory-hard.
- ❌ **PBKDF2-SHA256** — CPU-only. GPU-vulnerable. CWE-916.
- ❌ **MD5/SHA-1/SHA-256 (plain)** — unsalted. Rainbow tables. Instant crack.

### 4.2 Access Tokens: Short-Lived HS256 JWT (15 minutes)

**Claims:** `sub` (UserId Guid), `email`, `jti` (opaque nonce). `iss=Griot`, `aud=GriotClients`. HS256 ≥ 256-bit key (≥32 UTF-8 bytes).

**Why secure:**
- 15-minute TTL limits the **theft window**: a leaked access token grants at most 15 minutes of access (no blacklist infrastructure needed).
- No workspace/role claims embedded — workspace membership is checked LIVE in resolver/controller (prevents stale privilege escalation from old JWTs).
- Symmetric HS256: single backend service, no RSA key-rollover complexity. Key rotation = env redeploy.
- **Startup key-length validation:** Before constructing `SymmetricSecurityKey`, the configured `JWT:Key` is checked for UTF-8 byte length ≥ 32; missing or shorter keys throw via the existing startup error path (InvalidOperationException at DI build time), preventing launch with a weak key.
- `jti` present for future per-token blacklist (Redis SET with 15m TTL) if needed post-launch.

**What insecure alternatives this fixes:**
- ❌ **30-day JWT** — token theft = 30-day impersonation. You'd need a Redis blacklist anyway.
- ❌ **Role/permission claims in JWT** — admin demotion doesn't take effect until token expires (stale privilege).
- ❌ **JWT without jti** — no audit trail, no way to revoke a specific access token without full logout.

### 4.3 Refresh Tokens: Opaque + Rotation + Family Revoke

**Structure:** 32 random bytes → 64 hex chars to client. SQL stores ONLY `SHA256(hex)` → 64 hex `TokenHash` (UNIQUE, NOT NULL). 30-day TTL. `FamilyId` indexed per login.

**Rotation:** Every `/refresh` call atomically revokes the old row AND inserts a new row with same `FamilyId`. One-use-only.

**Why secure:**
- **Hash-at-rest:** DB breach → attacker has SHA256 hashes, NOT the 64-hex tokens. Brute-forcing 256-bit random tokens is thermodynamically impossible.
- **Rotation = single-use theft window:** An attacker who steals a refresh token from browser memory can use it ONCE. The legitimate user's next refresh will hit reuse → **family-scoped revoke**.
- **Family scoping (NOT user-wide):** Laptop token reuse revokes ONLY the laptop family; mobile session stays logged in. Honest users with 2 devices do not get logged out because one device was compromised.
- **Provider-default transaction + conditional UPDATE:** Parameterless `BeginTransactionAsync()` uses SQL Server provider-default isolation (Read Committed). `ExecuteUpdateAsync WHERE Id = X AND RevokedAt IS NULL` reports affected rows for concurrency handling. Exactly 1 concurrent caller wins. Losers → rollback + family revoke. Race conditions CANNOT create double-valid tokens.
- **Malformed token guard before DB touch:** `token.Length != 64 || !token.All(Uri.IsHexDigit) → return null`. Garbage input → instant 401, no DB call, no 500 crash, no stack trace leak.

**What insecure alternatives this fixes:**
- ❌ **Long-lived opaque token (no rotation)** — theft = 30-day impersonation. No way to detect theft until user manually logs out.
- ❌ **Plain refresh token stored in DB** — DB breach = instant full account takeover for every user.
- ❌ **User-wide (not family-scoped) revoke** — attacker uses stolen token; victim's phone gets logged out too, victim can't tell it was an attack (they just re-login).
- ❌ **Race-unaware rotation** (no transaction, no conditional UPDATE) — concurrent refresh creates TWO valid tokens in a race. CWE-362 (Race Condition).

### 4.4 Timing Oracle Prevention: Dummy Password Hash

**Mechanism:** `LoginAsync` ALWAYS runs `VerifyPassword(user?.PasswordHash ?? DUMMY_PASSWORD_HASH, password)` with IDENTICAL Argon2 cost. The `if (user == null)` check comes AFTER hashing.

**Why secure:**
- Wall-clock time for "unknown email + any password" ≈ "known email + wrong password". An attacker cannot send 10,000 login emails and sort by response time to find which emails are actually registered.
- Eliminates CWE-208 (Observable Timing Discrepancy) for the login endpoint.
- The DUMMY_PASSWORD_HASH is a valid Argon2id record (same t/m/p parameters) — not a short-circuit "fake" verify.

**Test evidence:** `AuthServiceTests.Login_UnknownUser_RejectsEvenWhenDummyPasswordMatches` — 2 Theory cases.

**What insecure alternatives this fixes:**
- ❌ **Early-exit `if (user == null) return null;`** — 10,000 requests, sort by latency → valid emails identified. Account enumeration. CWE-204.
- ❌ **Different cost for dummy verify** — fast-path for unknown users = same oracle.

### 4.5 Duplicate Email: Two-Layer Defense with Precision

**Layer 1:** Pre-check `UserExistsByEmailAsync` → fast-path 409.  
**Layer 2:** `SaveChangesAsync` wrapped in try/catch. Only `SqlException` with `Number ∈ {2601, 2627}` AND message contains BOTH `IX_Users_Email` AND `dbo.Users` → `DuplicateEmailException` (409). All other UNIQUE violations → generic 500.

**Why secure:**
- Race-safe: two concurrent registrations for the same email cannot both pass Layer 1. Layer 2 catches the collision on UNIQUE index `IX_Users_Email`.
- Precision: `RefreshTokens.TokenHash` UNIQUE collision (one-in-billion) is NOT mislabeled as "duplicate email". No false 409s that would leak email addresses of random users hitting collision.
- CWE-703 (Improper Check for Exceptional Conditions) closed — no blanket catch-all `DbUpdateException → 409`.

**What insecure alternatives this fixes:**
- ❌ **Layer 1 only** (no post-Save catch) — concurrent registrations → 500 (or 200 with duplicate depending on transaction isolation).
- ❌ **Catch-all `DbUpdateException → 409`** — any UNIQUE violation mislabeled as duplicate email. Could leak emails via error message shape.

### 4.6 Rate Limiting: Redis Sliding Window (Lua script)

**Login:** 10 attempts / 15 minutes per IP. Key: `ratelimit:login:{ip}`  
**OTP Request:** 3 attempts / 15 minutes per email. Key: `ratelimit:otp:request:{email}`

Lua script atomically: removes expired entries, adds current timestamp, checks count. Returns `{allowed:0|1, retryAfterSec:int, remaining:int}`.

**Singleton `IConnectionMultiplexer`** — NOT scoped. Scoped multiplexer gets disposed after first request, breaking all subsequent Redis calls.

**Why secure:**
- NIST SP 800-63B §5.2.2: ≤100 online guesses/hour. 10/15min = ~40/hour (conservative).
- Sliding window (not fixed): 10 attempts at 14:59 + 10 at 15:01 = NOT allowed (window is the last 15 minutes of wall-clock).
- Global behind load balancer: shared Redis count, not per-server in-memory.
- OTP spam prevention: attacker cannot flood a victim's inbox (3 emails max per 15 minutes).
- `Retry-After: <seconds>` header set so honest clients know exactly when to retry.

**What insecure alternatives this fixes:**
- ❌ **In-memory rate limiter (per-server)** — 2 backends behind LB = 20 attempts/15min, not 10. Attacker splits traffic.
- ❌ **No rate limit** — credential stuffing with 10M common-password list → mass account takeover. CWE-799.
- ❌ **Fixed-window limiter** — 10 attempts at 14:59:59 + 10 at 15:00:01 → 20 attempts in 2 seconds. CWE-799 variant.

### 4.7 Logout Idempotence + Zero Information Leak

Malformed refresh token? Unknown token? Already revoked token? Token for a different user?
→ **HTTP 204 No Content**, every time. No body, no error, no distinction.

**Why secure:**
- An attacker cannot distinguish "this token existed and we revoked it" from "you made up 64 hex chars". No probing for valid refresh-token existence.
- Matches the spec for logout: "idempotent operation". Calling logout 10 times with the same token → 10× 204, not errors.
- CWE-200 (Exposure of Sensitive Information to Unauthorized Actor) for token probing closed.

**What insecure alternatives this fixes:**
- ❌ **200 for valid, 404 for unknown** — attacker iterates 64-hex space → finds valid (leaked) refresh tokens, can attempt reuse before family revoke kicks in.
- ❌ **500 for malformed tokens** — crash path, attacker learns token format rules.

### 4.8 Email OTP: Crypto-Secure Code + HMAC Pepper At-Rest + Constant-Time Compare

- **Code:** `RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")` — NOT `Random.Next` (which is modulo-biased and not crypto-secure).
- **Storage:** `HMACSHA256(OTP_PEPPER, UTF8("123456"))` → 64 hex `CodeHash`. NEVER plaintext code in DB or logs.
- **Compare:** `CryptographicOperations.FixedTimeEquals(codeHashA, codeHashB)` — NOT `==` (early-exit string compare = timing oracle per digit).
- **Lifetime:** 10 minutes. 5 failed attempts → challenge permanently locked out (429).
- **Pepper rotation consequence:** Changing `Otp:Pepper` invalidates all in-flight codes (acceptable — 10-min window).

**Why secure:**
- DB breach of `OtpChallenges` → attacker has HMAC hashes, not the 6-digit codes. Cannot login without the pepper (env var, NOT in DB).
- Pepper is a server secret, NOT per-code salt. Pepper rotation wipes all in-flight challenges (useful if you suspect server compromise).
- Constant-time compare: no oracle for "first digit correct". Attack cannot reduce search space from 1,000,000 to 10 × 10^5 by timing.
- 5-attempt lockout: even with 10-minute windows, brute-forcing a code takes (5 attempts / 10 min) × 1M codes = 200M minutes = impossible.

**What insecure alternatives this fixes:**
- ❌ **Plaintext OTP code in DB** — DB breach → every in-flight OTP visible to attacker.
- ❌ **`Random.Next(0, 1000000)`** — modulo bias (some codes appear slightly more often). Not cryptographically secure.
- ❌ **`if (a.CodeHash == b.CodeHash)`** (early-exit) — 6-digit timing oracle. Can crack in ~60 guesses with enough samples.
- ❌ **No attempt limit** — 1,000,000 requests = guaranteed crack.
- ❌ **No expiry** — OTP code from 6 months ago still works if attacker finds it in email.

### 4.9 Bearer-Only (No Cookies) = CSRF-Safe

All auth uses `Authorization: Bearer <token>` header. No HTTP-only cookies, no auto-send.

**Why secure:**
- OWASP CSRF Prevention Cheat Sheet: Bearer-token authentication is **inherently CSRF-safe** because browsers do NOT auto-attach the header on cross-site form POSTs.
- Cookie-based auth → even with `SameSite=Strict`, you risk legacy browser bypasses + `GET /change-email?to=attacker@evil.com` links.
- Mobile/React apps explicitly send the header; no ambient authority.

**What insecure alternatives this fixes:**
- ❌ **Cookie auth without CSRF tokens** — cross-site `<form action="https://api/account/delete" method="POST">` submits with cookies. Account deleted.
- ❌ **Cookie + SameSite=Strict only** — breaks OAuth callback flows; legacy browsers (IE11) ignore SameSite. Defense-in-depth = avoid cookies entirely.

### 4.10 Email Transport (Resend) — Fail-Open on Register, Fail-Closed on OTP Request

- **Register** sends `email_verify` best-effort: Resend 429/5xx → log + return 201 anyway. User can re-request verification OTP later.
- **`POST /api/auth/otp/request` (explicit):** Resend failure → **502 Bad Gateway** (fail-closed — explicit request MUST deliver or inform).
- From address fallback: `Griot <onboarding@resend.dev>` (Resend onboarding sender).
- Admin notice: "New user registered" → `Resend:ContactToEmail` (canonical fallback: `info.donartkins.ke@gmail.com` when configuration keys are unset — notice is always delivered, never skipped).
- Never throws: `HttpRequestException` / generic `Exception` → `_logger.LogWarning` + return `false`. No 500 crash on email network blip.

**Why secure:**
- Register (user just typed their email) → no Resend key = user can STILL login. Blocking registration on email failure would be a denial-of-service.
- Explicit OTP request (user expects an email NOW) → 502 tells the user it didn't send; they debug API key / inbox.
- No stack traces or Resend response bodies in 502 payload (CWE-209 closed).

**What insecure alternatives this fixes:**
- ❌ **Register fail-closed** — Resend rate-limited → every new user gets 502. DoS.
- ❌ **OTP request fail-open** — `202 Accepted` but no email sent. User waits 10 minutes, thinks code is in spam, requests another, another.

### 4.11 CORS: Specific Origins, Never Wildcard in Prod

Allowed origins from `Cors:AllowedOrigins` (comma-separated) plus always-added localhost dev servers: `http://localhost:5173` (Vite Web), `http://localhost:5064`, `https://localhost:7198` (self).

**Why secure:**
- Wildcard `*` + JWT Bearer = any website can call your API from the user's browser (combined with token theft from browser extension / XSS).
- Explicit list: only `https://app.griot.tld` can make authenticated cross-origin requests.

**What insecure alternatives this fixes:**
- ❌ **CORS Allow: `*`** — attacker's `evil.com` makes fetch() calls with the user's Bearer token (stolen via XSS) and exfiltrates workspace data.

---

## 5. OWASP Top 10 2025 — Coverage Summary

| OWASP ID | Risk | Closed by which safety rail |
|----------|------|------------------------------|
| **A01 Broken Access Control** | Missing auth, CWE-862 | `[Authorize]` on all non-auth controllers + GraphQL. Workspace membership live-checked per resolver. |
| **A02 Cryptographic Failures** | Weak hashing, plaintext secrets | §4.1 Argon2id, §4.3 SHA-256 refresh hash, §4.8 HMAC OTP pepper, §4.2 HS256 ≥256-bit. |
| **A03 Injection** | SQL/command injection | EF Core parameterized queries; Dapper `usp_` procs use TVP params; no string-concat SQL. |
| **A04 Insecure Design** | Missing rate limit / revoke / timing guards | §4.3 rotation+family revoke, §4.6 rate limit, §4.4 timing oracle, §4.5 duplicate-email precision, §4.7 zero-info logout, §4.8 OTP lockout. |
| **A05 Security Misconfiguration** | Debug in prod, default creds | All secrets from env/appsettings.Local.json (git-ignored). Swagger UI `IsDevelopment` only. |
| **A07 Identification & Auth Failures** | Credential stuffing, brute-force, no 2FA | §4.6 login rate limit, §4.8 OTP 2FA + 5-attempt lockout, §4.1 Argon2 memory-hard, §4.4 no enumeration, §4.3 family revoke. |
| **A08 Software Integrity Failures** | Untrusted deps | Dependency audit at `docs/DEPENDENCY-AUDIT.md`. All NuGets pinned. |
| **A10 Server-Side Request Forgery** | Blind HTTP calls from backend | Resend `IHttpClientFactory` named client configured with `AllowAutoRedirect = false` (redirects disabled on primary handler) and request URI restricted to `https://api.resend.com/emails` — cannot reach internal `localhost:6380`/`14333` via open redirect or 3xx response. |

---

## 6. Complete List of Rejected Insecure Alternatives

These approaches were explicitly considered and REJECTED in ADR-003 §3. Each would have opened critical vulnerabilities:

| # | Rejected Alternative | What Would Have Gone Wrong |
|---|---------------------|----------------------------|
| A | **No auth at all** (scaffold 200 for every entity) | OWASP A01/A02/A04/A07 fully open. Any caller can read/modify any workspace. Week-6 OWASP QA = complete failure. |
| B | **IdentityServer / OpenIddict + DataProtection** | 6+ NuGets, DataProtection key-ring persistence (Redis/SQL), overkill for 1 backend container × 1 bootcamp. Cookie-only default breaks CSRF model. Learning curve for the whole team. |
| C | **Keycloak / Auth0 in separate container** | Breaks hard rule 1 (auth code in infra, not backend). +1 container → Railway 512MB RAM can't fit SQLServer+Redis+.NET+Keycloak simultaneously. Postman dev needs 2GB IdP booted to test login. |
| D | **bcrypt + 30-day JWT + no refresh rotation** | bcrypt 72-byte truncation (silent entropy loss). GPU crackable. 30-day JWT = theft → 30-day impersonation. You'd need Redis blacklist anyway. |
| E | **Server-sticky cookie + in-memory rate limiter** | Fails on multiple backends / Vercel serverless (sticky sessions). In-memory limiter = per-server count, not global behind LB. CSRF attack surface. |
| F | **Pure Redis session (every API call = Redis lookup)** | Every REST/GraphQL resolver pays 1-2ms Redis roundtrip. Dashboard paginated 100-task queries → N+1 Redis lookups. Redis restart = full logout (SPOF). |
| G (this one) | **Argon2id + 15m HS256 JWT (≥32-byte key validated at startup) + SHA-256 refresh-at-rest + rotation + FamilyId revoke + provider-default transaction conditional UPDATE + Redis sliding limit + Resend OTP HMAC-peppered + branded email + Bearer-only + Resend client redirects disabled** | ✅ **ACCEPTED**. See §4 for 11 rails. |

---

## 7. Configuration Matrix (Env Vars / appsettings.Local.json)

Set these in `backend/appsettings.Local.json` (git-ignored) OR as env vars (Docker uses `__` separator).

| Setting (appsettings) | Env var (Docker) | Default | Required? | Purpose |
|----------------------|------------------|---------|-----------|---------|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | none | ✅ YES | SQL Server. Local: `localhost,14333`. |
| `JWT:Key` | `JWT__Key` | none | ✅ YES | HS256 ≥ 32 bytes. Dev-only key in Local json. |
| `JWT:Issuer` | `JWT__Issuer` | `Griot` | no | JWT `iss` claim |
| `JWT:Audience` | `JWT__Audience` | `GriotClients` | no | JWT `aud` claim |
| `Redis:Connection` | `Redis__Connection` | `localhost:6380` | no | `host:port` format. Compose: `sababisha-redis:6379`. |
| `Otp:Pepper` | `Otp__Pepper` | `griot-dev-otp-pepper-change-me` | ⚠️ dev-only | HMAC pepper. **CHANGE IN PROD.** |
| `Resend:ApiKey` | `RESEND_API_KEY` | none | no | Unset → OTP request returns 502; register still works. |
| `Resend:FromEmail` | `RESEND_FROM_EMAIL` | `Griot <onboarding@resend.dev>` | no | Resend sending address. |
| `Resend:ContactToEmail` | `CONTACT_TO_EMAIL` | `info.donartkins.ke@gmail.com` (canonical fallback used when unset) | no | Admin inbox for new-user notices — canonical address `info.donartkins.ke@gmail.com`. |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins` | localhosts only | ⚠️ prod required | Comma-separated origins (Vercel prod domains). |
| `SITE_URL` | `SITE_URL` | `https://griot.app` | no | Used in branded email template footer links. |

---

## 8. Troubleshooting (Postman / Local Dev)

### "I get 501 Not Implemented on /api/auth/*"
**Cause:** You have a stale build (old `backend/src/Griot.Api/bin` with scaffold controllers).  
**Fix:** Rebuild clean.
```bash
cd backend
rm -rf src/Griot.Api/bin src/Griot.Api/obj
dotnet build --no-incremental
dotnet run --project src/Griot.Api
```

### "Register works but POST /api/auth/otp/request returns 502"
**Cause:** No `RESEND_API_KEY`. The code is still stored in `OtpChallenges` (for manual inspection with SQL).  
**Fix:** Set `RESEND_API_KEY` in env. Or for testing without email: query SQL `SELECT CodeHash, ExpiresAt FROM OtpChallenges WHERE UserId = (SELECT Id FROM Users WHERE Email='you@x.com')` — but you cannot reverse HMAC (no code = cannot verify). Use an API key.

### "Refresh returns 401 immediately after register/login"
**Cause 1:** The 64-hex token was truncated when copy-pasting from Postman. Check `token.Length == 64` and `all hex digits`.  
**Cause 2:** You already used it once (rotation). Every `/refresh` issues a NEW refresh. Old one won't work.  
**Cause 3:** Two concurrent callers carried the same refresh → both raced → family revoked (see §4.3). The legitimate refresh also fails (defensive tradeoff). Re-login to get a new family.

### "Login returns 401 but I'm sure password is correct"
**Cause 1:** Email not lowercase-trimmed. Login/register normalize to `lowercase(trim(email))`.  
**Cause 2:** Registration was rolled back (migration issue, Resend crash, etc.). Check `SELECT COUNT(*) FROM Users WHERE Email = 'x'`.

### "Redis connection error on startup"
**Cause:** `sababisha-redis` container not running (or wrong port).  
**Fix:** `docker compose up -d` from repo root (starts Redis on `localhost:6380` host port).

### "Logout returns 401"
**Cause:** Missing or invalid `Authorization: Bearer <accessToken>` header. Logout is the ONLY auth endpoint that REQUIRES the Bearer header. Refresh/register/login/OTP do not.

---

## 9. Verification Commands

Run these from `backend/` directory before committing or testing in Postman:

```bash
# 1. Build (catches any compilation errors — stale bin is #1 cause of "Not implemented")
dotnet build --no-incremental

# 2. Auth unit tests (18 tests — no SQL/Redis required for AuthServiceTests)
dotnet test --filter AuthServiceTests

# 3. Redis rate limiter tests (2 tests)
dotnet test --filter RedisRateLimiterTests

# 4. SQL integration tests (OPTIONAL — creates disposable test DB on configured SQL Server)
GRIOT_RUN_SQL_TESTS=1 dotnet test --filter AuthSqlTests

# 5. All tests
GRIOT_RUN_SQL_TESTS=1 dotnet test

# 6. Contract sync check (from repo root)
python3 scripts/check-contract-sync.py
```

**Expected results:** Build: 0W/0E. Tests: all green. Contract sync: exit 0.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
