using System;
using System.Threading.Tasks;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Griot.Api.Services;

/// <summary>
/// Spec 20 (pipeline §2): the global exception handler records every unhandled 5xx
/// here. Delivery is fire-and-forget through the bounded <see cref="ITelemetryWriter"/>
/// queue — this service NEVER throws and NEVER blocks the 500 response (failure-
/// isolation hard rule; coverage gaps surface via the warning log only).
/// </summary>
public sealed class ErrorLogService : IErrorLogService
{
    /// <summary>StackTrace column cap (single row sanity bound; matches ADR-002 sizing intent).</summary>
    private const int MaxStackTraceLength = 8000;

    private readonly ITelemetryWriter _telemetry;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(ITelemetryWriter telemetry, ILogger<ErrorLogService> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
    }

    public Task RecordAsync(Exception exception, Guid? requestId, Guid? userId, string source)
    {
        try
        {
            var type = exception.GetType().FullName ?? exception.GetType().Name;
            var message = exception.Message ?? string.Empty;
            var stack = exception.StackTrace is { Length: > 0 } trace
                ? (trace.Length > MaxStackTraceLength ? trace[..MaxStackTraceLength] : trace)
                : null;

            _telemetry.WriteErrorLog(new ErrorLogRecord(requestId, userId, type, message, stack, source));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Spec-20 ErrorLog recording failed; error telemetry lost (best-effort contract).");
        }

        return Task.CompletedTask;
    }
}
