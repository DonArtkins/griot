using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectController : ControllerBase
{

    [HttpGet("/api/workspaces/{id}/projects")]
    public IActionResult GetProjects(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("/api/workspaces/{id}/projects")]
    public IActionResult CreateProject(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpGet("{id}")]
    public IActionResult GetProject(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateProject(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteProject(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
