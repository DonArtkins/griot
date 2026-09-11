# Feature 31 — RBAC v2: System + Custom Company Roles & Permissions (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

The permission engine: five system roles seeded per company (**SuperAdmin** = platform operator/owner of Griot; **Admin** = the company owner who can view ALL projects under that company only; **ProjectManager** = manages only their assigned projects; **Member**; **Client**) plus **custom roles Admin/PM create inside their own company** after logging in, composed from the fixed permission catalogue (`docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §3).

## Dependencies

- Spec 29 (`OrganizationMembers`, `Roles`) · spec 30 (`perms` claim).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §3 · `diagrams/erd/multi-tenant-amendment.md`
- Existing `WorkspaceRole` policy code (the model stays: same policy code shape per role)

## Files Owned

- `Griot.Application/Services/RoleService.cs`, `Authorization/PermissionCatalogue.cs`, `IPermissionService.cs`
- `Griot.Api/Controllers/RoleController.cs` + GraphQL types

## Implementation Notes

- System roles are seeded rows (`IsSystem = true`) per organization at onboarding (spec 32); they cannot be edited or deleted.
- Custom roles: `POST /api/organizations/{orgId}/roles` (Admin with `org.roles.manage`, or PM with the same permission granted via an existing custom role) → `Roles` row with permission keys from the catalogue only; unknown/escalating keys → 400.
- Enforcement: `[RequirePermission("project.manage")]` attribute (REST) + HotChocolate field-level policy (GraphQL) reading the `perms` claim; **authorization decisions are re-checked server-side even though the claim exists** (claims are an optimization, never the policy source of truth).
- Role changes take effect on next refresh (spec 30); force-revoke endpoint for immediate effect (revokes refresh families of the affected user+org).
- ProjectManager scoping: `project.manage` evaluated against assigned projects only; Admin's `org.projects.view_all` never crosses the company boundary.

## Acceptance Criteria

- [ ] Five system roles seeded per company; undeletable/uneditable
- [ ] Admin/PM create a custom role limited to catalogue keys; cross-org role CRUD returns 403/404
- [ ] `[RequirePermission]` + GraphQL policy enforce; bypass attempts fail integration tests
- [ ] Role downgrade reflected after refresh; force-revoke works
- [ ] `dotnet build` + `dotnet test` green
