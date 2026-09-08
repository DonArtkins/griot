using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/invites")]
public class InviteController : ControllerBase
{

    [HttpPost("{token}/accept")]
    public IActionResult AcceptInvite(string token)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet" });
    }

}
