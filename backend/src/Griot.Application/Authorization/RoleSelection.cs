using System;
using Griot.Domain.Entities;
using Griot.Domain.Enums;

namespace Griot.Application.Authorization;

/// <summary>
/// Spec 30: the resolved tenant session stamped into every JWT v2 access token.
/// <see cref="OrganizationId"/> is null for platform-only sessions (SuperAdmin, or a
/// user with no active organization) — the `org` claim is then omitted by design.
/// </summary>
public sealed record ActiveOrganization(
    Guid? OrganizationId,
    string Role,
    string Perms);

/// <summary>
/// Spec 30 role/perms resolution (tenancy guide §3 precedence: the platform role
/// resolves before any organization role). <see cref="RoleSelection.EffectiveRole"/>
/// and <see cref="RoleSelection.EffectivePerms"/> are the single source for what the
/// `role`/`perms` claims contain; <see cref="PermissionCatalogue"/> owns the key list.
/// </summary>
public static class RoleSelection
{
    public const string RoleSuperAdmin = "super_admin";
    public const string RoleOwner = "owner";
    public const string RoleAdmin = "admin";
    public const string RoleProjectManager = "project_manager";
    public const string RoleMember = "member";
    public const string RoleClient = "client";
    public const string RoleCustomPrefix = "custom:";

    /// <summary>Canonical role-string for an <see cref="OrganizationRole"/>.</summary>
    public static string RoleName(OrganizationRole role) => role switch
    {
        OrganizationRole.Owner => RoleOwner,
        OrganizationRole.Admin => RoleAdmin,
        OrganizationRole.ProjectManager => RoleProjectManager,
        OrganizationRole.Member => RoleMember,
        OrganizationRole.Client => RoleClient,
        OrganizationRole.Custom => RoleCustomPrefix, // caller appends the CustomRoleId (spec 31)
        _ => RoleMember
    };

    /// <summary>
    /// Effective role per tenancy guide §3. SuperAdmin wins over any org role. A null
    /// membership with a SuperAdmin principal is the platform session (super_admin);
    /// a null membership otherwise means an identity-only session (member, no perms).
    /// </summary>
    public static string EffectiveRole(bool isSuperAdmin, OrganizationMember? member)
    {
        if (isSuperAdmin)
            return RoleSuperAdmin;
        if (member is null)
            return RoleMember;
        if (member.Role == OrganizationRole.Custom && member.CustomRoleId is Guid customId)
            return $"{RoleCustomPrefix}{customId}";
        return RoleName(member.Role);
    }

    /// <summary>Effective perms string for the active membership (empty outside a tenant).</summary>
    public static string EffectivePerms(OrganizationMember? member)
    {
        if (member is null)
            return string.Empty;

        if (member.Role == OrganizationRole.Custom)
        {
            if (member.CustomRole is null || member.CustomRole.IsSystem
                || member.CustomRole.Id != member.CustomRoleId
                || member.CustomRole.OrganizationId != member.OrganizationId)
                return string.Empty;
            // Trust the persisted Role.Permissions column (spec 31 validates writes),
            // but the JWT claim contract is catalogue keys only — unknown persisted
            // values never reach a token (reserved keys like log.read_tier are known).
            var keys = (member.CustomRole?.Permissions ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(PermissionCatalogue.IsKnown);
            return PermissionCatalogue.Join(keys);
        }

        return PermissionCatalogue.PermsForSystemRole(member.Role);
    }
}
