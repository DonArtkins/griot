using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/boards")]
public class BoardController : ControllerBase
{

    [HttpGet("/api/projects/{id}/boards")]
    public IActionResult GetBoards(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("/api/projects/{id}/boards")]
    public IActionResult CreateBoard(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpGet("{id}")]
    public IActionResult GetBoard(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

    [HttpPost("{id}/columns")]
    public IActionResult CreateColumn(Guid id)
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
