using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{

    [HttpGet("summary")]
    public IActionResult GetSummary([FromQuery] Guid workspaceId)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpGet("/api/workspaces/{id}/activity")]
    public IActionResult GetActivity(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpGet("/api/logs/errors")]
    public IActionResult GetErrors()
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpGet("/api/logs/audit")]
    public IActionResult GetAudit()
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
