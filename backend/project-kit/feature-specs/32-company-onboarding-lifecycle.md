# Feature 32 — Company Onboarding & Platform Management (SuperAdmin) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **IMPLEMENTED 2026-09-12** on
`feature/backend/32-company-onboarding-lifecycle` (build 0W/0E; spec-32 suite 32/32
green — 25 unit + 7 SQL, SQL gated by `GRIOT_RUN_SQL_TESTS=1`)

## What This Delivers

The SuperAdmin (the operator/owner of Griot) **onboards companies** and manages them platform-wide: create company → provision owner (Owner) user via Brevo invite → seed system roles → default workspace → audit event. Also: list/search all companies, suspend/reactivate, transfer ownership, plan metadata.

## Dependencies

- Specs 29 (schema), 30 (SuperAdmin bootstrap), 31 (roles), 12 ✅ (Brevo invite mail), 20 ✅ (audit).

## Files Owned

- `Griot.Application/Services/OrganizationLifecycleService.cs` + `Griot.Application/Interfaces/Services/IOrganizationLifecycleService.cs`
- `Griot.Application/Interfaces/Repositories/IOrganizationLifecycleRepository.cs` + `Griot.Infrastructure/Repositories/OrganizationLifecycleRepository.cs`
- `Griot.Application/DTOs/Organizations/OrganizationDtos.cs`
- `Griot.Api/Controllers/OrganizationController.cs` (platform routes under `/api/organizations`)
- `Griot.Application/Email/BrandedEmailTemplate.cs` (org-invite renderer + subject)
- Tests: `tests/Griot.Tests/Organizations/OrganizationLifecycleServiceTests.cs` (unit) + `OrganizationLifecycleSqlTests.cs` (SQL, `GRIOT_RUN_SQL_TESTS=1`)

## Implementation Notes

- `POST /api/organizations {name, slug, ownerEmail, plan}` (SuperAdmin only; `plan` must be a defined `OrganizationPlan` value — undefined numeric values are rejected with a `Validation` error before any write) → 201 `{organization, ownerInvite}`; the owner accepts via `POST /api/organizations/invites/{token}/accept` (Brevo branded template `RenderOrganizationInviteEmail`, `OrganizationInvites`, plaintext two-Guid token — same storage convention as workspace invites, 7-day expiry; the invite-accept route is guarded by `ForbidIfAiCall()` so AI OBO principals get 403).
- Seeding is transactional: `Organizations` + owner membership (`Role = Owner`, `Status = Invited`) + 5 system `Roles` (shared `RoleService.SystemRoleNames`/`SystemRolePermissionString` statics so seeded permissions can never diverge from spec 31) + default `Workspaces` (`{slug}-default` — globally unique slug index) + `OrganizationLifecycleEvents(Onboarded)` + AuditLogs row, serialized on the unique Slug key (UPDLOCK/HOLDLOCK probe, `RoleRepository` lock pattern). `Organization.OwnerId` is a non-nullable FK (spec 29), so `ownerEmail` must already be a registered account (422-style `Validation` error otherwise).
- Owner membership seeds `Invited` and flips `Active` on invite accept (email match, concurrent-accept re-check under the org row lock) — the owner lands in the default workspace via company membership; claims re-resolve on the next refresh (spec 30 session resolution).
- `GET /api/organizations` (SuperAdmin; paginated per spec 18 contract, name/slug search), `GET /api/organizations/{id}` (SuperAdmin or that org's Active Owner/Admin — own org only), `POST …/{id}/suspend` → `Status = Suspended` + all writes in that org return 403 `org_suspended` via the spec-29 `TenantGuard` gate (reads + auth stay allowed — enforced service/middleware-side, NOT controller-side), `POST …/{id}/reactivate`, `POST …/{id}/transfer-ownership` (new owner must be a different Active member; previous owner demotes to Admin; no forced refresh revocation — claims re-resolve on next refresh), `PUT …/{id}/plan` (metadata only — no payments in this wave).
- Every transition writes `OrganizationLifecycleEvents` **and** `AuditLogs` (spec 20 pipeline) in the same transaction; the transition audit row's `After` value records the resulting status **and** the suspension/reactivation reason, while the lifecycle event keeps its own `{from, to, reason}` payload. Platform paths run `IgnoreQueryFilters` + explicit organization targeting (the SuperAdmin session has no tenant scope; spec-29 filters fail closed without one). Non-SuperAdmin mutations fail closed 403 on both `ITenantContext.IsSuperAdmin` (service) and the JWT `role` claim (controller, `AuthController` mirror).
- Brevo invite mail is best-effort AFTER the durable commit (spec 12: delivery never blocks state; the token also travels in the 201 body). New email caller for spec 12: org-invite acceptance (no new config keys — reuse `SITE_URL`).

## Acceptance Criteria

- [x] SuperAdmin creates a company; owner receives Brevo invite; on accept the owner is `Active` and lands in the default workspace — SQL `Onboard_SeedCommitsAllRowsTogether` + `InviteAccept_EndToEnd_ActivatesOwner_SecondAccept_Conflicts`
- [x] Seeding transactional — a failure leaves zero partial rows — SQL `Onboard_MidSeedFailure_LeavesZeroPartialRows` (unique-slug blocker mid-seed) + `Onboard_DuplicateSlug_SerializesToOneConflict`
- [x] Suspend blocks writes (403 `org_suspended`) but keeps reads/auth; reactivate restores — SQL `Suspend_ThenTenantWriteGate_FailsClosedWithOrgSuspended` (`TenantGuard.RequireOrganization` asserts the gate; enforced by spec-29 middleware/TenantGuard on tenant writes)
- [x] Non-SuperAdmin gets 403 on all platform routes; company Admin sees only their org — unit `Create_NonSuperAdmin_Forbidden_NothingSeeded`, `List_NonSuperAdmin_Forbidden`, `Suspend_NonSuperAdmin_Forbidden`, `GetById_CompanyAdminOfOwnCompany_Allowed`, `GetById_MemberOfOtherCompany_Forbidden` + SQL `InviteAccept_EmailMismatch_Forbidden`, `Transfer_EndToEnd_RewritesOwnerAndDemotesPrevious`
- [x] `dotnet build` + `dotnet test` green — build 0W/0E; full SQL-enabled suite run 2026-09-12 (see backend progress tracker)

