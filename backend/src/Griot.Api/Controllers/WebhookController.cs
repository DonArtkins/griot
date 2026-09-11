using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

/// <summary>HMAC-verified callback boundary. Dispatch is not implemented yet.</summary>
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    /// <summary>Keep delivery retryable until a durable, replay-safe inbox is available.</summary>
    [HttpPost("trigger")]
    public IActionResult Trigger()
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { message = "Webhook dispatch is not available." });
    }
}
