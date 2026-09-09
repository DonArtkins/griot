using System;
using System.Security.Claims;
using Griot.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

/// <summary>
/// Shared helpers for [Authorize] domain controllers: extract the JWT `sub` as a
/// Guid, and map <see cref="DomainError"/> to HTTP status (400/401/403/404/409).
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
