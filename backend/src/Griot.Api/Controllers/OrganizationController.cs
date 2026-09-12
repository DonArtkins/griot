using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Griot.Application.Authorization;
using Griot.Application.DTOs.Organizations;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

/// <summary>
/// Spec 32 — SuperAdmin company onboarding &amp; platform management surface
/// (/api/organizations). Routes under {id}/list/create/plan are SuperAdmin-only
/// (fail closed 403); GET {id} also admits an Active Owner/Admin member of that
/// company (own company only). The invite-accept route is for the invited,
/// authenticated user. Suspended companies keep reads + auth — tenant writes
/// elsewhere fail 403 org_suspended via TenantGuard.
/// </summary>
[ApiController]
[Authorize]
[Route("api/organizations")]
public sealed class OrganizationController : DomainControllerBase
{
    private readonly IOrganizationLifecycleService _lifecycle;

    public OrganizationController(IOrganizationLifecycleService lifecycle) => _lifecycle = lifecycle;

    /// <summary>True when the JWT `role` claim is the platform SuperAdmin marker (AuthController mirror).</summary>
    private bool IsSuperAdminCaller =>
        string.Equals(User.FindFirst("role")?.Value, RoleSelection.RoleSuperAdmin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(User.FindFirst(ClaimTypes.Role)?.Value, RoleSelection.RoleSuperAdmin, StringComparison.OrdinalIgnoreCase);

    private void EnsureSuperAdminCaller()
    {
        if (!IsSuperAdminCaller)
            throw new DomainError(DomainErrorKind.Forbidden, "SuperAdmin authority is required for platform management.");
    }

    /// <summary>Onboard a company (transactional seed + Brevo owner invite). SuperAdmin only.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrganizationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            EnsureSuperAdminCaller();
            var result = await _lifecycle.CreateAsync(CurrentUserId(), request).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = result.Organization.Id }, result);
        }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>Paginated, name/slug-searchable company list. SuperAdmin only.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
    {
        try
        {
            EnsureSuperAdminCaller();
            return Ok(await _lifecycle.ListAsync(CurrentUserId(), page, pageSize, search).ConfigureAwait(false));
        }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>One company. SuperAdmin, or an Active Owner/Admin member of that company.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            return Ok(await _lifecycle.GetByIdAsync(CurrentUserId(), id).ConfigureAwait(false));
        }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>Active → Suspended. Every tenant write then fails 403 org_suspended (reads + auth stay allowed).</summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendRequest? request = null)
    {
        try
        {
            EnsureSuperAdminCaller();
            await _lifecycle.SuspendAsync(CurrentUserId(), id, request).ConfigureAwait(false);
            return NoContent();
        }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>Suspended → Active (tenant writes work again).</summary>
    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reactivate(Guid id, [FromBody] SuspendRequest? request = null)
    {
        try
        {
            EnsureSuperAdminCaller();
            await _lifecycle.ReactivateAsync(CurrentUserId(), id, request).ConfigureAwait(false);
            return NoContent();
        }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>Move company ownership to a different Active member (previous owner demotes to Admin).</summary>
    [HttpPost("{id:guid}/transfer-ownership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipRequest request)
    {
        try
        {
            EnsureSuperAdminCaller();
            await _lifecycle.TransferOwnershipAsync(CurrentUserId(), id, request).ConfigureAwait(false);
            return NoContent();
        }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>Update the plan metadata field (no payments in this wave).</summary>
    [HttpPut("{id:guid}/plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanRequest request)
    {
        try
        {
            EnsureSuperAdminCaller();
            return Ok(await _lifecycle.UpdatePlanAsync(CurrentUserId(), id, request).ConfigureAwait(false));
        }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>
    /// Accept an organization invite by token (the invited, authenticated user; the
    /// invite email must match the caller account). Activates the membership and
    /// grants access to the company (including its default workspace). AI OBO
    /// principals are forbidden (403) — membership activation is a human-only action.
    /// </summary>
    [HttpPost("invites/{token}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvite(string token)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try
        {
            await _lifecycle.AcceptInviteAsync(token, CurrentUserId()).ConfigureAwait(false);
            return Ok(new { message = "Organization invite accepted." });
        }
        catch (DomainError e) { return Handle(e); }
    }
}
