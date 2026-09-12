using System;
using Griot.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Griot.Api.Authorization;

/// <summary>
/// Spec 31: REST permission requirement — the policy name IS the permission key
/// ("perm:task.manage"). Evaluated by <see cref="PermissionAuthorizationHandler"/>,
/// which re-resolves the caller's ACTIVE tenant scope from the JWT `org` claim and
/// re-checks the DB (IPermissionService) — the `perms` claim is never the policy.
/// AI On-Behalf-Of principals keep their existing <see cref="Griot.Api.Auth.AiAccess"/>
/// scope contract on top (filter ordering); `perm:` policies only widen nothing.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>The catalogue permission key required (e.g. "task.manage").</summary>
    public string Permission { get; }

    public PermissionRequirement(string permission) =>
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
}

/// <summary>
/// One shared handler resolves every "perm:*" policy. The active organization is the
/// REQUEST tenant scope (spec 29/30 middleware — never caller-supplied), so the DB
/// check and the token's own `org` claim always agree. SuperAdmin passes via the
/// service's platform-resolution.
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var http = context.Resource as HttpContext;
        if (http is null && context.Resource is HotChocolate.Resolvers.IResolverContext)
            http = _httpContextAccessor.HttpContext;
        if (http is null || context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userId, out var callerId))
            return; // Not authenticated → fail closed (the [Authorize] frame already 401'd).

        // Scoped resolution from the REQUEST's service provider: authorization handlers
        // are registered as singletons, so the constructor-injected IServiceProvider is
        // the root container — resolving scoped services (ITenantContext, IPermission-
        // Service) from it would throw under scope validation (dev default).
        var permissions = http.RequestServices.GetRequiredService<IPermissionService>();
        var tenant = http.RequestServices.GetRequiredService<Griot.Application.Tenancy.ITenantContext>();

        if (await permissions.HasPermissionAsync(callerId, tenant.OrganizationId, requirement.Permission)
                .ConfigureAwait(false))
            context.Succeed(requirement);
    }
}

/// <summary>Registers the dynamic "perm:{key}" policy registry (call once at startup).</summary>
public static class PermissionPolicies
{
    /// <summary>The policy name for a catalogue permission key ("org.read" → "perm:org.read").</summary>
    public static string PolicyName(string permissionKey) => $"perm:{permissionKey}";

    /// <summary>
    /// Adds a shared <see cref="PermissionAuthorizationHandler"/> plus one named policy
    /// per catalogue key ("perm:{key}"), so [Authorize(Policy = "perm:task.manage")]
    /// needs no manual registration per endpoint.
    /// </summary>
    public static AuthorizationBuilder AddPermissionPolicies(
        this AuthorizationBuilder builder, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddHttpContextAccessor();

        foreach (var key in Griot.Application.Authorization.PermissionCatalogue.All)
        {
            builder.AddPolicy(PolicyName(key), policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new PermissionRequirement(key));
            });
        }
        return builder;
    }
}
