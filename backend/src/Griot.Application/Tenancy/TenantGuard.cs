using System;
using Griot.Application.Services;
using Griot.Application.Tenancy;

namespace Griot.Application.Tenancy;

/// <summary>
/// Write-side tenant enforcement (spec 29): every tenant-scoped service call
/// resolves the active org through here. Reads are already isolated by the
/// EF Core global query filters; this guard makes writes **fail closed** —
/// a missing tenant scope or an OrganizationId that differs from the active
/// scope throws <see cref="DomainError"/> (403 cross-tenant).
/// </summary>
public static class TenantGuard
{
    /// <summary>Active org or throws (tenant-scoped writes require a scope).</summary>
    public static Guid RequireOrganization(ITenantContext tenant)
    {
        if (tenant.OrganizationId is Guid orgId)
            return orgId;
        throw new DomainError(DomainErrorKind.Forbidden, "Organization scope is required.");
    }

    /// <summary>Asserts the entity's org matches the active scope (or throws 403).</summary>
    public static void AssertTenant(ITenantContext tenant, Guid entityOrganizationId)
    {
        var active = RequireOrganization(tenant);
        if (entityOrganizationId != active)
            throw new DomainError(DomainErrorKind.Forbidden, "cross-tenant");
    }

    /// <summary>Stamps the active org onto a new entity; returns it for chaining.</summary>
    public static T Stamp<T>(ITenantContext tenant, T entity, Action<T, Guid> setOrg)
    {
        setOrg(entity, RequireOrganization(tenant));
        return entity;
    }
}
