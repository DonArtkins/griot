# RBAC v2 contract — backend feature 31

Owner: [feature 31](../../backend/project-kit/feature-specs/31-rbac-roles-custom-permissions.md).
Tenant: the authenticated JWT `org` claim; route IDs must match it.
Verification: [completion TODO](../planning/FEATURE-31-COMPLETION-TODO.md).

## Routes

All routes require a human JWT and current database permission. AI OBO is denied.
Let `org` mean `/api/organizations/{organizationId}`.

| Method | Route | Permission | Success |
|---|---|---|---|
| GET | `org/roles` | `org.read` | 200, array of RoleDto |
| POST | `org/roles` | `org.roles.manage` | 201, RoleDto |
| PUT | `org/roles/{roleId}` | `org.roles.manage` | 200, RoleDto |
| DELETE | `org/roles/{roleId}` | `org.roles.manage` | 204 |
| POST | `org/roles/{roleId}/revoke` | `org.roles.manage` | 200, ForceRevokeResult |
| PUT | `org/members/{memberId}/role` | `org.members.manage` | 200, MemberRoleResult |

GraphQL `organizationRoles` reads the active company and requires `perm:org.read`.
The shared authorization handler supports HTTP and HotChocolate resolver resources.
There are no GraphQL role mutations or organization/member-list fields in feature 31.

## Bodies and results

- Create: `{name, permissions: string[]}`. Update: `{name?, permissions?: string[]}`.
- Assign: `{role: "Admin"}` or `{role: "custom:{roleId}"}`; system names are case-insensitive.
- RoleDto: `id, organizationId, name, isSystem, permissions, createdAt`.
- MemberRoleResult: `memberId, organizationId, role, customRoleId`.
- ForceRevokeResult: `roleId, organizationId, affectedUsers, familiesRevoked`.
  The last field counts distinct unrevoked user/family pairs, not users or token rows.

Errors: 400 invalid body/name/key/custom reference; 401 unauthenticated; 403 missing
permission, escalation, self-change, protected Owner change, or cross-tenant target;
404 missing organization/role/member; 409 duplicate or reserved name. System roles
cannot be edited/deleted (403). Inactive organizations reject writes (403).

## Permission and mutation rules

Owner/Admin receive the enabled catalogue. ProjectManager has its fixed subset;
it needs an existing custom-role grant of `org.roles.manage` to manage roles.
Member has no organization management permissions. Client has `org.read` and
`client.feedback.read`. `log.read_tier` stays reserved for feature 25.

Custom permissions must be enabled catalogue keys that the caller holds. Assignment
also checks the target role's permissions. Only Owner/SuperAdmin may assign Owner
or change an Owner; this applies to both system and custom assignment forms.
Self-change is forbidden. A custom reference must point to a custom role in the
same company. Invalid persisted custom-role links grant no permissions.

The database is authoritative on every protected request, so an unchanged JWT
cannot retain downgraded permissions. Refresh reissues current role/permission
claims. Force-revoke invalidates all refresh families of active role holders;
already-issued access JWTs remain authenticated until expiry, subject to current
database permission checks.

Deleting a custom role moves all referencing members and invitations to Member
and revokes affected users' refresh families. Role mutations, fallback, revocation,
and audit rows commit in one transaction; failure rolls everything back. Role-name
checks and startup/onboarding seeding serialize on the organization row. Seeding
fills partial sets of the five immutable system roles without an HTTP tenant.

Project/workspace access still uses the existing resource membership checks;
organization permissions do not imply newly implemented project-wide behavior.
