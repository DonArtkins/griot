using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public class WorkspaceController : ControllerBase
{

    [HttpGet]
    public IActionResult GetWorkspaces()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPost]
    public IActionResult CreateWorkspace()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpGet("{id}")]
    public IActionResult GetWorkspace(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateWorkspace(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteWorkspace(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpGet("{id}/members")]
    public IActionResult GetMembers(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPost("{id}/members")]
    public IActionResult AddMember(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPatch("{id}/members/{userId}")]
    public IActionResult UpdateMember(Guid id, Guid userId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpDelete("{id}/members/{userId}")]
    public IActionResult RemoveMember(Guid id, Guid userId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpPost("{id}/invites")]
    public IActionResult CreateInvite(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

}
