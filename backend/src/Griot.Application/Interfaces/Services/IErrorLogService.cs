using System;
using System.Threading.Tasks;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// ErrorLogs writer (spec 20, pipeline §2): the global exception handler calls
/// <see cref="RecordAsync"/> — best-effort, never throws, never blocks the 500 response.
/// Only unhandled 5xx land here; DomainError-based 4xx are business errors, not exceptions.
/// </summary>
public interface IErrorLogService
{
    /// <summary>Record one unhandled exception. Fire-and-forget; failures are logged, never surfaced.</summary>
    Task RecordAsync(Exception exception, Guid? requestId, Guid? userId, string source);
}
