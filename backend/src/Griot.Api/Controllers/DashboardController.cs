using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public DashboardController(IDomainService domain) { _domain = domain; }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid workspaceId)
    { try { return Ok(await _domain.GetDashboardSummaryAsync(workspaceId, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet("/api/workspaces/{id}/activity")]
    public async Task<IActionResult> GetActivity(Guid id, [FromQuery] int page, [FromQuery] int pageSize)
    { try { return Ok(await _domain.GetActivityAsync(id, CurrentUserId(), page, pageSize)); } catch (DomainError e) { return Handle(e); } }

    [HttpGet("/api/logs/errors")]
    public async Task<IActionResult> GetErrors([FromQuery] Guid workspaceId, [FromQuery] int limit)
    { try { return Ok(await _domain.GetErrorLogsAsync(workspaceId, CurrentUserId(), limit)); } catch (DomainError e) { return Handle(e); } }

    [HttpGet("/api/logs/audit")]
    public async Task<IActionResult> GetAudit([FromQuery] Guid workspaceId, [FromQuery] string? entityType, [FromQuery] Guid? entityId, [FromQuery] int limit)
    { try { return Ok(await _domain.GetAuditLogsAsync(workspaceId, CurrentUserId(), entityType, entityId, limit)); } catch (DomainError e) { return Handle(e); } }
}
