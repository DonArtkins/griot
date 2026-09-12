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

    /// <summary>
    /// Spec 32/39 suspend gate (CodeRabbit fix): lifecycle status of the scoped
    /// organization, stamped by the middleware once per request. Writes must be
    /// rejected with 403 `org_suspended` when this is false; reads stay allowed.
    /// Nullable backing flag: UNSET means "no status stamped" → treated as Active
    /// (unit tests / non-HTTP paths without the middleware stay fail-open, matching
    /// the pre-gate behavior; only a middleware-stamped `false` suspends writes).
    /// </summary>
    private static readonly System.Threading.AsyncLocal<bool?> OrganizationActiveFlag = new();

    public static void SetOrganizationActive(bool? value) => OrganizationActiveFlag.Value = value;

    public Guid? OrganizationId => Current.Value;

    public bool IsSuperAdmin
    {
        get => SuperAdminFlag.Value;
        set => SuperAdminFlag.Value = value;
    }

    /// <summary>True when no org scope is active, no status was stamped, or the scoped org is Active (spec 32 gate).</summary>
    public bool IsOrganizationActive => OrganizationActiveFlag.Value ?? true;

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
