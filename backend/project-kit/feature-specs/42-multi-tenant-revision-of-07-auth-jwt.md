# Feature 42 — Multi-Tenant Revision of Feature 07 (Auth: JWT + Argon2 + Redis) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 07; the original spec 07 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The **delta** implemented spec 07 needs for the tenant wave: JWT **v2 claims** (`name`, `org`, `role`, `perms`), the **`POST /api/auth/select-organization`** active-org switch, **SuperAdmin bootstrap**, and the production **signing-key rotation** policy. The owning spec for the full v2 contract is **spec 30** — this file records the revision of implemented spec 07's surface only; `docs/api/auth-contract.md` remains the implemented + PLANNED reference.

## Dependencies

- Specs 29 (Organizations/OrganizationMembers), 31 (role → `perms` mapping)
- **Spec 30 (owner of the v2 auth contract)** — implemented spec 07 (auth core ✅, refresh rotation/family revoke unchanged)
- Spec 32 (SuperAdmin bootstrap consumption)

## Context To Read First

- `docs/api/auth-contract.md` (implemented contract + this wave's PLANNED section)
- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §4 (opaque refresh rationale)
- `.agents/skills/jwt-{decode,encode,validate}/SKILL.md`
- Original spec: `backend/project-kit/feature-specs/07-auth-jwt-argon2-redis.md`

## Agent Skills To Use

- `backend/.agents/skills/jwt-argon2-auth/SKILL.md`
- Root `.agents/skills/jwt-decode/SKILL.md`, `jwt-validate/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Application/Services/TokenService.cs` (v2 claim builder), `AuthService.cs` (refresh re-derivation, org switch)
- `Griot.Api/Controllers/AuthController.cs` (`select-organization`, `organizations` list routes)
- `Griot.Api/Program.cs` (JWT validation update: `org` optional for SuperAdmin)
- SuperAdmin bootstrap hosted task; key-rotation runbook section in `docs/api/auth-contract.md`

## Implementation Notes

- Access-token claim set v2: `sub, email, name, jti, iss=Griot, aud=GriotClients, org, role, perms` (space-separated permission keys) — stamped from `OrganizationMembers` + `Roles` at issue time; refresh tokens **stay opaque 64-hex (not a JWT, by design)** and re-derive v2 claims on rotation so role changes land within one refresh, never mid-access-token.
- `POST /api/auth/select-organization {organizationId}` → **200** + a new token pair carrying the new `org`/`role`/`perms`; **403** when the caller is not an active member; **404** unknown org. `GET /api/auth/organizations` lists the caller's memberships.
- SuperAdmin bootstrap: on startup, if `SUPERADMIN__EMAIL` has no user, create/upgrade it idempotently (audit event); SuperAdmin tokens carry `role=super_admin` and **may omit `org`** (platform-wide); `PlatformRole=SuperAdmin` set on the user.
- JWT validation: `org` claim required for tenant principals, optional for `super_admin`; the claim-handler populates `ITenantContext` consumed by spec 39's middleware and spec 37's filters.
- Signing-key policy: production `JWT__Key` ≥ 64 chars (512 bits), CSPRNG-generated, stored only in the platform secret store (Railway env; `appsettings.Local.json` git-ignored); **dual-key rotation window** documented (validate with old + new key during the overlap, re-issue on next refresh, retire old key) — the rotation runbook belongs to spec 30 and is referenced, not duplicated.
- Argon2 password hashing, Redis OTP gates, refresh rotation/family-revoke, replay revocation: **all unchanged** from implemented 07 — the revision adds claims and routes, never touches the token mechanics.
- Email-OTP 2FA routes (`otp/request`, `otp/verify`), register-201-after-persistence, 204 logout, 401-malformed-refresh: unchanged (verified by the existing auth test suite still passing).
- Step-up tokens and password flows are spec 23's surface — explicitly out of scope here.
- Postman auth folder updated to decode + assert `name`/`org`/`role`/`perms` (spec 43 revision).

## Separation of Concerns

`TokenService` owns claim construction; middleware only validates and binds the tenant context; controllers stay thin. No auth logic in GraphQL (spec 40). The original spec 07 file documents the implemented base contract and is never re-opened.

## Acceptance Criteria

- [ ] Access token payload contains `name`, `org`, `role`, `perms`; decodes **and verifies** at jwt.io with the real configured key
- [ ] Refresh token remains opaque 64-hex; rotation + family-revoke tests from implemented 07 still green
- [ ] `select-organization` re-issues with the new org role; non-member 403; unknown org 404
- [ ] SuperAdmin bootstrap idempotent; SuperAdmin token valid without `org`
- [ ] Production key-length validation rejects <64-char keys; dual-key rotation documented
- [ ] `dotnet build` + `dotnet test` green (existing auth suite unbroken)

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.