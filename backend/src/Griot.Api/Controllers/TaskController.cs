using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("/api/boards/{id}/tasks")]
    public IActionResult GetTasks(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("/api/boards/{id}/tasks")]
    public IActionResult CreateTask(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpGet("{id}")]
    public IActionResult GetTask(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateTask(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteTask(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPatch("{id}/move")]
    public IActionResult MoveTask(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPatch("bulk-status")]
    public async Task<IActionResult> BulkStatusUpdate([FromBody] BulkUpdateTaskStatusRequest request)
    {
        // Pre-validation: empty array or malformed body
        if (request == null || request.TaskIds == null || request.TaskIds.Count == 0)
        {
            return BadRequest(new { message = "Task IDs array cannot be empty" });
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(new { message = "Status is required" });
        }

        // Get authenticated user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user authentication" });
        }

        var result = await _taskService.BulkUpdateStatusAsync(request, userId);

        if (!result.Success)
        {
            // Check if it's a validation error (400) or conflict (409)
            if (result.Message?.Contains("invalid", StringComparison.OrdinalIgnoreCase) == true ||
                result.Message?.Contains("do not belong", StringComparison.OrdinalIgnoreCase) == true)
            {
                // 409 for any invalid task id in batch (per api-surface.md)
                return Conflict(new { message = result.Message });
            }

            if (result.Message?.Contains("not a member", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Forbid();
            }

            // General bad request for other validation failures
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }
}
