using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/columns")]
public class ColumnController : ControllerBase
{

    [HttpPatch("{id}")]
    public IActionResult UpdateColumn(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteColumn(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

}
