using System;

namespace Griot.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public virtual TaskItem TaskItem { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public virtual User Author { get; set; } = null!;

    /// <summary>Tenant owner (spec 29 — mirrors the parent task's org).</summary>
    public Guid OrganizationId { get; set; }

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
