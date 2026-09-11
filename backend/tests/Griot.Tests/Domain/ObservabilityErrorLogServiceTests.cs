using System;
using System.Threading.Tasks;
using Griot.Api.Services;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Griot.Tests.Domain;

/// <summary>Spec 20 (pipeline §2): the global handler's ErrorLog recording path.</summary>
public class ObservabilityErrorLogServiceTests
{
    private sealed class ThrowingTelemetry : ITelemetryWriter
    {
        public void WriteApiLog(ApiLogRecord record) => throw new InvalidOperationException("queue broken");
        public void WriteErrorLog(ErrorLogRecord record) => throw new InvalidOperationException("queue broken");
        public Task DrainAsync(TimeSpan timeout) => Task.CompletedTask;
    }

    private sealed class CapturingTelemetry : ITelemetryWriter
    {
        public ErrorLogRecord? Captured;
        public void WriteApiLog(ApiLogRecord record) { }
        public void WriteErrorLog(ErrorLogRecord record) => Captured = record;
        public Task DrainAsync(TimeSpan timeout) => Task.CompletedTask;
    }

    [Fact]
    public async Task RecordAsync_CapturesExceptionDetails()
    {
        var telemetry = new CapturingTelemetry();
        var service = new ErrorLogService(telemetry, NullLogger<ErrorLogService>.Instance);
        var requestId = Guid.NewGuid();

        await service.RecordAsync(new InvalidOperationException("boom"), requestId, null, "Griot.Api");

        var captured = Assert.IsType<ErrorLogRecord>(telemetry.Captured);
        Assert.Equal(requestId, captured.RequestId);
        Assert.Equal("System.InvalidOperationException", captured.ExceptionType);
        Assert.Equal("boom", captured.Message);
        Assert.Equal("Griot.Api", captured.Source);
    }

    [Fact]
    public async Task RecordAsync_TruncatesOversizedStackTrace()
    {
        var telemetry = new CapturingTelemetry();
        var service = new ErrorLogService(telemetry, NullLogger<ErrorLogService>.Instance);
        var oversized = new string('s', 20_000);

        await service.RecordAsync(
            new ExceptionWithStackTrace("boom", oversized), Guid.NewGuid(), null, "Griot.Api");

        Assert.True(telemetry.Captured!.StackTrace!.Length <= 8000);
    }

    [Fact]
    public async Task RecordAsync_NeverThrows_WhenTelemetryIsBroken()
    {
        var service = new ErrorLogService(new ThrowingTelemetry(), NullLogger<ErrorLogService>.Instance);

        var exception = await Record.ExceptionAsync(() =>
            service.RecordAsync(new InvalidOperationException("boom"), Guid.NewGuid(), null, "Griot.Api"));

        Assert.Null(exception);                              // failure-isolation hard rule
    }

    /// <summary>Exception with an injectable stack trace (stack caps are applied by the service).</summary>
    private sealed class ExceptionWithStackTrace : Exception
    {
        public ExceptionWithStackTrace(string message, string stackTrace) : base(message)
            => StackTrace = stackTrace;

        public override string? StackTrace { get; }
    }
}
