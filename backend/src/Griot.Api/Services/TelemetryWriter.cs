using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Griot.Api.Services;

/// <summary>
/// Spec 20 telemetry delivery (pipeline §1 + "Bumped §1"): ApiLogs/ErrorLogs are
/// BEST-EFFORT signals carried on a bounded queue (capacity 1,000) and flushed to
/// SQL Server by one background reader. Writes never block or throw — overflow
/// drops only telemetry and warns non-recursively (the warning must not itself
/// try to enqueue another telemetry row). Rows are written through fresh scoped
/// <see cref="IGenericRepository{T}"/>s so a broken business request can never
/// poison the telemetry scope (and vice versa). AuditLogs/ActivityLogs do NOT go
/// through this queue — they commit with the domain transaction in
/// <see cref="Griot.Application.Services.AuditService"/>.
/// On shutdown the writer completes the channel and drains the queue with a
/// bounded timeout, so stopping the API does not silently lose queued rows.
/// </summary>
public sealed class TelemetryWriter : BackgroundService, ITelemetryWriter
{
    /// <summary>Bounded queue capacity (spec 20 "Bumped §1").</summary>
    public const int QueueCapacity = 1000;

    /// <summary>Bound on the shutdown drain so a dead database cannot hang host shutdown.</summary>
    private static readonly TimeSpan ShutdownFlushTimeout = TimeSpan.FromSeconds(5);

    private readonly Channel<object> _channel = Channel.CreateBounded<object>(QueueCapacity);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelemetryWriter> _logger;
    private int _pending;
    private long _dropped;

    public TelemetryWriter(IServiceScopeFactory scopeFactory, ILogger<TelemetryWriter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Rows still queued and not yet persisted.</summary>
    public int Pending => Volatile.Read(ref _pending);

    /// <summary>Rows dropped because the bounded queue was full (overflow counter).</summary>
    public long Dropped => Interlocked.Read(ref _dropped);

    public void WriteApiLog(ApiLogRecord record) => TryEnqueue(record, apiLog: true);

    public void WriteErrorLog(ErrorLogRecord record) => TryEnqueue(record, apiLog: false);

    public async Task DrainAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (Volatile.Read(ref _pending) > 0)
        {
            if (DateTime.UtcNow >= deadline)
                return; // Bounded: never hang the caller on a slow store.
            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    private void TryEnqueue(object record, bool apiLog)
    {
        try
        {
            Interlocked.Increment(ref _pending);
            if (_channel.Writer.TryWrite(record))
                return;

            // Queue full: drop THIS row only, warn without writing another telemetry row.
            Interlocked.Decrement(ref _pending);
            Interlocked.Increment(ref _dropped);
            _logger.LogWarning(
                "Spec-20 telemetry queue full ({Capacity}); dropped one {Kind} row (total dropped: {Dropped}). Telemetry is best-effort — the request is unaffected.",
                QueueCapacity, apiLog ? "ApiLogs" : "ErrorLogs", Interlocked.Read(ref _dropped));
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _pending, Math.Max(0, Volatile.Read(ref _pending) - 1));
            _logger.LogWarning(ex, "Spec-20 telemetry enqueue failed; row lost (best-effort contract).");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // The loop ends when the writer is completed (shutdown) — cancellation
            // falls through to the bounded final flush below.
            while (await _channel.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
                await FlushAvailableAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown cancelled the loop; drain what is left below.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spec-20 telemetry reader loop crashed; attempting a final bounded flush.");
        }

        await FlushAvailableAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task FlushAvailableAsync(CancellationToken cancellationToken)
    {
        while (_channel.Reader.TryRead(out var item))
        {
            try
            {
                await PersistRowAsync(item, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _pending);
            }
        }
    }

    private async Task PersistRowAsync(object item, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            switch (item)
            {
                case ApiLogRecord api:
                    var apiRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<ApiLog>>();
                    await apiRepo.AddAsync(new ApiLog
                    {
                        RequestId = api.RequestId,
                        UserId = api.UserId,
                        Method = api.Method,
                        Path = api.Path,
                        QueryString = api.QueryString,
                        StatusCode = api.StatusCode,
                        // Acceptance contract: every ApiLogs row carries DurationMs > 0 —
                        // the writer is the final guarantee (middleware rounds UP at capture).
                        DurationMs = Math.Max(1, api.DurationMs),
                        UserAgent = api.UserAgent,
                        IpAddress = api.IpAddress,
                        CreatedAt = DateTime.UtcNow
                    }).ConfigureAwait(false);
                    await apiRepo.SaveChangesAsync().ConfigureAwait(false);
                    break;

                case ErrorLogRecord error:
                    var errorRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<ErrorLog>>();
                    await errorRepo.AddAsync(new ErrorLog
                    {
                        RequestId = error.RequestId,
                        UserId = error.UserId,
                        ExceptionType = error.ExceptionType,
                        Message = error.Message,
                        StackTrace = error.StackTrace,
                        Source = error.Source,
                        FixStatus = Domain.Enums.ErrorFixStatus.Open,
                        CreatedAt = DateTime.UtcNow
                    }).ConfigureAwait(false);
                    await errorRepo.SaveChangesAsync().ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning("Unknown telemetry row type {Type}; dropped.", item.GetType().Name);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Telemetry-store failure is isolated: the (already committed) business
            // request is unaffected; the coverage gap is surfaced via the log for
            // reports/alerts (spec 20 "Bumped §1").
            _logger.LogWarning(ex, "Spec-20 telemetry row could not be persisted; row lost (best-effort contract).");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop accepting rows, then give the reader a bounded window to flush.
        _channel.Writer.TryComplete();
        if (ExecuteTask is not null)
            await Task.WhenAny(ExecuteTask, Task.Delay(ShutdownFlushTimeout, cancellationToken)).ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
