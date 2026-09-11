using System;
using System.Threading.Tasks;

namespace Griot.Application.Interfaces.Services;

/// <summary>One normalized ApiLogs row (spec 20). IP arrives already masked; query already redacted.</summary>
public sealed record ApiLogRecord(
    Guid RequestId,
    Guid? UserId,
    string Method,
    string Path,
    string? QueryString,
    int StatusCode,
    int DurationMs,
    string? UserAgent,
    string? IpAddress);

/// <summary>One ErrorLogs row (spec 20) — unhandled 5xx only.</summary>
public sealed record ErrorLogRecord(
    Guid? RequestId,
    Guid? UserId,
    string ExceptionType,
    string Message,
    string? StackTrace,
    string? Source);

/// <summary>
/// Best-effort telemetry delivery (spec 20 "Failure isolation"): writes must never
/// fail or block the business request. Implementations queue (bounded) and flush in
/// the background; overflow warns non-recursively and loses only telemetry.
/// </summary>
public interface ITelemetryWriter
{
    /// <summary>Queue one ApiLogs row. Non-blocking; never throws.</summary>
    void WriteApiLog(ApiLogRecord record);

    /// <summary>Queue one ErrorLogs row. Non-blocking; never throws.</summary>
    void WriteErrorLog(ErrorLogRecord record);

    /// <summary>Wait until every queued row has been delivered (used at shutdown + by tests).</summary>
    Task DrainAsync(TimeSpan timeout);
}
