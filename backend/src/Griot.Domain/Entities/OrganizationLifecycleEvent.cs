using System;

namespace Griot.Domain.Entities;

/// <summary>
/// Audit trail of company lifecycle transitions (spec 29 / 32 / 33):
/// Onboarded / Suspended / Reactivated / OffboardStarted / DataExported /
/// Offboarded / Purged. Complements (does not replace) spec-20 AuditLogs rows.
/// </summary>
public class OrganizationLifecycleEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public string Kind { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
