using Griot.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Griot.Api.Auth;

/// <summary>
/// Spec 31: names the single catalogue permission an endpoint requires
/// (e.g. <c>[RequirePermission(PermissionCatalogue.ProjectManage)]</c>). It maps
/// to the matching <c>perm:{key}</c> policy registered by
/// <see cref="PermissionPolicies"/> at startup, whose requirement is evaluated
/// server-side by <see cref="PermissionAuthorizationHandler"/> through
/// <c>IPermissionService</c> — the JWT `perms` claim is never the policy source.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        => Policy = PermissionPolicies.PolicyName(permission);
}