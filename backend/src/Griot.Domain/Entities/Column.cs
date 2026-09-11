using System;
using System.Collections.Generic;

namespace Griot.Domain.Entities;

public class Column
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BoardId { get; set; }
    public virtual Board Board { get; set; } = null!;

    /// <summary>Tenant owner (spec 29 — mirrors the parent board's org).</summary>
    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? WipLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}
