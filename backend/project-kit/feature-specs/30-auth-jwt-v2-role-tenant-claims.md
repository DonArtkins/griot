# Feature 30 — Auth & JWT v2: Role + Tenant Claims, Secure Signing (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (extends implemented spec 07; original spec 07 file stays untouched)

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

## Acceptance Criteria

- [ ] Access token payload contains `name`, `org`, `role`, `perms`; decodes+verifies at jwt.io with the real key
- [ ] Refresh token remains 64-hex opaque; rotation + family revoke tests still green
- [ ] `select-organization` re-issues with the new org role; non-member gets 403
- [ ] SuperAdmin bootstrap idempotent; key-length validation rejects <64-char production keys
- [ ] `dotnet build` + `dotnet test` green
