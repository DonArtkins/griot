# Feature 30 — Auth & JWT v2: Role + Tenant Claims, Secure Signing (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **✅ IMPLEMENTED — 2026-09-12** on `feature/backend/30-auth-jwt-v2` (extends implemented spec 07; original spec 07 file stays untouched)

## What This Delivers

JWT v2: the 15-minute access token now carries the caller's identity **with their role and tenant** — `name`, `org` (active OrganizationId), `role` (effective role incl. `super_admin` / `custom:{roleId}`), `perms` (permission keys) — so every surface (web/mobile/GraphQL/AI OBO) can authorize without a per-request DB role lookup. Opaque refresh tokens and rotation/family-revoke are **unchanged by design** (see rationale in `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §4: the refresh token is not a JWT and must stay opaque; jwt.io decoding it is impossible *on purpose*).

## Dependencies

- Spec 29 (Organizations exist) · spec 31 (role → permission mapping) · implemented spec 07 (auth core ✅).

## Context To Read First

- `docs/api/auth-contract.md` (implemented contract + this spec's PLANNED section)
- `.agents/skills/jwt-{decode,encode,validate}/SKILL.md` (newly installed via `npx skills add jsonwebtoken/jwt-skills`)
- `backend/.agents/skills/jwt-argon2-auth/SKILL.md`

## Files Owned

- `Griot.Application/Services/TokenService.cs` (claim builder), `Griot.Application/Services/AuthService.cs`
- `Griot.Api/Controllers/AuthController.cs` (`select-organization` route), `docs/api/auth-contract.md`

## Setup / Initialization

```bash
# env: JWT__Key (>=64 chars / 512 bits, CSPRNG-generated, Railway secret store),
#      JWT__Issuer=Griot, JWT__Audience=GriotClients, SUPERADMIN__EMAIL, SUPERADMIN__PASSWORD (first-boot bootstrap)
```

## Implementation Notes

- Claim set v2: `sub, email, name, jti, iss=Griot, aud=GriotClients, org, role, perms` (space-separated keys). Claims are stamped from `OrganizationMembers` + `Roles` at issue time; refresh re-derives them (role changes take effect within one refresh, never mid-access-token).
- `POST /api/auth/select-organization {organizationId}` → 200 + new token pair with the new `org` claim; 403 when the user is not an active member; 404 unknown org. `GET /api/auth/organizations` lists the caller's memberships.
- SuperAdmin bootstrap: on startup, if `SUPERADMIN__EMAIL` has no user, create/upgrade it idempotently (audit event). SuperAdmin tokens carry `role=super_admin` and may omit `org`.
- Signing key policy: production `JWT__Key` ≥ 512 bits, generated with a CSPRNG, stored only in the platform secret store; rotation runbook (dual-key validation window) in this spec. **The jwt.io `a-string-secret-at-least-256-bits-long` string is jwt.io's placeholder — signature verification succeeds only with the real configured key.**
- AI OBO tokens (spec 09) remain separate and untouched.

## Separation of Concerns

`TokenService` owns claim construction; middleware only validates; controllers stay thin. No auth logic in GraphQL.

## Docker & Deploy

No new container. `JWT__Key`/`SUPERADMIN__*` documented for Railway env.

## Out of Scope

Cookie transport for web 05 (existing pending item), password flows (spec 23), payments.

## Implementation record (2026-09-12, this branch)

`TokenService` (sole claim builder: `sub/email/name/jti/org?/role/perms`, HS256 15-min) + `PermissionCatalogue`/`RoleSelection` (guide §3 precedence, `log.read_tier` never stamped on system roles) + `AuthService` session resolution (explicit org pin / single-membership auto-pick / platform view; SuperAdmin platform authority incl. org selection without a member row) + `GET /api/auth/organizations` / `POST /api/auth/select-organization` (404/403 fail-closed; presented family revoked after mint) + stateless `RefreshRequest.OrganizationId` pin (family id is replay-chain only) + startup key-policy fail-fast (32-byte dev / 64-byte prod) + `JWT__Key_Previous` rotation window + idempotent SuperAdmin bootstrap (audit-logged). Tests: 30 AuthServiceTests green (11 new: org list/select/bootstrap/key-policy/role-perms). Verified: `dotnet build` 0W/0E; full suite green (see tracker).

## Acceptance Criteria

- [x] Organization-session access tokens contain `name`, `org`, `role`, `perms`; platform-only sessions may omit `org`; tokens decode and verify with the real key
- [x] Refresh token remains 64-hex opaque; rotation + family revoke tests still green
- [x] `select-organization` re-issues with the new org role; non-member gets 403
- [x] SuperAdmin bootstrap idempotent; key-length validation rejects <64-char production keys
- [x] `dotnet build` + `dotnet test` green
