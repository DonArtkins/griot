# Backend Feature Spec 23 — Critical-Action OTP & Step-Up Verification [own-stack]

**Status:** PLANNED — user-approved scope extension of spec 07's email-OTP (research: `research/ai-features-research.md` §1, §3.6; ADR-003 constraint 6 already reserves the `TwoFactorMethod` switch and the step-up session design). No production code exists yet; this branch creates the spec and contract-syncs every dependent doc. One feature branch: `feature/backend/23-critical-action-otp-step-up`.

## Goal

Turn the existing email-OTP (`email_verify` / `login_2fa` / `password_reset`) into a full **critical-action policy** so that every high-blast-radius action is verified by a Brevo-delivered code, and no AI principal can ever touch this surface. Canonical OTP matrix (all auth actions OTP-gated): `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §8; account-deletion request workflow (reason + SuperAdmin review + export + 30-day purge, superseding instant delete): guide §9; destructive intent/confirm/cancel + countdown safety contract: guide §10.

1. **Register** — auto-sent `email_verify` OTP + admin notice (implemented, spec 07).
2. **Login 2FA** — `Users.TwoFactorMethod = EmailOtp` → login challenges *before* any token is issued (guide §8: all logins OTP-capable, enforced for EmailOtp users).
3. **Forgot / reset password** — `password_reset` OTP gates the reset; reset revokes every other session (guide §8: uniform 202 forgot, Argon2 re-hash + revoke-all-families reset).
4. **Delete account — REQUEST WORKFLOW (supersedes instant delete; guide §9 is canonical):** reason (10–2000 chars) + `delete_account` OTP → step-up → `POST /api/auth/account/deletion-request` (202) → SuperAdmin approve/reject → export download (`Account.ExportData` step-up) → 30-day `PurgeScheduled` window with blast-radius notices → purge job deletes only the user's chain (re-home/tombstone, no orphans). The v1 `DELETE /api/auth/account` immediate-204 shape below is retained only as the superseded baseline; implementation follows guide §9.
5. **Major operations** — step-up OTP (purpose `step_up`, action allow-list) for a curated risk list (guide §8 matrix + §10 intent/confirm/cancel + 10-s countdown contract for every destructive operation).

## Non-negotiable security rules (research §1 + ADR-003)

- 6-digit codes from a crypto-secure RNG (`RandomNumberGenerator`); stored as HMAC-SHA256 with `Otp:Pepper`; never logged or returned; constant-time compare; 10-minute expiry; 5-attempt lockout per challenge (`OtpChallenges.AttemptCount`).
- Redis `ratelimit:otp:request:{email}` 3/15 min + API global limiter 100/min/caller; lockout returns 429 with `Retry-After`.
- Every request/verify/attempt writes an `AuditLogs` row (`Action ∈ {Auth.Otp.Request, Auth.Otp.Verify, Auth.Otp.Failed, Auth.StepUp.Issued, Auth.StepUp.Used}` — with backend spec 20).
- **AI is permanently 403 on this whole surface.** Service-token OBO callers (spec 09) never request/verify OTP and never change MFA settings (research §3.6: auth stays human-gated, full stop). **Implementation (review fix):** one shared `RejectAiOnAuthSurface()` guard in `AuthController` runs BEFORE the rate limiter and any service call on `login`, `otp/request`, `otp/verify`, `forgot-password`, `reset-password` and `account` routes — 403 for AI OBO callers, normal human behavior untouched; xUnit asserts 403 per route.

## Dependencies

- Spec 07 (OTP endpoints, pepper, Redis gates, `TwoFactorMethod` enum) — implemented.
- Spec 12 (Brevo Email — the single verified sender `Brevo:FromEmail` carries every OTP template; 2026-09-11: `Brevo:Senders:<Key>` profile map removed).
- Spec 20 (audit rows for every auth/step-up event; this spec's tests are part of 20's trail).
- Spec 04/05 (guarded REST/GraphQL routes).

## Contract changes (PLANNED — additive or 202-on-challenge)

**🚨 SUPERSESSION — HARD RULE, NO EXCEPTIONS (2026-09-11 contract sync).**
The v1 `DELETE /api/auth/account` route (instant soft-delete → 204 + `Users.DeletedAt` + revoke tokens) is **REMOVED from code** on the `feature/backend/23-critical-action-otp-step-up` branch. Any REST or GraphQL caller hitting the old handler receives **HTTP 410 Gone** with a pointer to the new request workflow. The **ONLY** account-deletion surface is the request→review→export→30-day-purge workflow in [MULTI-TENANCY-GUIDE.md §9](file:///home/artkins/sababisha/projects/gtp/griot/docs/multi-tenancy/MULTI-TENANCY-GUIDE.md#L123-L247). xUnit test: `OldDeleteRoute_Returns410Gone` passes before any other account-deletion AC is run. Implementation order: step-up infrastructure → forgot/reset-password → login 2FA → deletion-request workflow (§9). AC below (line 74) is updated to reflect the new workflow, not the old instant delete.

| Route | Auth | Behavior |
|---|---|---|
| `POST /api/auth/login` (modified) | public | `TwoFactorMethod == EmailOtp` → **202** `{twoFactorRequired:true, purpose:"login_2fa"}` without tokens; otherwise 200 as today |
| `POST /api/auth/otp/request` (extended) | public | new purposes: `delete_account`, `step_up` (optional `action` param) |
| `POST /api/auth/otp/verify` (extended) | public | `login_2fa` success → **200 `{verified:true, accessToken, refreshToken, user}`** (single round-trip login); `step_up`/`delete_account` success → **200 `{verified:true, stepUpAccessToken}`** — 5-min JWT with claims `stepup=true`, `purpose`, `action`, `challengeId`, `org?` |
| `POST /api/auth/forgot-password` (new) | public | `{email}` → **202** (fires `password_reset` OTP; uniform 202 even for unknown email — no user enumeration) |
| `POST /api/auth/reset-password` (new) | public | `{email, code, newPassword}` → verify `password_reset`, Argon2 re-hash, revoke ALL refresh families (every session, every family) → **200** |
| `DELETE /api/auth/account` (v1 — REMOVED, returns 410 Gone) | — | **REMOVED from implementation.** Controller stub returns HTTP 410 with `{error:"instant_delete_superseded", message:"Use POST /api/auth/account/deletion-request instead.", docs:"/docs/multi-tenancy#9-account-deletion"}`. Old instant-204 behavior is NEVER invoked. |
| `POST /api/auth/account/deletion-request` (new — guide §9 CANONICAL) | bearer + `delete_account` step-up | `{reason (10–2000 chars), stepUpAccessToken}` → validates claim + reason + no open request (partial unique index) → creates `AccountDeletionRequests(Requested)` → **202** `{requestId, status:"DeletionRequested", requestedAtUtc, scheduledPurgeAtUtc}`. `scheduledPurgeAtUtc = requestedAtUtc + Deletion.PurgeWindowDays (default 30)`. Read-mostly 403 §9f activates after successful insert. |
| `POST /api/auth/account/deletion-request/{id}/cancel` (new — guide §9) | bearer requester only | cancel while `Status = Requested` (before SuperAdmin decides) → **200** `{cancelled:true, status:"Cancelled"}`, audit row, read-mostly deactivated, 7-day cooling off before new request |
| `GET /api/admin/account-deletion-requests` (new — guide §9 + spec 18 pagination) | SuperAdmin + pagination | paginated list, filterable by status, sorted `RequestedAt` desc → **200**. Query params: `page, size, status?` |
| `POST /api/admin/account-deletion-requests/{id}/approve` (new — guide §9c–§9e) | SuperAdmin + `RequireStepUp("AccountDeletion.Decide")` | Body: `{note? (0–2000), scheduledPurgeAtUtc? (optional override, min=decisionAt+MinPurgeWindowDays, max=decisionAt+90d)}` → §9b clamp formula applied → builds export bundle (§9d, 1h SLA) → fires blast-radius fan-out (§9e, best-effort, 25-recipient cap → digest) → `Status = ExportAvailable` → `PurgeScheduled` → **200** `{approved:true, exportAvailable:true, scheduledPurgeAtUtc, fanOutRecipients:N, fanOutFailures:M}` |
| `POST /api/admin/account-deletion-requests/{id}/reject` (new — guide §9c) | SuperAdmin + `RequireStepUp("AccountDeletion.Decide")` | Body: `{note (required, 10–2000 chars, communicated to user)}` → `Status = Rejected`, fires Brevo + in-app notification to requester, audit row, 7-day cooling-off → **200** |
| `GET /api/auth/account/export` (new — guide §9d) | bearer + `RequireStepUp("Account.ExportData")` (OTP step-up) | streams zip bundle (manifest + per-entity JSON + signed attachment links) as `application/octet-stream`, filename `griot-user-export-{userId}-{utcNow}.zip`; valid only while `Status ∈ {ExportAvailable, PurgeScheduled}`. Background-rebuilt every 7 days to reflect latest state. Each download → AuditLogs row. |

### Step-up guarded action list (v1)

`Workspace.Delete` · `Workspace.TransferOwnership` · `Member.RoleChange` · `Member.Remove` · `Invite.Create` · `Project.Delete` · `Board.Delete` · `Column.Delete` · `Task.BulkStatus` · `Task.Delete` · `Comment.Delete` · `Attachment.Delete` · `Account.Delete` · `Account.ExportData` · `AccountDeletion.Decide`

Each maps to its existing handler plus a `RequireStepUp(action)` guard that validates the 5-minute `stepup` claim allow-list before the domain call (403 without it; 409 on action mismatch). Allow-list configurable via `Security:StepUpActions` (default = full list). Every destructive action additionally follows the guide §10 safety contract: server-issued intent (`confirmAfterUtc = now+10 s`, 409 `confirm_too_early` on early confirm) → confirm inside one transaction with rollback → cancel honored only before commit starts (409 `already_committing` after; client disables Cancel at `T-1s` and on confirm-send) → `DB.`-prefixed trigger rows + same-transaction app audit.

## Implementation notes (PLANNED)

- `AuthService` gains: `LoginAsync` challenge branch, `ForgotPasswordAsync`, `ResetPasswordAsync`, `RequestStepUpAsync(action)`, `VerifyStepUpAsync`, `DeleteAccountAsync`, plus the guide-§9 workflow: `RequestAccountDeletionAsync(reason, stepUp)`, `CancelAccountDeletionAsync`, `ReviewAccountDeletionAsync(approve/reject, note)`, `GetAccountExportAsync` (all audit-logged; purge itself runs on the spec-33-pattern background job).
- `IAuthRepository` gains: `FindUserByEmailAsync`, `RevokeAllFamiliesAsync(userId)`, `SoftDeleteAccountAsync(userId)` — single-transaction, SQL-parameterized — plus `AccountDeletionRequests` persistence (`CreateDeletionRequestAsync`, `UpdateDeletionRequestAsync`, `FindOpenDeletionRequestAsync`) backing the guide-§9 workflow.
- `AuthController`: new handlers; `RequireStepUp` reads the short-lived step-up JWT (`Security:StepUpTtlSeconds`, default 300) and checks `action` against the allow-list.
- Branded email: one template per purpose in `BrandedEmailTemplate` (`delete_account` and `step_up` explain the action and the 10-minute window; single verified sender per spec 12).
- Postman folder 01 (Auth) gains: login-challenge, forgot/reset, delete-account, step-up cases; folder 14 asserts `Auth.*` AuditLogs rows (post-20).

## Separation of Concerns

Controllers + `RequireStepUp` guard: `Griot.Api` · orchestration (challenge/verify/revoke): `Griot.Application` · persistence + SQL: `Griot.Infrastructure`. New migration `AddCriticalActionOtp` adds `Users.DeletedAt` (soft-delete marker); `OtpChallenges` already fits all purposes.

## Docker & Deploy

No new containers. New env: `Security:StepUpTtlSeconds` (300), `Security:StepUpActions` (JSON allow-list).

## Out of scope

TOTP / passkeys (enum reserves `Totp`) · admin session UI · OTP for AI principals (never allowed).

## Acceptance Criteria (all PENDING — supersession note: old instant-delete AC #3 replaced with new workflow AC 3a–3h)

- [ ] 1. Login with `TwoFactorMethod=EmailOtp` → 202 challenge; `otp/verify(login_2fa)` returns the token pair
- [ ] 2. Forgot-password returns uniform 202 (ALWAYS, even for unknown email — no enumeration); reset with valid code re-hashes Argon2 + revokes ALL refresh families and invalidates every other session for that user (xUnit: FamilyId count before reset = after = 0 active families for same user across different logins)
- [ ] 3a. **SUPERSESSION GATE (blocking, must pass first):** Call to `DELETE /api/auth/account` returns **HTTP 410 Gone** with `{error:"instant_delete_superseded", message, docs}` body (xUnit). Old instant-delete controller code path is **REMOVED** (not just deprecated) — code review confirms zero invocations of `SoftDeleteAccountAsync` outside the spec-33 background purge job.
- [ ] 3b. Delete-account request flow: valid OTP `delete_account` → verify → 5-min step-up token → `POST deletion-request {reason (10–2000 chars), stepUpAccessToken}` → 202 with requestId + `DeletionRequested` status + `scheduledPurgeAtUtc = requestedAtUtc + 30d`. Reason <10 or >2000 chars → 400. Missing/expired step-up → 401. Open request exists → 409 (partial unique index + app check both trigger independently).
- [ ] 3c. **§9b purge-window formula (xUnit 4 cases):** (i) request day0, approve day5 → scheduledPurgeAtUtc = day30 (no clamp); (ii) request day0, approve day29 → scheduledPurgeAtUtc = day36 (clamped to approval + 7 = MinPurgeWindowDays); (iii) SuperAdmin override to 91d → clamped to approval + 90d (ceiling); (iv) SuperAdmin override to 6d → clamped to approval + 7d (floor). All 4 tests pass against AuthService directly.
- [ ] 3d. Read-mostly 403 during DeletionPending (xUnit + HTTP): POST create-workspace, PUT task, DELETE comment → ALL return 403 `{error:"account_deletion_pending", message, scheduledPurgeAtUtc}`. GET workspace/list → 200 allowed. POST deletion-request/cancel → 200 allowed. POST otp/verify → 200 allowed. Export download → 200 allowed.
- [ ] 3e. User cancel: `POST cancel` while `Requested` → 200 + `Status = Cancelled` + read-mostly lifted. 7-day cooling-off: new request within 7d → 409 `{error:"cooling_off", retryAfterDays:N}`.
- [ ] 3f. SuperAdmin review: `GET /admin/account-deletion-requests` (page=1,size=10,status="Requested") → paginated list. `POST /approve` without step-up → 403. `POST /reject {note (required)}` → 200 + requester gets email+in-app notification. SuperAdmin can cancel purge while `PurgeScheduled` and BEFORE date → `POST /cancel-purge` → 200 + account reactivated.
- [ ] 3g. Export: After approval, calling `/auth/account/export` with `Account.ExportData` step-up OTP → 200 `application/octet-stream` zip filename matches pattern. Without step-up → 403. Before approval → 404. Re-download 10 days later (during PurgeScheduled) → 200 (re-issuable).
- [ ] 3h. **Blast-radius fan-out (mock Brevo test):** Approve a request where requester owns 2 workspaces with 5 members each (8 distinct unique users) → spec-22 fan-out (mocked) produces 8 Notification rows + 8 queued Brevo emails (or 1 digest ≥ 25 recipients test). Each email body string-contains: (i) requester display name, (ii) exact scheduledPurgeAtUtc ISO date, (iii) the phrase "Save or export any data you need before that date". SuperAdmin sees `fanOutFailures:0` in approve response (mock all succeed).
- [ ] 4. Guarded route WITHOUT step-up → 403; expired step-up claim → 401; action mismatch (claim action=Workspace.Delete used on Project.Delete) → 409 `step_up_action_mismatch` with required/received in body.
- [ ] 5. 5 bad verify attempts on same challenge → 429 lockout; 3 otp-request/15min on same email → 429 with `Retry-After` header.
- [ ] 6. Service-token OBO caller (AI, spec 09) → **403 on EVERY route** in this spec (xUnit per route × 13 = 13 tests, all assert 403 status code). `RejectAiOnAuthSurface()` guard returns BEFORE rate-limiter so Redis counters are NOT incremented by AI callers.
- [ ] 7. Audit rows present per event once backend 20 is implemented (Postman folder 14 + xUnit): `Auth.Otp.Request`, `Auth.Otp.Verify`, `Auth.Otp.Failed`, `Auth.StepUp.Issued`, `Auth.StepUp.Used`, `AccountDeletion.Requested`, `AccountDeletion.Approved`, `AccountDeletion.Rejected`, `AccountDeletion.Cancelled`, `AccountDeletion.Exported`, `AccountDeletion.PurgeScheduled`. Each row has correct ActorId, correct `PayloadJson`, non-null `RequestId` (correlated to ApiLogs row for same HTTP call).

## Verification

```bash
cd backend && dotnet build --no-incremental --nologo -v minimal
GRIOT_RUN_SQL_TESTS=1 dotnet test --no-restore --nologo -m:1
python3 ../scripts/check-contract-sync.py
```

## Multi-Tenant Update (2026-09-11 — PLANNED)

- **Step-up is required for org lifecycle destructive actions (PLANNED additions to the guarded action list):** `Organization.TransferOwnership` and `Organization.OffboardStart` (SuperAdmin), plus the company-Admin destroy actions the tenant wave adds — a suspend/offboard/transfer routed without a valid 5-minute `stepup` claim is 403/409 exactly like the existing v1 list.
- **Step-up tokens gain an `org` claim binding:** a step-up issued while active org A cannot authorize an action in org B — `RequireStepUp` validates purpose + action + org; `select-organization` after issuance invalidates the claim.
- **AI stays permanently 403 on the whole surface (unchanged):** OBO callers never request/verify OTP or touch org lifecycle routes; `RejectAiOnAuthSurface()` guards extend to the new lifecycle endpoints.
- `Auth.*`/`Auth.StepUp.*` audit rows (spec 20) carry `OrganizationId` (spec 51 revision) so every OTP/step-up event answers "which tenant".
- Delete-account flow additionally **revokes `OrganizationMembers` rows** (all orgs) alongside refresh families/invites; reset-password and login-2FA semantics are unchanged by the wave.
- The guarded-action list update is **contract-synced** (this table + `docs/api/auth-contract.md` PLANNED section + Postman folder 01 + `Security:StepUpActions` default) in the same branch.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.