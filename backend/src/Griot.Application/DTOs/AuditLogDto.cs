using System;

namespace Griot.Application.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
    public DateTime CreatedAt { get; set; }
}
