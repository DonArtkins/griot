using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Authorize]
public class DashboardController : DomainControllerBase
{
    private readonly IDomainService _domain;
    public DashboardController(IDomainService domain) { _domain = domain; }

    [HttpGet]
    [Route("api/dashboard/summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid workspaceId)
    { try { return Ok(await _domain.GetDashboardSummaryAsync(workspaceId, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet]
    [Route("api/workspaces/{id}/activity")]
    public async Task<IActionResult> GetActivity(Guid id, [FromQuery] int page, [FromQuery] int pageSize)
    { try { return Ok(await _domain.GetActivityAsync(id, CurrentUserId(), page, pageSize)); } catch (DomainError e) { return Handle(e); } }

    [HttpGet]
    [Route("api/logs/errors")]
    public async Task<IActionResult> GetErrors([FromQuery] Guid workspaceId, [FromQuery] int limit)
    { try { return Ok(await _domain.GetErrorLogsAsync(workspaceId, CurrentUserId(), limit)); } catch (DomainError e) { return Handle(e); } }

    [HttpGet]
    [Route("api/logs/audit")]
    public async Task<IActionResult> GetAudit([FromQuery] Guid workspaceId, [FromQuery] string? entityType, [FromQuery] Guid? entityId, [FromQuery] int limit)
    { try { return Ok(await _domain.GetAuditLogsAsync(workspaceId, CurrentUserId(), entityType, entityId, limit)); } catch (DomainError e) { return Handle(e); } }

    // Spec 20: FixStatus lifecycle writer (Owner/Admin). Destructive-ish log mutation —
    // AI OBO calls are forbidden (spec 09 default-deny boundary; raw-log mutation is
    // permanently closed to AI, spec 25 tiering comes later).
    [HttpPatch]
    [Route("api/logs/errors/{id}/fix-status")]
    public async Task<IActionResult> UpdateErrorFixStatus(Guid id, [FromQuery] Guid workspaceId, [FromBody] Griot.Application.DTOs.UpdateErrorLogStatusRequest request)
    {
        if (ForbidIfAiCall() is IActionResult forbid) return forbid;
        try { return Ok(await _domain.UpdateErrorLogStatusAsync(workspaceId, id, CurrentUserId(), request.FixStatus)); }
        catch (DomainError e) { return Handle(e); }
    }
}
