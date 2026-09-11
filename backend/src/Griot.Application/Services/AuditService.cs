using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Entities;

namespace Griot.Application.Services;

/// <summary>
/// Spec 20 audit trail: queues AuditLogs/ActivityLogs rows into the SAME scoped
/// change tracker the domain mutation uses, so the caller's next SaveChanges
/// commits domain state + audit + activity atomically (failure-isolation hard
/// rule: a lost audit rolls the mutation back). Rows are correlated to the
/// producing request via <see cref="IRequestContext"/>. Immediate durable writes
/// (auth events) commit their own transaction.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly IGenericRepository<AuditLog> _audit;
    private readonly IGenericRepository<ActivityLog> _activity;
    private readonly IRequestContext _requestContext;

    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public AuditService(
        IGenericRepository<AuditLog> audit,
        IGenericRepository<ActivityLog> activity,
        IRequestContext requestContext)
    {
        _audit = audit;
        _activity = activity;
        _requestContext = requestContext;
    }

    public Guid QueueActivity(ActivityEntry entry)
    {
        var row = new ActivityLog
        {
            WorkspaceId = entry.WorkspaceId,
            // Spec 29: log writers stamp the tenant at write time (nullable = legacy /
            // platform rows); fail-closed write paths resolve the org before queuing.
            OrganizationId = entry.OrganizationId,
            ActorId = entry.ActorId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Action = entry.Action,
            Payload = entry.Payload,
            CreatedAt = DateTime.UtcNow
        };
        _activity.AddAsync(row).GetAwaiter().GetResult();
        return row.Id;
    }

    public void QueueAudit(AuditEntry entry)
    {
        _audit.AddAsync(new AuditLog
        {
            ActivityId = entry.ActivityId,
            ActorId = entry.ActorId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Before = entry.Before,
            After = entry.After,
            RequestId = _requestContext.RequestId,
            // Spec 29: tenant stamp resolved by the caller at write time.
            OrganizationId = entry.OrganizationId,
            CreatedAt = DateTime.UtcNow
        }).GetAwaiter().GetResult();
    }

    public async Task RecordAsync(AuditEntry entry)
    {
        _audit.AddAsync(new AuditLog
        {
            ActivityId = entry.ActivityId,
            ActorId = entry.ActorId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            Before = entry.Before,
            After = entry.After,
            RequestId = _requestContext.RequestId,
            // Spec 29: tenant stamp resolved by the caller at write time.
            OrganizationId = entry.OrganizationId,
            CreatedAt = DateTime.UtcNow
        }).GetAwaiter().GetResult();
        // Durable without a domain write: commit its own transaction immediately.
        await _audit.SaveChangesAsync();
    }

    /// <summary>Serialize a Before/After snapshot from a plain entity or DTO (cycle-safe, non-throwing).</summary>
    public static string? Snapshot(object? value)
    {
        if (value is null) return null;
        try
        {
            return JsonSerializer.Serialize(value, SnapshotOptions);
        }
        catch (Exception)
        {
            return null; // A snapshot failure must not fail the business change.
        }
    }
}
