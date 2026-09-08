using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentController : ControllerBase
{

    [HttpGet("/api/tasks/{id}/comments")]
    public IActionResult GetComments(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("/api/tasks/{id}/comments")]
    public IActionResult CreateComment(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
