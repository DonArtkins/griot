using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Spec 31: server-side permission resolution — the DB is the source of truth;
/// the JWT `perms` claim is an optimization, never the policy. Every enforcement
/// decision ([RequirePermission] REST attribute, GraphQL `perm:` policies, and
/// role-service self-checks) goes through here and re-resolves the caller's
/// active membership (including the custom role's persisted permission set).
/// SuperAdmin resolves first (tenancy-guide §3) with the full catalogue.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// True when the caller holds <paramref name="permission"/> in the given
    /// organization. A null organizationId only succeeds for the platform
    /// SuperAdmin. Unknown users / inactive memberships → false (fail closed).
    /// </summary>
    Task<bool> HasPermissionAsync(Guid callerId, Guid? organizationId, string permission);

    /// <summary>
    /// The caller's effective permission keys in the organization (empty set
    /// outside a tenant / for pure Members). SuperAdmin returns the full
    /// catalogue including reserved keys.
    /// </summary>
    Task<IReadOnlySet<string>> EffectivePermissionsAsync(Guid callerId, Guid? organizationId);

    /// <summary>
    /// The caller's effective role name in the organization (spec 30 session
    /// semantics: "super_admin", "Owner", "Admin", … or "custom:{roleId}"),
    /// or null outside a tenant / without an active membership.
    /// </summary>
    Task<string?> GetEffectiveRoleAsync(Guid callerId, Guid? organizationId);
}