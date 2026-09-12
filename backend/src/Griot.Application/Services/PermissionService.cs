using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.Authorization;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Enums;

namespace Griot.Application.Services;

/// <summary>
/// Spec 31: server-side permission resolution — the DB is the source of truth; the
/// JWT `perms` claim is an optimization, never the policy. Re-resolves the caller's
/// platform role and active membership on every check (including the custom role's
/// persisted permission set via <see cref="RoleSelection.EffectivePerms"/>) so a
/// stale token can never grant a permission the DB no longer grants.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>(StringComparer.Ordinal);

    private readonly IAuthRepository _auth;

    public PermissionService(IAuthRepository auth) => _auth = auth;

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(Guid callerId, Guid? organizationId, string permission)
    {
        // Unknown keys never authorize — the catalogue is the only key space.
        if (!PermissionCatalogue.IsKnown(permission))
            return false;

        var user = await _auth.FindUserByIdAsync(callerId).ConfigureAwait(false);
        if (user is null)
            return false;

        // Tenancy guide §3: the platform role resolves before any organization role.
        if (user.PlatformRole == PlatformRole.SuperAdmin)
            return true;

        if (organizationId is not Guid organizationIdValue)
            return false;

        var member = await _auth
            .FindActiveOrganizationMemberAsync(organizationIdValue, callerId)
            .ConfigureAwait(false);
        if (member is null || member.Organization.Status != OrganizationStatus.Active)
            return false;

        return RoleSelection
            .EffectivePerms(member)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(permission, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> EffectivePermissionsAsync(Guid callerId, Guid? organizationId)
    {
        var user = await _auth.FindUserByIdAsync(callerId).ConfigureAwait(false);
        if (user is null)
            return Empty;

        // SuperAdmin: the full stammable catalogue (reserved keys stay spec-25-gated).
        if (user.PlatformRole == PlatformRole.SuperAdmin)
            return new HashSet<string>(PermissionCatalogue.All, StringComparer.Ordinal);

        if (organizationId is not Guid organizationIdValue)
            return Empty;

        var member = await _auth
            .FindActiveOrganizationMemberAsync(organizationIdValue, callerId)
            .ConfigureAwait(false);
        if (member is null || member.Organization.Status != OrganizationStatus.Active)
            return Empty;

        return new HashSet<string>(
            RoleSelection.EffectivePerms(member)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries),
            StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<string?> GetEffectiveRoleAsync(Guid callerId, Guid? organizationId)
    {
        var user = await _auth.FindUserByIdAsync(callerId).ConfigureAwait(false);
        if (user is null)
            return null;

        if (user.PlatformRole == PlatformRole.SuperAdmin)
            return RoleSelection.RoleSuperAdmin;

        if (organizationId is not Guid organizationIdValue)
            return null;

        var member = await _auth
            .FindActiveOrganizationMemberAsync(organizationIdValue, callerId)
            .ConfigureAwait(false);
        if (member is null || member.Organization.Status != OrganizationStatus.Active)
            return null;

        return RoleSelection.EffectiveRole(isSuperAdmin: false, member);
    }
}