# Feature 31 — RBAC v2: System + Custom Company Roles & Permissions (own-stack)

## Type

Backend feature · multi-tenant wave · current branch:
`feature/backend/31-rbac-roles-custom-permissions`.
Completion evidence is recorded in the [backend tracker](../context/progress-tracker.md).

## What This Delivers

Five immutable system roles per company (Owner, Admin, ProjectManager, Member,
Client), custom roles composed from the fixed permission catalogue, member role
assignment, and force revocation. SuperAdmin is a platform role, not a seeded
company role. REST and GraphQL authorize against current database membership and
permissions; JWT `perms` is display/session metadata.

## Dependencies

- Backend 29: approved Organizations, Roles, OrganizationMembers, and OrganizationInvites schema.
- Backend 30: JWT v2 tenant sessions and refresh-family persistence.
- Backend 20: mandatory mutation audit trail.

## Context To Read First

- [Tenancy guide](../../../docs/multi-tenancy/MULTI-TENANCY-GUIDE.md), section 3.
- [Approved ERD amendment](../../../diagrams/erd/multi-tenant-amendment.md).
- [API surface](../context/api-surface.md) and [RBAC contract](../../../docs/api/rbac-contract.md).

## Skills

Use the root contract-sync, documentation-standards, git-branch-flow, and Context7
skills; use backend EF Core, JWT, and HotChocolate skills for their respective changes.

## Files Owned

- Application: `RoleService`, `PermissionService`, `PermissionCatalogue`, role DTOs and interfaces.
- Infrastructure: `RoleRepository`; shared `AuthRepository.RevokeAllFamiliesAsync`.
- API: `RoleController`, `RequirePermissionAttribute`, permission handler/policies,
  `organizationRoles` GraphQL query, and role types.
- Tests: `tests/Griot.Tests/Rbac/`; Postman role requests.

## Setup / Initialization

Startup calls `BackfillSystemRolesAsync` for active companies with fewer than five
system roles. `EnsureSystemRolesAsync(orgId)` fills partial seeds and is reusable by
spec 32 onboarding. Explicit platform lookups work without an HTTP tenant.
Concurrent seeds serialize on the target organization row. Reserved system-role
names cannot be used by custom roles. Startup failures are logged; failed seeds
roll back and can be retried.

No new package, environment variable, or schema migration is needed.

## Separation of Concerns

Controllers and GraphQL resolvers call application services. Permissions come from
active database membership, with SuperAdmin resolved first. Request role operations
must match the active tenant; inactive organizations reject writes, including
SuperAdmin writes. Organization role powers do not replace project/workspace
membership checks in existing domain operations. Project-wide Admin/PM/client
behavior beyond these role endpoints belongs to the relevant revision/client specs.

Role mutations run inside one repository transaction. An update lock on the existing
organization row serializes name checks, seeding, assignment, deletion, and revocation.
Audit or revocation failure rolls back the operation.

## Implemented Surface

See the [RBAC contract](../../../docs/api/rbac-contract.md) for exact routes and responses.

- `GET/POST /api/organizations/{organizationId}/roles`.
- `PUT/DELETE /api/organizations/{organizationId}/roles/{roleId}`.
- `POST /api/organizations/{organizationId}/roles/{roleId}/revoke`.
- `PUT /api/organizations/{organizationId}/members/{memberId}/role`.
- GraphQL `organizationRoles` reads the active company; no role-management mutations.

Custom creation/update requires `org.roles.manage`; assignment requires
`org.members.manage`. Requested permissions must belong to the enabled catalogue
and the caller's effective set. Unknown/reserved keys return 400; escalation returns
403; duplicate/reserved names return 409. `log.read_tier` remains reserved for spec 25.

Assignment accepts system names or `custom:{roleId}`. Self-changes return 403.
Only Owner/SuperAdmin may assign Owner or change an existing Owner, including via
a custom-role reference. A system role cannot be assigned as a custom-role reference.
Custom roles must belong to the same company, and assignments cannot grant powers
the caller lacks.

**Approved deletion behavior:** deleting a custom role moves all holders (including
inactive members) and referencing invitations to Member, revokes affected users'
refresh families, and records an audit. Force-revoke revokes all refresh families
of active holders. `FamiliesRevoked` counts distinct unrevoked user/family pairs.
Permission changes affect protected routes immediately through database checks;
refresh reissues updated claims. Revoking refresh families does not blacklist
already-issued access JWTs.

## Docker & Deploy

Use the existing backend/SQL Server/Redis topology. Startup backfill runs on each
application start; tenant row locking makes concurrent instances converge. No
Compose, deployment, or production migration change is part of this feature.

## Out of Scope

Company onboarding/offboarding (32/33), ownership-transfer workflow (32), client
portal/handoff (34/35), step-up OTP (23), log tiers (25), and project/workspace
authorization revisions. AI OBO remains denied on role reads and writes.

## Acceptance Criteria

- [x] Five immutable system roles per company; partial and concurrent startup seeds converge. — `RoleServiceTests.EnsureSystemRoles_SeedsFiveRoles_ThenIdempotent`, `BackfillSystemRoles_SkipsOrganizationsAlreadySeeded`; `RoleSqlTests.PartialStartupSeed_RepairsWithoutTenant_AndConcurrentSeedsStayUnique`, `ConcurrentCustomRoleCreation_OneSuccessOneConflict`.
- [x] Catalogue-only custom CRUD; no escalation, cross-tenant writes, or reserved-name collisions. — `Create_UnknownPermissionKey_ThrowsValidation`, `Create_NullPermissionEntryReturnsValidation`, `Create_GrantingPermissionCallerDoesNotHold_ThrowsForbidden`, `Create_DuplicateName_ThrowsConflict`, `Update_SystemRole_ThrowsForbidden`, `Update_CrossTenantRole_ThrowsForbidden`, `Delete_SystemRole_ThrowsForbidden`, `Delete_CrossTenantRole_ThrowsForbidden`, `Create_OtherOrganizationDeniedEvenWhenPermissionServiceAllowsIt`.
- [x] REST and GraphQL policies reject stale-claim and tenant bypass attempts in HTTP integration tests. — `RoleSqlTests.HttpAndGraphqlPolicies_UseDatabasePermissions_AndRejectCrossTenantWrites` (stale Owner JWT → 403/GraphQL errors until refresh; foreign-organization write → 403).
- [x] Member assignment enforces Owner and custom-role boundaries. — `SetMemberRole_SelfChange_ThrowsForbidden`, `SetMemberRole_DemoteOwner_NonOwnerCaller_ThrowsForbidden`, `SetMemberRole_DemoteOwner_OwnerCaller_Succeeds`, `SetMemberRole_CustomRoleCannotDemoteOwner`, `SetMemberRole_DelegatedManagerCannotEscalate`, `SetMemberRole_SystemRoleCannotBeUsedAsCustomReference`, `SetMemberRole_ForeignCustomRole_ThrowsValidation`, `SetMemberRole_CustomLiteralName_ThrowsValidation`.
- [x] Delete fallback handles inactive members and invitations; audit/revoke failures roll back all changes. — `RoleSqlTests.DeleteRole_HandlesInactiveMembersAndInvites_AndAuditFailureRollsEverythingBack` (audit failure rolls back demotion + deletes), `ForceRevoke_FailureIsNotReportedAsSuccess` (revoke failure → exception, no audit row).
- [x] Refresh reflects current roles; force-revoke invalidates refresh families and reports actual family count. — `RoleSqlTests.HttpAndGraphqlPolicies_...` (refresh mints `role=member` without `org.roles.manage`), `ForceRevoke_CountsFamiliesAndRevokesEverySession` (2 families revoked + counted), `ForceRevoke_SystemRole_RevokesAllHolders`, `ForceRevoke_CustomRole_RevokesAllHolders`.
- [x] Backend build/tests, SQL regression suite, local health, documentation structure, and contract sync pass. — Final completion wave (2026-09-12): build 0W/0E; full suite with `GRIOT_RUN_SQL_TESTS=1` 212 passed / 0 skipped / 0 failed (incl. 5 SQL RBAC regression tests); `/health` Healthy; contract-sync exit 0. Repairs this wave: RoleSelection cross-tenant fixture fixed (`CustomRole.OrganizationId`), Prune SQL test now seeds the real `Organizations` row (spec-29 FK).
