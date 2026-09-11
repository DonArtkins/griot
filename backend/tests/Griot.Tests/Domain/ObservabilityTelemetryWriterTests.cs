using System;
using System.Threading;
using System.Threading.Tasks;
using Griot.Api.Services;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Griot.Tests.Domain;

/// <summary>
/// Spec 20 ("Bumped §1" + failure-isolation hard rule): TelemetryWriter is a BOUNDED
/// (1,000) queue with a background flush — writes never throw or block; overflow drops
/// only telemetry and counts the drop; persist failures are isolated (row lost, warning
/// logged, committed request unaffected).
/// </summary>
public class ObservabilityTelemetryWriterTests : IDisposable
{
    private static ApiLogRecord ApiRecord() => new(
        Guid.NewGuid(), Guid.NewGuid(), "GET", "/api/workspaces", null, 200, 12, "ua", "127.0.0.0");

    private static ErrorLogRecord ErrorRecord() => new(
        Guid.NewGuid(), Guid.NewGuid(), "System.InvalidOperationException", "boom", "stack", "Griot.Api");

    private readonly Mock<IGenericRepository<ApiLog>> _apiRepo = new();
    private readonly Mock<IGenericRepository<ErrorLog>> _errorRepo = new();
    private readonly TelemetryWriter _writer;

    public ObservabilityTelemetryWriterTests()
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IGenericRepository<ApiLog>))).Returns(_apiRepo.Object);
        provider.Setup(p => p.GetService(typeof(IGenericRepository<ErrorLog>))).Returns(_errorRepo.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);

        _writer = new TelemetryWriter(factory.Object, NullLogger<TelemetryWriter>.Instance);
    }

    public void Dispose() => _writer.Dispose();

    /// <summary>The writer is constructed but NEVER started here — the channel is not
    /// being read, so the bounded capacity is exercised deterministically.</summary>
    [Fact]
    public void WriteApiLog_AcceptsRows_UpToBoundedCapacity()
    {
        for (var i = 0; i < TelemetryWriter.QueueCapacity; i++)
            _writer.WriteApiLog(ApiRecord());

        Assert.Equal(TelemetryWriter.QueueCapacity, _writer.Pending);
    }

    [Fact]
    public void WriteApiLog_WhenQueueFull_DropsRow_CountsDrop_NeverThrows()
    {
        for (var i = 0; i < TelemetryWriter.QueueCapacity; i++)
            _writer.WriteApiLog(ApiRecord());

        var exception = Record.Exception(() => _writer.WriteApiLog(ApiRecord()));

        Assert.Null(exception);                              // best-effort contract
        Assert.Equal(TelemetryWriter.QueueCapacity, _writer.Pending);
        Assert.Equal(1, _writer.Dropped);                    // overflow warned non-recursively
    }

    [Fact]
    public async Task Started_Writer_FlushesQueuedApiRowsThroughScopedRepository()
    {
        await _writer.StartAsync(CancellationToken.None);

        var record = ApiRecord();
        _writer.WriteApiLog(record);
        await _writer.DrainAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, _writer.Pending);
        _apiRepo.Verify(r => r.AddAsync(It.Is<ApiLog>(a =>
            a.RequestId == record.RequestId &&
            a.UserId == record.UserId &&
            a.Method == "GET" &&
            a.Path == "/api/workspaces" &&
            a.StatusCode == 200 &&
            a.DurationMs == 12 &&
            a.IpAddress == "127.0.0.0")), Times.Once);
        _apiRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Started_Writer_FlushesErrorRows_WithOpenFixStatus()
    {
        await _writer.StartAsync(CancellationToken.None);

        _writer.WriteErrorLog(ErrorRecord());
        await _writer.DrainAsync(TimeSpan.FromSeconds(5));

        _errorRepo.Verify(r => r.AddAsync(It.Is<ErrorLog>(e =>
            e.ExceptionType == "System.InvalidOperationException" &&
            e.Message == "boom" &&
            e.Source == "Griot.Api" &&
            e.FixStatus == Griot.Domain.Enums.ErrorFixStatus.Open)), Times.Once);
    }

    [Fact]
    public async Task StoreFailure_Isolated_RowLost_RequestUnaffected()
    {
        _apiRepo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("SQL down"));
        await _writer.StartAsync(CancellationToken.None);

        var exception = await Record.ExceptionAsync(async () =>
        {
            _writer.WriteApiLog(ApiRecord());
            await _writer.DrainAsync(TimeSpan.FromSeconds(5));
        });

        Assert.Null(exception);                              // coverage gap surfaced via warning only
        Assert.Equal(0, _writer.Pending);
    }

    [Fact]
    public async Task QueuedRow_DurationMs_IsAlwaysPositive()
    {
        await _writer.StartAsync(CancellationToken.None);

        _writer.WriteApiLog(new ApiLogRecord(Guid.NewGuid(), null, "GET", "/x", null, 404, 0, null, null));
        await _writer.DrainAsync(TimeSpan.FromSeconds(5));

        // Acceptance: every request row carries DurationMs > 0 (sub-ms rounds UP at capture).
        _apiRepo.Verify(r => r.AddAsync(It.Is<ApiLog>(a => a.DurationMs > 0)), Times.Once);
    }
}
