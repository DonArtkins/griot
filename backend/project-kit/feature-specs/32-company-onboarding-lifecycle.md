# Feature 32 — Company Onboarding & Platform Management (SuperAdmin) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

The SuperAdmin (the operator/owner of Griot) **onboards companies** and manages them platform-wide: create company → provision owner (Admin) user via Brevo invite → seed system roles → default workspace → audit event. Also: list/search all companies, suspend/reactivate, transfer ownership, plan metadata.

## Dependencies

- Specs 29 (schema), 30 (SuperAdmin bootstrap), 31 (roles), 12 ✅ (Brevo invite mail), 20 ✅ (audit).

## Files Owned

- `Griot.Application/Services/OrganizationLifecycleService.cs`
- `Griot.Api/Controllers/OrganizationController.cs` (platform routes under `/api/organizations`)

## Implementation Notes

- `POST /api/organizations {name, slug, ownerEmail, ownerDisplayName, plan}` (SuperAdmin only) → 201 `{organization, ownerInvite}`; owner accepts via existing invite-token flow (Brevo branded template, `OrganizationInvites`).
- Seeding is transactional: `Organizations` + owner membership (`Role = Owner`) + 5 system `Roles` + default `Workspaces` + `OrganizationLifecycleEvents(Onboarded)`.
- `GET /api/organizations` (SuperAdmin; paginated per spec 18 contract), `GET /api/organizations/{id}` (SuperAdmin or that org's Admin — own org only), `POST …/{id}/suspend` → `Status = Suspended` + all writes in that org return 403 `org_suspended` (reads allowed), `POST …/{id}/reactivate`, `POST …/{id}/transfer-ownership`.
- Every transition writes `OrganizationLifecycleEvents` **and** `AuditLogs` (spec 20 pipeline) in the same transaction.

## Acceptance Criteria

- [ ] SuperAdmin creates a company; owner receives Brevo invite; on accept the owner is `Active` and lands in the default workspace
- [ ] Seeding transactional — a failure leaves zero partial rows
- [ ] Suspend blocks writes (403 `org_suspended`) but keeps reads/auth; reactivate restores
- [ ] Non-SuperAdmin gets 403 on all platform routes; company Admin sees only their org
- [ ] `dotnet build` + `dotnet test` green
