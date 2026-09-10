using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;

namespace Griot.Api.Controllers;

[ApiController]
[Authorize]
public class TaskController : DomainControllerBase
{
    private readonly IDomainService _domain;
    private readonly ITaskService _taskService;

    public TaskController(IDomainService domain, ITaskService taskService)
    {
        _domain = domain;
        _taskService = taskService;
    }

    [HttpGet]
    [Route("api/boards/{id}/tasks")]
    public async Task<IActionResult> GetTasks(Guid id)
    { try { return Ok(await _domain.GetTasksAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost]
    [Route("api/boards/{id}/tasks")]
    public async Task<IActionResult> CreateTask(Guid id, [FromBody] CreateTaskRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateTaskAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet]
    [Route("api/tasks/{id}")]
    public async Task<IActionResult> GetTask(Guid id)
    { try { return Ok(await _domain.GetTaskAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPut]
    [Route("api/tasks/{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    { try { return Ok(await _domain.UpdateTaskAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpDelete]
    [Route("api/tasks/{id}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    { try { return (await _domain.DeleteTaskAsync(id, CurrentUserId())) ? NoContent() : NotFound(new { message = "Task not found." }); } catch (DomainError e) { return Handle(e); } }

    [HttpPatch]
    [Route("api/tasks/{id}/move")]
    public async Task<IActionResult> MoveTask(Guid id, [FromBody] MoveTaskRequest request)
    { try { return Ok(await _domain.MoveTaskAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPatch]
    [Route("api/tasks/bulk-status")]
    public async Task<IActionResult> BulkStatusUpdate([FromBody] BulkUpdateTaskStatusRequest request)
    {
        if (request == null || request.WorkspaceId == Guid.Empty || request.TaskIds == null || request.TaskIds.Count == 0)
            return BadRequest(new { message = "Request must include valid workspace ID and non-empty task IDs array" });
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status is required" });
        var userId = CurrentUserId();
        if (userId == Guid.Empty) return Unauthorized(new { message = "Invalid user authentication" });

        var result = await _taskService.BulkUpdateStatusAsync(request, userId);

        if (!result.Success)
        {
            if (result.Message?.Contains("Invalid status value", StringComparison.OrdinalIgnoreCase) == true)
                return BadRequest(new { message = result.Message });
            if (result.Message?.Contains("do not belong", StringComparison.OrdinalIgnoreCase) == true)
                return Conflict(new { message = result.Message });
            if (result.Message?.Contains("not a member", StringComparison.OrdinalIgnoreCase) == true)
                return Forbid();
            if (result.Message?.Contains("server error", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(500, new { message = "An error occurred while processing the bulk update" });
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }
}
