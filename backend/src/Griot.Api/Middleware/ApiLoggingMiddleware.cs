using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Microsoft.AspNetCore.Http;

namespace Griot.Api.Middleware;

/// <summary>
/// Spec 20 (pipeline §1 + "Bumped §2"): one ApiLogs row per request, INCLUDING the
/// early exits (401 challenge, 404, 429 limiter, HMAC 401, 500) — capture starts
/// right after the request-id middleware and BEFORE any early-exit pipeline stage;
/// final status/user/RequestId are read from RESPONSE COMPLETION, after
/// authentication and the exception handler have run, so the row carries the
/// status the client actually saw. Health checks are excluded. The write is
/// fire-and-forget (bounded queue): a telemetry failure never fails a request.
/// Stored data contract: QueryString redacted (<see cref="ApiLogSanitizer"/>),
/// IP masked (/24 IPv4, /64 IPv6); raw authorization headers, cookies, OTPs and
/// request bodies are never stored.
/// </summary>
public sealed class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ApiLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITelemetryWriter telemetry)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = Truncate(context.Request.Path.Value ?? "/", 300);
        var rawQuery = context.Request.QueryString.Value;
        var userAgent = Truncate(context.Request.Headers.UserAgent.FirstOrDefault(), 300);
        var ip = ApiLogSanitizer.MaskIp(context.Connection.RemoteIpAddress?.ToString());

        // Finalize from RESPONSE COMPLETION (after authentication/exception handling):
        // the row must record the status the client actually saw. Non-async: the
        // telemetry enqueue is synchronous by contract.
        context.Response.OnCompleted(() =>
        {
            stopwatch.Stop();
            try
            {
                // DurationMs > 0 contract: sub-millisecond requests round UP.
                var durationMs = (int)Math.Ceiling(stopwatch.Elapsed.TotalMilliseconds);
                var requestId = Guid.TryParse(context.TraceIdentifier, out var rid)
                    ? rid
                    : Guid.NewGuid();

                telemetry.WriteApiLog(new ApiLogRecord(
                    requestId,
                    ResolveUserId(context),
                    method,
                    path,
                    ApiLogSanitizer.RedactQueryString(rawQuery) is { Length: > 0 } query ? query : null,
                    context.Response.StatusCode,
                    durationMs < 1 ? 1 : durationMs,
                    userAgent,
                    ip));
            }
            catch (Exception)
            {
                // Belt-and-braces: WriteApiLog itself never throws; finalize must
                // never fail the response even if something above it did.
            }

            return Task.CompletedTask;
        });

        await _next(context).ConfigureAwait(false);
    }

    private static Guid? ResolveUserId(HttpContext context)
    {
        var claim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
