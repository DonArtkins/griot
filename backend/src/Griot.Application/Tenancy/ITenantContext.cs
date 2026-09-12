using System;

namespace Griot.Application.Tenancy;

/// <summary>
/// Request-level tenant surface (spec 29 — Pool model). The middleware
/// resolves this per request from the JWT `org` claim (spec 30) — never from
/// caller-supplied headers or bodies — and the DbContext global query filters
/// plus the repository guard enforce it. Platform-level paths (SuperAdmin,
/// outbox workers) run with a null organization via
/// <see cref="WithTenantScope"/>; that bypass is audit-logged by the caller.
/// </summary>
public interface ITenantContext
{
    /// <summary>Active organization for this request, or null outside a tenant scope.</summary>
    Guid? OrganizationId { get; }

    /// <summary>True when the current principal is the platform SuperAdmin (spec 32/33).</summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// Lifecycle gate (spec 32/39 suspend semantics — CodeRabbit fix): true when no
    /// org scope is active or the scoped organization is `Active`; false when the
    /// scoped organization is Suspended/Offboarding/Archived (writes → 403
    /// `org_suspended` via <see cref="TenantGuard.RequireOrganization"/>; reads/auth
    /// stay allowed).
    /// </summary>
    bool IsOrganizationActive { get; }

    /// <summary>
    /// Runs <paramref name="action"/> with the tenant scope temporarily replaced.
    /// Used only by platform-level paths; restores the previous scope on return.
    /// </summary>
    void WithTenantScope(Guid? organizationId, Action action);
}
