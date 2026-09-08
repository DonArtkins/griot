# Feature 07 — Auth Repair & Evidence (Email-OTP 2FA + CodeRabbit)

Date: 2026-09-08. Branch: `feature/backend/07-auth-jwt-argon2-redis`.

## 1. Root cause: "Not implemented yet" on every auth route (POSTMAN)

The auth controller/service/repository WERE implemented in this branch, but the running binary was
**stale** — `backend/src/Griot.Api/bin/` dated before the auth code was written, so Postman received
`200 {"message":"Not implemented yet"}` (the Feature 04 scaffold stub( and DBeaver showed no rows.

**Fix:** rebuild (see Verification( — the current source returns `201` register, `200` login/refresh,
`204` logout, and persists user + refresh-token rows in SQL Server;the OTP endpoints `202/200/401/429`.

##  ‏2. CodeRabbit review — 8 actionable comments, all addressed

| # | Finding | Status | Where |
|---|---|---|---|
| 1 | GRAPHQL-STATUS/DEPENDENCY-AUDIT stale (pending, 3 warnings, Spec  ‏07 next( | ✅ already fixed in working tree: zero-warnings + Spec  ‏08 next; re-verified | `backend/GRAPHQL-STATUS.md`, `docs/DEPENDENCY-AUDIT.md` |
| 2 | Redis fallback `redis://localhost:6380` breaks StackExchange parse | ✅ `localhost:6380` | `Program.cs` |
| 3 | `IConnectionMultiplexer` registered scoped → disposed per request | ✅ `AddSingleton` | `Program.cs` |
| 4 | Register race: `DbUpdateException` must map only the Email unique-violation →409 | ✅ catch in `CreateUserAsync` scoped to `IX_Users_Email`/`dbo.Users`; others rethrow | `AuthRepository.cs` |
| 5 | Login timing oracle: skip `VerifyPassword` for unknown users | ✅ always verify against fixed `DummyPasswordHash` | `AuthService.cs` |
| 6 | Malformed refresh token → `Convert.FromHexString` throws (500( | ✅ `HashToken` returns null unless exactly 64 hex chars; refresh→401, logout→204 | `AuthService.cs` |
| 7 | Refresh rotation not atomic; concurrent reuse revokes unrelated sessions | ✅ conditional `RevokedAt IS NULL` update + insert in one transaction; zero-row → reuse→family revoke | `AuthRepository.RotateRefreshTokenAsync` |
| 8 | Reuse revocation user-wide (`user-wide family-aware revocation(` | ✅ family-scoped (`FamilyId` preserved on rotation; `RevokeFamilyAsync(userId, familyId(` | migration + `AuthRepository` + contract |

Coverage proof: `AuthServiceTests` (unit: malformed tokens, dummy-login, family reuse, rotation-race(,
`AuthSqlTests` (SQL: migration backfills chains/independent sessions, concurrent rotation exactly-one,
replay keeps independent session, HTTP register→login→refresh→reuse→logout( + OTP verify/lockout test.

##  ‏3. Email-OTP 2FA (research/ai-features-research.md §1( — now in scope and implemented

| Contract | Implementation |
|---|---|
| Routes | `POST /api/auth/otp/request` (202; 3/15min/email Redis limit; 401 unknown email; 502 Resend fails(; `POST /api/auth/otp/verify` (200/401; lockout 429 after 5( |
| Purposes | `email_verify` (auto-sent on register; sets `Users.EmailVerified`(, `login_2fa`, `password_reset` |
| Security | crypto-secure 6-digit code (RNG(; HMAC-SHA256 hashed at rest with `Otp:Pepper`; 10-min expiry; `AttemptCount` lockout; constant-time compare; old challenges superseded |
| Email | Resend transport (`Resend:ApiKey`/`RESEND_API_KEY`(; branded per-purpose template (`BrandedEmailTemplate.cs` — port of the Sababisha site's swervy shell(; admin "New user registered" notice → `Resend:ContactToEmail`/`CONTACT_TO_EMAIL` |
| Config | `Otp__Pepper`, `Resend__ApiKey`, `Resend__FromEmail`/`RESEND_FROM_EMAIL`, `Resend__ContactToEmail`/`CONTACT_TO_EMAIL`, `SITE_URL` |

##  ‏4. Contract sync matrix (changed → synced(

- API routes/statuses/config → `docs/api/auth-contract.md` (+ route table(, `backend/project-kit/context/api-surface.md`, `project-kit/context/integration-contracts.md`, `docs/api/README.md`, root+backend `AGENTS.md` (Feature 07 contract block(, spec 07 (Implementation Notes, Out of Scope corrected, Acceptance Criteria(.
- Entity/field (`Users.EmailVerified`, refresh `FamilyId`( → ERD amendment `diagrams/erd/auth-family-amendment.md`, snapshot + migration Designer(`.
- Skill stale claims (`sub`+`wid` → `sub`/`email`/`jti`;( → `backend/.agents/skills/jwt-argon2-auth/SKILL.md`。
- Research → spec mapping: `research/ai-features-research.md` §1 DoD items now implemented for Griot `[own-stack]` auth (SPF/DKIM/DMARC landing-domain + bounce-webhook suppression remain separate ops items(.

##  ‏5. Verification

```bash
cd backend
dotnet build --no-incremental                    # 0 errors / 0 warnings
GRIOT_RUN_SQL_TESTS=1 dotnet test            # unit + SQL/HTTP auth + OTP green
# live (compose redis + sql up; Resend key set(:
dotnet watch run --project src/Griot.Api
curl -s -X POST http://localhost:5064/api/auth/register -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com","displayName":"You","password":"Password123!"}'   # → 201 + token pair
# → inbox receives the branded "Verify your email" code; then:
curl -s -X POST http://localhost:5064/api/auth/otp/verify -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com","code":"<code>","purpose":"email_verify"}'      # → 200 {verified:true,emailVerified:true}
python3 scripts/check-contract-sync.py            # from repo root → pass
```

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**