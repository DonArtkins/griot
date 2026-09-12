# Feature 31 — RBAC v2: System + Custom Company Roles & Permissions (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **✅ IMPLEMENTED (2026-09-12) on `feature/backend/31-rbac-roles-custom-permissions`**

## What This Delivers

The permission engine: five system roles seeded per company (**Owner/Admin** = the company owner who can view ALL projects under that company only; **ProjectManager** = manages only their assigned projects; **Member**; **Client**) plus **custom roles Admin/PM create inside their own company** after logging in, composed from the fixed permission catalogue (`docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §3). The platform **SuperAdmin** is NOT a seeded org role — it is `Users.PlatformRole` (spec 29/30) and resolves first in every permission decision (tenancy-guide §3 precedence), then the organization role.

## Dependencies

- Spec 29 (`OrganizationMembers`, `Roles`) · spec 30 (`perms` claim).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §3 · `diagrams/erd/multi-tenant-amendment.md`
- Existing `WorkspaceRole` policy code (the model stays: same policy code shape per role)

## Files Owned

- `Griot.Application/Services/RoleService.cs`, `Authorization/PermissionCatalogue.cs`, `IPermissionService.cs`
- `Griot.Api/Controllers/RoleController.cs` + GraphQL types

## Implementation Notes

- System roles are seeded rows (`IsSystem = true`) per organization — implemented as an idempotent **startup backfill** (`IRoleService.BackfillSystemRolesAsync`, Program.cs) that also serves spec 32's onboarding via `EnsureSystemRolesAsync(orgId)`; they cannot be edited or deleted.
- Custom roles: `POST /api/organizations/{orgId}/roles` (Admin with `org.roles.manage`, or PM with the same permission granted via an existing custom role) → `Roles` row with permission keys from the catalogue only; unknown/escalating keys → 400.
- Enforcement: `[RequirePermission("perm:{key}")]` attribute (REST) + HotChocolate field-level `perm:{key}` policies (GraphQL) — every decision is evaluated server-side by `PermissionService` against the DB (re-resolved membership + persisted `Roles.Permissions`), never against the JWT `perms` claim (claims are an optimization, never the policy source of truth).
- Role changes take effect on next refresh (spec 30); force-revoke endpoint for immediate effect (revokes ALL refresh families of the affected users via `IAuthRepository.RevokeAllFamiliesAsync`).
- ProjectManager scoping: `project.manage` is granted at the role level (never `org.projects.view_all`); per-project assignment scoping remains enforced by the spec-14 project-membership checks in `ProjectService`.
- **Approval (2026-09-12):** custom-role DELETE cascades affected members back to the system `Member` role (then revokes their refresh families) — user-approved design.

## Implemented Surface (2026-09-12)

- `Griot.Application/Authorization/PermissionCatalogue.cs` — fixed keys, `IsKnown/Join/Parse/PermsForSystemRole/All`.
- `Griot.Application/Services/RoleService.cs` (+ `IRoleService`) — seeding/backfill, custom-role CRUD (catalogue-only, no-escalation, duplicate-name 409), `SetMemberRoleAsync` (`Admin`/`custom:{roleId}`; self-change 403; Owner demotion needs an Owner), `ForceRevokeAsync`, delete cascade → Member.
- `Griot.Application/Services/PermissionService.cs` (+ `IPermissionService`) — server-side `HasPermissionAsync`/`EffectivePermissionsAsync`/`GetEffectiveRoleAsync` (DB truth; SuperAdmin-first; fail-closed).
- `Griot.Infrastructure/Repositories/RoleRepository.cs` (+ `IRoleRepository`) — tenant-filtered request path + `IgnoreQueryFilters` platform backfill; `IAuthRepository.RevokeAllFamiliesAsync` (all-families revoke).
- `Griot.Api/Auth/RequirePermissionAttribute.cs` + `Griot.Api/Authorization/` (policy provider + handler) — one `perm:{key}` policy per catalogue key.
- `Griot.Api/Controllers/RoleController.cs` — `GET/POST /api/organizations/{orgId}/roles`, `PUT/DELETE .../roles/{roleId}`, `POST .../roles/{roleId}/revoke`, `PUT .../members/{memberId}/role` (AI OBO calls forbidden on every write route).
- `Griot.Api/GraphQL/Types/RoleType.cs` + `organizations`/`organizationRoles`/`organizationMembers` fields on `GriotQuery` — role reads gated by `perm:` policies.
- Program.cs: `IPermissionService`/`IRoleService`/`IRoleRepository` DI, `AddPermissionPolicies`, startup backfill (logged, never aborts startup).
- 23 new tests in `tests/Griot.Tests/Rbac/RoleServiceTests.cs` (seeding idempotency, catalogue validation/escalation, duplicate-name 409, custom delete cascade, force-revoke, member-role guards, permission fail-closed). Build 0W/0E; full suite **187 passed / 8 SQL-skipped / 0 failed**.

## Acceptance Criteria

- [x] Five system roles seeded per company; undeletable/uneditable — `BackfillSystemRolesAsync` idempotent (`IsSystem=true` rows; update/delete → 403)
- [x] Admin/PM create a custom role limited to catalogue keys; cross-org role CRUD returns 403/404 — catalogue validation + `TenantGuard` fail-closed cross-tenant
- [x] `[RequirePermission]` + GraphQL policy enforce; bypass attempts fail integration tests — server-side `PermissionService` re-check (DB truth)
- [x] Role downgrade reflected after refresh; force-revoke works — refresh re-resolves `perms` from the DB; `RevokeAllFamiliesAsync` immediate effect
- [x] `dotnet build` + `dotnet test` green — 0W/0E; 187 passed / 8 skipped / 0 failed (2026-09-12)
