using System;
using System.Security.Claims;
using Griot.Api.Auth;
using Griot.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

/// <summary>
/// Shared helpers for [Authorize] domain controllers: extract the JWT `sub` as a
/// Guid, detect AI-originated On-Behalf-Of (OBO) service-token calls, and map
/// <see cref="DomainError"/> to HTTP status (400/401/403/404/409).
/// </summary>
public abstract class DomainControllerBase : ControllerBase
{
    /// <summary>Authenticated user id from the JWT `sub` claim, or Guid.Empty when absent/invalid.</summary>
    protected Guid CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    /// <summary>True when the caller is authenticated (sub present + parseable).</summary>
    protected bool IsAuthenticated => CurrentUserId() != Guid.Empty;

    /// <summary>
    /// True when the caller authenticated via GRIOT_SERVICE_TOKEN On-Behalf-Of flow
    /// (spec 09 / OBO pattern). Detected by the "ai-on-behalf-of" role marker claim or
    /// the "auth_method=service_token_obo" claim. AI calls carry a real user's identity
    /// (user Id, display name, email resolved from DB) with restricted scope claims:
    /// ReadWorkspace, CreateTask, AddComment, CreateNotification. Destructive operations
    /// (delete, invite, member management) MUST call <see cref="ForbidIfAiCall"/> before
    /// executing so the restricted scope contract (spec 09) is enforced ON TOP OF the
    /// impersonated user's own RBAC permissions.
    /// </summary>
    protected bool IsAiCall =>
        User.IsInRole(ServiceTokenHandler.AiOnBehalfOfRole)
        || User.HasClaim("auth_method", "service_token_obo");

    /// <summary>
    /// Returns a 403 Forbid result when the caller originated via the AI service-token
    /// OBO flow, otherwise returns null. Apply to every delete / invite / member-role-change
    /// endpoint so the restricted GRIOT_SERVICE_TOKEN scope contract is enforced even when
    /// the OBO user is a Workspace Owner who would normally be authorized.
    /// </summary>
    protected IActionResult? ForbidIfAiCall()
    {
        if (IsAiCall)
            return Forbid();
        return null;
    }

    /// <summary>
    /// Defense-in-depth scope check for AI OBO calls: returns 403 Forbid when the caller
    /// is an AI OBO principal that does NOT carry the specified scope claim. For non-AI
    /// callers (normal JWT user) this is always a no-op — their own RBAC governs access.
    /// </summary>
    protected IActionResult? RequireAiScope(string scope)
    {
        if (!IsAiCall)
            return null;
        if (!User.HasClaim("scope", scope))
            return Forbid();
        return null;
    }

    /// <summary>401 when unauthenticated.</summary>
    protected IActionResult UnauthorizedIfAnonymous()
        => IsAuthenticated ? null! : Unauthorized(new { message = "Authentication required." });

    /// <summary>Map a DomainError to its HTTP response.</summary>
    protected IActionResult Handle(DomainError e)
    {
        if (e.Kind == DomainErrorKind.NotFound)
            return NotFound(new { message = e.Message });
        if (e.Kind == DomainErrorKind.Conflict)
            return Conflict(new { message = e.Message });
        if (e.Kind == DomainErrorKind.Forbidden)
            return Forbid();
        return BadRequest(new { message = e.Message });
    }
}
