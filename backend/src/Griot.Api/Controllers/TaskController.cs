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
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPost("/api/boards/{id}/tasks")]
    public IActionResult CreateTask(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpGet("{id}")]
    public IActionResult GetTask(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateTask(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteTask(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPatch("{id}/move")]
    public IActionResult MoveTask(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPatch("bulk-status")]
    public async Task<IActionResult> BulkStatusUpdate([FromBody] BulkUpdateTaskStatusRequest request)
    {
        // Pre-validation: empty array, malformed body, or missing workspace ID
        if (request == null || request.WorkspaceId == Guid.Empty ||
            request.TaskIds == null || request.TaskIds.Count == 0)
        {
            return BadRequest(new { message = "Request must include valid workspace ID and non-empty task IDs array" });
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
            // Handle invalid status value separately (400 Bad Request)
            if (result.Message?.Contains("Invalid status value", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BadRequest(new { message = result.Message });
            }

            // Check if it's a conflict error (409)
            if (result.Message?.Contains("do not belong", StringComparison.OrdinalIgnoreCase) == true)
            {
                // 409 for any invalid task id in batch (per api-surface.md)
                return Conflict(new { message = result.Message });
            }

            // Check if it's a forbidden error (403)
            if (result.Message?.Contains("not a member", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Forbid();
            }

            // Server error (stored procedure failure) - return 500 without exposing details
            if (result.Message?.Contains("server error", StringComparison.OrdinalIgnoreCase) == true)
            {
                return StatusCode(500, new { message = "An error occurred while processing the bulk update" });
            }

            // General bad request for other validation failures
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }
}
