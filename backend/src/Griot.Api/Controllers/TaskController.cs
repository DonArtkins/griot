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
[Route("api/tasks")]
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

    [HttpGet("/api/boards/{id}/tasks")]
    public async Task<IActionResult> GetTasks(Guid id)
    { try { return Ok(await _domain.GetTasksAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPost("/api/boards/{id}/tasks")]
    public async Task<IActionResult> CreateTask(Guid id, [FromBody] CreateTaskRequest request)
    { try { return StatusCode(StatusCodes.Status201Created, await _domain.CreateTaskAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(Guid id)
    { try { return Ok(await _domain.GetTaskAsync(id, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    { try { return Ok(await _domain.UpdateTaskAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    { try { return (await _domain.DeleteTaskAsync(id, CurrentUserId())) ? NoContent() : NotFound(new { message = "Task not found." }); } catch (DomainError e) { return Handle(e); } }

    [HttpPatch("{id}/move")]
    public async Task<IActionResult> MoveTask(Guid id, [FromBody] MoveTaskRequest request)
    { try { return Ok(await _domain.MoveTaskAsync(id, request, CurrentUserId())); } catch (DomainError e) { return Handle(e); } }

    [HttpPatch("bulk-status")]
    public IActionResult BulkStatusUpdate([FromBody] BulkUpdateTaskStatusRequest request)
    {
        if (request == null || request.WorkspaceId == Guid.Empty || request.TaskIds == null || request.TaskIds.Count == 0)
            return BadRequest(new { message = "Request must include valid workspace ID and non-empty task IDs array" });
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status is required" });
        var userId = CurrentUserId();
        if (userId == Guid.Empty) return Unauthorized(new { message = "Invalid user authentication" });
        return Ok(_taskService.BulkUpdateStatusAsync(request, userId));
    }
}
