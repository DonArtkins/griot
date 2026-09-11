# Backend Feature Spec 23 — Critical-Action OTP & Step-Up Verification [own-stack]

**Status:** PLANNED — user-approved scope extension of spec 07's email-OTP (research: `research/ai-features-research.md` §1, §3.6; ADR-003 constraint 6 already reserves the `TwoFactorMethod` switch and the step-up session design). No production code exists yet; this branch creates the spec and contract-syncs every dependent doc. One feature branch: `feature/backend/23-critical-action-otp-step-up`.

## Goal

Turn the existing email-OTP (`email_verify` / `login_2fa` / `password_reset`) into a full **critical-action policy** so that every high-blast-radius action is verified by a Brevo-delivered code, and no AI principal can ever touch this surface.

1. **Register** — auto-sent `email_verify` OTP + admin notice (implemented, spec 07).
2. **Login 2FA** — `Users.TwoFactorMethod = EmailOtp` → login challenges *before* any token is issued.
3. **Forgot / reset password** — `password_reset` OTP gates the reset; reset revokes every other session.
4. **Delete account** — NEW `DELETE /api/auth/account` gated by a step-up OTP + re-auth.
5. **Major operations** — step-up OTP (purpose `step_up`, action allow-list) for a curated risk list.

## Non-negotiable security rules (research §1 + ADR-003)

- 6-digit codes from a crypto-secure RNG (`RandomNumberGenerator`); stored as HMAC-SHA256 with `Otp:Pepper`; never logged or returned; constant-time compare; 10-minute expiry; 5-attempt lockout per challenge (`OtpChallenges.AttemptCount`).
- Redis `ratelimit:otp:request:{email}` 3/15 min + API global limiter 100/min/caller; lockout returns 429 with `Retry-After`.
- Every request/verify/attempt writes an `AuditLogs` row (`Action ∈ {Auth.Otp.Request, Auth.Otp.Verify, Auth.Otp.Failed, Auth.StepUp.Issued, Auth.StepUp.Used}` — with backend spec 20).
- **AI is permanently 403 on this whole surface.** Service-token OBO callers (spec 09) never request/verify OTP and never change MFA settings (research §3.6: auth stays human-gated, full stop). **Implementation (review fix):** one shared `RejectAiOnAuthSurface()` guard in `AuthController` runs BEFORE the rate limiter and any service call on `login`, `otp/request`, `otp/verify`, `forgot-password`, `reset-password` and `account` routes — 403 for AI OBO callers, normal human behavior untouched; xUnit asserts 403 per route.

## Dependencies

- Spec 07 (OTP endpoints, pepper, Redis gates, `TwoFactorMethod` enum) — implemented.
- Spec 12 (Brevo sender identities — `security` sender carries every OTP template).
- Spec 20 (audit rows for every auth/step-up event; this spec's tests are part of 20's trail).
- Spec 04/05 (guarded REST/GraphQL routes).

## Contract changes (PLANNED — additive or 202-on-challenge)

| Route | Auth | Behavior |
|---|---|---|
| `POST /api/auth/login` (modified) | public | `TwoFactorMethod == EmailOtp` → **202** `{twoFactorRequired:true, purpose:"login_2fa"}` without tokens; otherwise 200 as today |
| `POST /api/auth/otp/request` (extended) | public | new purposes: `delete_account`, `step_up` (optional `action` param) |
| `POST /api/auth/otp/verify` (extended) | public | `login_2fa` success → **200 `{verified:true, accessToken, refreshToken, user}`** (single round-trip login); `step_up`/`delete_account` success → **200 `{verified:true, stepUpAccessToken}`** — 5-min JWT with claims `stepup=true`, `purpose`, `action`, `challengeId` |
| `POST /api/auth/forgot-password` (new) | public | `{email}` → **202** (fires `password_reset` OTP; uniform 202 even for unknown email — no user enumeration) |
| `POST /api/auth/reset-password` (new) | public | `{email, code, newPassword}` → verify `password_reset`, Argon2 re-hash, revoke all refresh families → **200** |
| `DELETE /api/auth/account` (new) | bearer + step-up | verifies `delete_account`/`step_up` claim, revokes all refresh tokens, marks `Users.DeletedAt`, revokes memberships/invites → **204** |

### Step-up guarded action list (v1)

`Workspace.Delete` · `Workspace.TransferOwnership` · `Member.RoleChange` · `Member.Remove` · `Invite.Create` · `Project.Delete` · `Board.Delete` · `Column.Delete` · `Task.BulkStatus` · `Task.Delete` · `Comment.Delete` · `Attachment.Delete` · `Account.Delete`

Each maps to its existing handler plus a `RequireStepUp(action)` guard that validates the 5-minute `stepup` claim allow-list before the domain call (403 without it; 409 on action mismatch). Allow-list configurable via `Security:StepUpActions` (default = full list).

## Implementation notes (PLANNED)

- `AuthService` gains: `LoginAsync` challenge branch, `ForgotPasswordAsync`, `ResetPasswordAsync`, `RequestStepUpAsync(action)`, `VerifyStepUpAsync`, `DeleteAccountAsync`.
- `IAuthRepository` gains: `FindUserByEmailAsync`, `RevokeAllFamiliesAsync(userId)`, `SoftDeleteAccountAsync(userId)` — single-transaction, SQL-parameterized.
- `AuthController`: new handlers; `RequireStepUp` reads the short-lived step-up JWT (`Security:StepUpTtlSeconds`, default 300) and checks `action` against the allow-list.
- Branded email: one template per purpose in `BrandedEmailTemplate` (`delete_account` and `step_up` explain the action and the 10-minute window; `security` sender).
- Postman folder 01 (Auth) gains: login-challenge, forgot/reset, delete-account, step-up cases; folder 14 asserts `Auth.*` AuditLogs rows (post-20).

## Separation of Concerns

Controllers + `RequireStepUp` guard: `Griot.Api` · orchestration (challenge/verify/revoke): `Griot.Application` · persistence + SQL: `Griot.Infrastructure`. New migration `AddCriticalActionOtp` adds `Users.DeletedAt` (soft-delete marker); `OtpChallenges` already fits all purposes.

## Docker & Deploy

No new containers. New env: `Security:StepUpTtlSeconds` (300), `Security:StepUpActions` (JSON allow-list).

## Out of scope

TOTP / passkeys (enum reserves `Totp`) · admin session UI · OTP for AI principals (never allowed).

## Acceptance Criteria (all PENDING)

- [ ] Login with `TwoFactorMethod=EmailOtp` → 202 challenge; `otp/verify(login_2fa)` returns the token pair
- [ ] Forgot-password returns uniform 202; reset with valid code re-hashes + revokes all families and invalidates other sessions
- [ ] Delete account with step-up → 204; all refresh tokens revoked; memberships gone; login afterwards 401
- [ ] Guarded route without step-up → 403; expired/mismatched claim → 401/409
- [ ] 5 bad verify attempts → 429 lockout; 3 request/15 min → 429 `Retry-After`
- [ ] Service-token OBO caller → 403 on every route in this spec (xUnit)
- [ ] Audit rows present per event once backend 20 is implemented (Postman folder 14 + xUnit)

## Verification

```bash
cd backend && dotnet build --no-incremental --nologo -v minimal
GRIOT_RUN_SQL_TESTS=1 dotnet test --no-restore --nologo -m:1
python3 ../scripts/check-contract-sync.py
```

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.