using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using System.Security.Cryptography;
using System.Text;

namespace Griot.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : DomainControllerBase
{
    private readonly IConfiguration _config;
    public WebhookController(IConfiguration config) { _config = config; }

    /// <summary>
    /// Trigger.dev webhook. Verifies the HMAC-SHA256 X-Trigger-Signature against
    /// the configured Webhook:Secret (env: WEBHOOK_SECRET). 202 on valid, 401 when
    /// the signature does not match. Spec 09 readiness (AI service token + webhooks).
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger([FromBody] string? body)
    {
        var secret = _config["Webhook:Secret"] ?? _config["WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Webhook secret not configured." });

        var rawBody = body ?? "";
        var signatureHeader = (Request.Headers.TryGetValue("X-Trigger-Signature", out var sig) ? sig.ToString() : "");
        var expected = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody)));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signatureHeader)))
            return Unauthorized(new { message = "Invalid signature." });

        return StatusCode(StatusCodes.Status202Accepted, new { message = "Webhook accepted." });
    }
}
