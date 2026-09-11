using System;

namespace Griot.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActivityId { get; set; }
    public virtual ActivityLog? ActivityLog { get; set; }
    public Guid ActorId { get; set; }
    public virtual User Actor { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
    // ERD amendment (2026-09-11, spec 20): normalized request id of the producing
    // request/auth event; null only for durable writes with no HTTP context.
    public Guid? RequestId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
