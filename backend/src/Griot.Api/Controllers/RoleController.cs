using System;
using System.Threading.Tasks;
using Griot.Api.Auth;
using Griot.Application.Authorization;
using Griot.Application.DTOs.Roles;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

/// <summary>
/// Spec 31 REST surface: role listing/composition, member role assignment and
/// the force-revoke control. Permission decisions are per-endpoint `perm:*`
/// policies (evaluated server-side by IPermissionService); controllers stay thin.
/// </summary>
[ApiController]
[Authorize]
public class RoleController : DomainControllerBase
{
    private readonly IRoleService _roles;
    public RoleController(IRoleService roles) => _roles = roles;

    /// <summary>GET /api/organizations/{organizationId}/roles — all system + custom roles.</summary>
    [HttpGet]
    [Route("api/organizations/{organizationId:guid}/roles")]
    [RequirePermission(PermissionCatalogue.OrgRead)]
    public async Task<IActionResult> List(Guid organizationId)
    {
        try { return Ok(await _roles.ListAsync(CurrentUserId(), organizationId)); }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>POST /api/organizations/{organizationId}/roles — create a custom role.</summary>
    [HttpPost]
    [Route("api/organizations/{organizationId:guid}/roles")]
    [RequirePermission(PermissionCatalogue.OrgRolesManage)]
    public async Task<IActionResult> Create(Guid organizationId, [FromBody] CreateRoleRequest request)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return Created($"/api/organizations/{organizationId}/roles", await _roles.CreateAsync(CurrentUserId(), organizationId, request)); }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>PUT /api/organizations/{organizationId}/roles/{roleId} — update a custom role.</summary>
    [HttpPut]
    [Route("api/organizations/{organizationId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionCatalogue.OrgRolesManage)]
    public async Task<IActionResult> Update(Guid organizationId, Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return Ok(await _roles.UpdateAsync(CurrentUserId(), organizationId, roleId, request)); }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>
    /// DELETE /api/organizations/{organizationId}/roles/{roleId} — delete a custom role.
    /// Members holding the role cascade back to the system Member role; their refresh
    /// families are revoked so the change is immediately effective.
    /// </summary>
    [HttpDelete]
    [Route("api/organizations/{organizationId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionCatalogue.OrgRolesManage)]
    public async Task<IActionResult> Delete(Guid organizationId, Guid roleId)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { await _roles.DeleteAsync(CurrentUserId(), organizationId, roleId); return NoContent(); }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>
    /// POST /api/organizations/{organizationId}/roles/{roleId}/revoke — force-revoke:
    /// revokes the refresh families of every member holding the role.
    /// </summary>
    [HttpPost]
    [Route("api/organizations/{organizationId:guid}/roles/{roleId:guid}/revoke")]
    [RequirePermission(PermissionCatalogue.OrgRolesManage)]
    public async Task<IActionResult> ForceRevoke(Guid organizationId, Guid roleId)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return Ok(await _roles.ForceRevokeAsync(CurrentUserId(), organizationId, roleId)); }
        catch (DomainError e) { return Handle(e); }
    }

    /// <summary>
    /// PUT /api/organizations/{organizationId}/members/{memberId}/role — assign a system
    /// role ("Admin") or a custom role ("custom:{roleId}") to one member.
    /// </summary>
    [HttpPut]
    [Route("api/organizations/{organizationId:guid}/members/{memberId:guid}/role")]
    [RequirePermission(PermissionCatalogue.OrgMembersManage)]
    public async Task<IActionResult> SetMemberRole(Guid organizationId, Guid memberId, [FromBody] SetMemberRoleRequest request)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return Ok(await _roles.SetMemberRoleAsync(CurrentUserId(), organizationId, memberId, request)); }
        catch (DomainError e) { return Handle(e); }
    }
}