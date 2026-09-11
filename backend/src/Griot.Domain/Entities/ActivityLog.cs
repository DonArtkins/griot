using System;

namespace Griot.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public virtual Workspace Workspace { get; set; } = null!;

    /// <summary>Tenant owner (spec 29 — mirrors the parent workspace's org).</summary>
    public Guid OrganizationId { get; set; }

    public Guid ActorId { get; set; }
    public virtual User Actor { get; set; } = null!;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
