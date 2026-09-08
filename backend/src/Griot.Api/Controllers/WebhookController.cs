using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{

    [HttpPost("trigger")]
    public IActionResult Trigger()
    {
        return Ok(new { message = "Not implemented yet" });
    }

}
