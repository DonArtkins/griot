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
        _ = tenantContext;
        Guid? organizationId = null;
        var raw = context.User?.Claims.FirstOrDefault(c => c.Type == OrgClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var parsed))
            organizationId = parsed;

        TenantContext.SetCurrent(organizationId);
        try
        {
            await _next(context);
        }
        finally
        {
            TenantContext.SetCurrent(null);
        }
    }
}
