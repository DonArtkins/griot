using System;
using System.Threading.Tasks;
using System.IO;
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
    public async Task<IActionResult> Trigger()
    {
        var secret = _config["Webhook:Secret"] ?? _config["WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Webhook secret not configured." });

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = (await reader.ReadToEndAsync()).Trim();
        var signatureHeader = (Request.Headers.TryGetValue("X-Trigger-Signature", out var sig) ? sig.ToString().Trim() : "");
        var expected = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        var matches = expected.Length == signatureHeader.Length
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signatureHeader));
        if (!matches)
            return Unauthorized(new { message = "Invalid signature." });

        return StatusCode(StatusCodes.Status202Accepted, new { message = "Webhook accepted." });
    }
}
