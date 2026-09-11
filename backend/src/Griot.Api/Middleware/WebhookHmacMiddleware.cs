using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Griot.Api.Middleware;

/// <summary>
/// Middleware that verifies the Trigger.dev HMAC signature on incoming webhook requests
/// to <c>POST /api/webhooks/trigger</c> before any handler runs.
///
/// Signature format: <c>X-Trigger-Signature: sha256=&lt;hex&gt;</c>
/// Secret lookup order:
///   1. Configuration["Webhook:Secret"]
///   2. env: WEBHOOK_SECRET
///
/// Returns 401 when:
///   - The secret is not configured (503 instead — misconfiguration is a server error).
///   - The signature header is absent.
///   - The signature does not match HMAC-SHA256(secret, rawBody).
///
/// The body is buffered so downstream middleware/handlers can re-read it.
/// Comparison is constant-time to prevent timing-oracle attacks.
/// </summary>
public sealed class WebhookHmacMiddleware
{
    private const string WebhookRoute = "/api/webhooks/trigger";
    private const int MaxWebhookBodyBytes = 64 * 1024; // documented webhook payload maximum (64 KiB)

    private readonly RequestDelegate _next;
    private readonly IConfiguration  _config;
    private readonly ILogger<WebhookHmacMiddleware> _logger;

    private static string? NonBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    public WebhookHmacMiddleware(
        RequestDelegate next,
        IConfiguration config,
        ILogger<WebhookHmacMiddleware> logger)
    {
        _next   = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only intercept the Trigger.dev webhook route.
        if (!context.Request.Path.Equals(WebhookRoute, StringComparison.OrdinalIgnoreCase)
            || !HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var secret = NonBlank(_config["Webhook:Secret"]) ?? NonBlank(_config["WEBHOOK_SECRET"]);
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Webhook secret not configured; rejecting request.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { message = "Webhook secret not configured." });
            return;
        }

        var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        var bodyLimit = MaxWebhookBodyBytes;
        if (sizeFeature is { IsReadOnly: false })
        {
            bodyLimit = (int)Math.Min(bodyLimit, sizeFeature.MaxRequestBodySize ?? bodyLimit);
            sizeFeature.MaxRequestBodySize = bodyLimit;
        }
        if (context.Request.ContentLength > bodyLimit)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Trigger-Signature", out var sigHeader))
        {
            _logger.LogWarning("Webhook request missing X-Trigger-Signature header.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Missing X-Trigger-Signature header." });
            return;
        }

        // Count bytes as well as applying the server limit: chunked bodies and test/proxy
        // streams may have no Content-Length or request-size feature. Never read past cap + 1.
        context.Request.EnableBuffering(bufferThreshold: bodyLimit, bufferLimit: bodyLimit + 1L);
        var body = new byte[bodyLimit + 1];
        var length = 0;
        try
        {
            while (length < body.Length)
            {
                var count = await context.Request.Body.ReadAsync(body.AsMemory(length), context.RequestAborted);
                if (count == 0) break;
                length += count;
            }
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }
        finally
        {
            context.Request.Body.Position = 0;
        }
        if (length > bodyLimit)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        var provided = sigHeader.ToString().Trim();
        var expected = "sha256=" + Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                body.AsSpan(0, length)
            )
        ).ToLowerInvariant();

        // Constant-time comparison — both strings must have the same byte length.
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var matches = expectedBytes.Length == providedBytes.Length
                   && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);

        if (!matches)
        {
            _logger.LogWarning("Webhook HMAC mismatch — request rejected.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid signature." });
            return;
        }

        _logger.LogInformation("Webhook HMAC verified — passing to handler.");
        await _next(context);
    }
}
