using System;
using System.Collections.Generic;

namespace Griot.Application.DTOs.Roles;

/// <summary>
/// One row of <c>dbo.Roles</c> (spec 29/31): a named role inside one company.
/// System roles (IsSystem = true) are seeded per company and read-only;
/// custom roles are composed ONLY from the fixed permission catalogue.
/// </summary>
public sealed record RoleDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    bool IsSystem,
    string[] Permissions,
    DateTime CreatedAt);

/// <summary>POST /api/organizations/{organizationId}/roles request body (spec 31).</summary>
public sealed record CreateRoleRequest(
    string Name,
    IReadOnlyList<string> Permissions);

/// <summary>PUT /api/organizations/{organizationId}/roles/{roleId} request body (spec 31).</summary>
public sealed record UpdateRoleRequest(
    string? Name,
    IReadOnlyList<string>? Permissions);

/// <summary>
/// PUT /api/organizations/{organizationId}/members/{memberId}/role request body (spec 31).
/// <c>Role</c> is either a system role name (<c>Admin</c>, <c>ProjectManager</c>, …,
/// case-insensitive) or <c>custom:{roleId}</c> for a company-defined role.
/// </summary>
public sealed record SetMemberRoleRequest(string Role);

/// <summary>Member role-assignment result (spec 31): the new effective role.</summary>
public sealed record MemberRoleResult(
    Guid MemberId,
    Guid OrganizationId,
    string Role,
    Guid? CustomRoleId);

/// <summary>POST .../roles/{roleId}/revoke result (spec 31 force-revoke).</summary>
public sealed record ForceRevokeResult(
    Guid RoleId,
    Guid OrganizationId,
    int AffectedUsers,
    int FamiliesRevoked);