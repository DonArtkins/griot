using System.Security.Claims;

namespace Griot.Api.Auth;

/// <summary>AI grants narrow the user's permissions; they never replace workspace RBAC.</summary>
public static class AiAccess
{
    public const string WorkspaceClaim = "ai_workspace";
    public static readonly IReadOnlyList<string> Scopes = Array.AsReadOnly(new[]
    {
        ServiceTokenHandler.ScopeReadWorkspace, ServiceTokenHandler.ScopeCreateTask,
        ServiceTokenHandler.ScopeAddComment, ServiceTokenHandler.ScopeCreateNotification
    });

    public static bool IsAi(ClaimsPrincipal user) =>
        user.IsInRole(ServiceTokenHandler.AiOnBehalfOfRole)
        || user.HasClaim("auth_method", "service_token_obo");

    public static bool AllowsScope(ClaimsPrincipal user, string? scope) =>
        !IsAi(user) || (scope is not null && Scopes.Contains(scope) && user.HasClaim("scope", scope));

    public static bool AllowsWorkspace(ClaimsPrincipal user, Guid workspaceId) =>
        !IsAi(user) || user.HasClaim(WorkspaceClaim, workspaceId.ToString("D"));

    public static void RequireScope(ClaimsPrincipal user, string? scope)
    {
        if (!AllowsScope(user, scope))
            throw new UnauthorizedAccessException("Operation is outside the AI delegation.");
    }

    public static void RequireWorkspace(ClaimsPrincipal user, Guid workspaceId)
    {
        if (!AllowsWorkspace(user, workspaceId))
            throw new UnauthorizedAccessException("Workspace is outside the AI delegation.");
    }
}
