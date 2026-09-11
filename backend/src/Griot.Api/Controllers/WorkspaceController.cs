using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspaceController : DomainControllerBase
{
    private readonly IDomainService _domain;

    public WorkspaceController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    public async Task<IActionResult> GetWorkspaces()
    {
        try { return Ok((await _domain.GetWorkspacesAsync(CurrentUserId())).Where(w => Griot.Api.Auth.AiAccess.AllowsWorkspace(User, w.Id))); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request)
    {
        try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateWorkspaceAsync(request, CurrentUserId())); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWorkspace(Guid id)
    {
        try { return Ok(await _domain.GetWorkspaceAsync(id, CurrentUserId())); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] UpdateWorkspaceRequest request)
    {
        try { return Ok(await _domain.UpdateWorkspaceAsync(id, request, CurrentUserId())); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkspace(Guid id)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return (await _domain.DeleteWorkspaceAsync(id, CurrentUserId())) ? NoContent() : NotFound(new { message = "Workspace not found." }); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        try { return Ok(await _domain.GetMembersAsync(id, CurrentUserId())); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return StatusCode(StatusCodes.Status201Created, await _domain.AddMemberAsync(id, request, CurrentUserId())); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpPatch("{id}/members/{userId}")]
    public async Task<IActionResult> UpdateMember(Guid id, Guid userId, [FromBody] UpdateMemberRoleRequest request)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return Ok(await _domain.UpdateMemberRoleAsync(id, userId, request, CurrentUserId())); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return (await _domain.RemoveMemberAsync(id, userId, CurrentUserId())) ? NoContent() : NotFound(new { message = "Member not found." }); }
        catch (DomainError e) { return Handle(e); }
    }

    [HttpPost("{id}/invites")]
    public async Task<IActionResult> CreateInvite(Guid id, [FromBody] CreateInviteRequest request)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateInviteAsync(id, request, CurrentUserId())); }
        catch (DomainError e) { return Handle(e); }
    }
}
