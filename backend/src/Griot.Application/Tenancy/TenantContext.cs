using System;

namespace Griot.Application.Tenancy;

/// <summary>
/// Default <see cref="ITenantContext"/> (spec 29): an <c>AsyncLocal</c> scope
/// holder registered scoped, so the tenant follows the logical request flow
/// (incl. awaits) and never leaks across concurrent requests. The middleware
/// sets the scope at request start; <see cref="WithTenantScope"/> supports the
/// audit-logged platform bypass.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private static readonly System.Threading.AsyncLocal<Guid?> Current = new();

    /// <summary>Sets the ambient tenant scope (middleware + tests only).</summary>
    public static void SetCurrent(Guid? organizationId) => Current.Value = organizationId;

    /// <summary>
    /// Spec 29: SuperAdmin flag travels on the ambient scope too — the middleware
    /// marks platform principals, and the pooled-factory wrapper must copy it onto
    /// each scoped context so org-wide reads stay resolvable without a tenant id.
    /// (The DI registration stays scoped, so the flag never leaks across requests.)
    /// </summary>
    private static readonly System.Threading.AsyncLocal<bool> SuperAdminFlag = new();

    public static void SetSuperAdmin(bool value) => SuperAdminFlag.Value = value;

    public Guid? OrganizationId => Current.Value;

    public bool IsSuperAdmin
    {
        get => SuperAdminFlag.Value;
        set => SuperAdminFlag.Value = value;
    }

    public void WithTenantScope(Guid? organizationId, Action action)
    {
        var previous = Current.Value;
        Current.Value = organizationId;
        try
        {
            action();
        }
        finally
        {
            Current.Value = previous;
        }
    }
}
