using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TaskController : ControllerBase
{

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
    public IActionResult BulkStatusUpdate()
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
