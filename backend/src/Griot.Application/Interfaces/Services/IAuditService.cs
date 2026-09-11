using System;

namespace Griot.Application.Interfaces.Services;

/// <summary>Input for one AuditLogs row (spec 20). Before/After are JSON strings (null on create/delete).</summary>
public sealed record AuditEntry(
    Guid ActorId,
    string Action,
    string EntityType,
    Guid EntityId,
    string? Before,
    string? After,
    Guid? ActivityId = null);

/// <summary>Input for one ActivityLogs row (spec 20, user-facing subset of mutations).</summary>
public sealed record ActivityEntry(
    Guid WorkspaceId,
    Guid ActorId,
    string EntityType,
    Guid EntityId,
    string Action,
    string? Payload);

/// <summary>
/// Audit trail writer (spec 20, pipeline §3–4 + failure isolation hard rule):
/// AuditLogs/ActivityLogs are mandatory — they persist in the SAME SQL transaction
/// as the state change, so a lost audit can never hide behind a successful mutation.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Queue one ActivityLogs row into the current change tracker; commits atomically
    /// with the caller's next SaveChanges. Returns the row id for AuditLog.ActivityId linking.
    /// </summary>
    Guid QueueActivity(ActivityEntry entry);

    /// <summary>Queue one AuditLogs row into the current change tracker; commits atomically with the caller's next SaveChanges.</summary>
    void QueueAudit(AuditEntry entry);

    /// <summary>
    /// Immediate durable audit write for paths without a tracked domain write
    /// (auth events: login/refresh/replay-revoke/logout). Commits its own transaction.
    /// </summary>
    System.Threading.Tasks.Task RecordAsync(AuditEntry entry);
}
