using Microsoft.AspNetCore.Mvc;

namespace Griot.Api.Controllers;

/// <summary>
/// Trigger.dev webhook entry point — <c>POST /api/webhooks/trigger</c>.
///
/// HMAC signature verification is handled upstream by <c>WebhookHmacMiddleware</c>
/// before this controller runs. A request that reaches this action has already been
/// authenticated as a valid Trigger.dev callback. The action relays the payload for
/// routing to the appropriate job/task handler.
///
/// No <c>[Authorize]</c> attribute — the HMAC middleware is the auth gate.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    /// <summary>
    /// Accept a Trigger.dev webhook callback.
    /// HMAC verified by <see cref="Middleware.WebhookHmacMiddleware"/> before this action runs.
    /// Returns 202 Accepted so Trigger.dev marks the delivery successful.
    /// </summary>
    [HttpPost("trigger")]
    public IActionResult Trigger()
    {
        // The body was already read and verified by WebhookHmacMiddleware.
        // Extend this handler in future specs (spec 20+) to route the payload
        // to the appropriate domain service (e.g. notificationFanout, taskUpdate).
        return StatusCode(StatusCodes.Status202Accepted, new { message = "Webhook accepted." });
    }
}
