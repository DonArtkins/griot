using System;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Griot.Api.Middleware;

/// <summary>
/// Resolves the request tenant (spec 29): reads the JWT `org` claim stamped by
/// spec 30 and stores it in the ambient <see cref="TenantContext"/> scope.
/// Runs AFTER UseAuthentication so claims are available. Never reads caller
/// headers/bodies for the tenant. Missing claim (pre-spec-30 tokens,
/// unauthenticated, SuperAdmin platform sessions) = no tenant scope — writes
/// by tenant-scoped services then fail closed (see <c>TenantGuard</c>).
/// </summary>
public sealed class TenantResolutionMiddleware
{
    public const string OrgClaimType = "org";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        Guid? organizationId = null;
        var raw = context.User?.Claims.FirstOrDefault(c => c.Type == OrgClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var parsed))
            organizationId = parsed;

        // Spec 29: SuperAdmin platform principals (spec 32/33) are resolved before
        // any org role — they bypass tenant scoping without an `org` claim. The
        // flag travels ambiently so the pooled-factory wrapper stays org-aware.
        var isSuperAdmin = string.Equals(
            context.User?.FindFirst("role")?.Value, "super_admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
                "super_admin", StringComparison.OrdinalIgnoreCase);

        TenantContext.SetCurrent(organizationId);
        TenantContext.SetSuperAdmin(isSuperAdmin);
        try
        {
            await _next(context);
        }
        finally
        {
            TenantContext.SetCurrent(null);
            TenantContext.SetSuperAdmin(false);
        }
    }
}
