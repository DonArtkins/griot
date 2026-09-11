using System;
using System.Collections.Generic;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public virtual Workspace Workspace { get; set; } = null!;

    /// <summary>Tenant owner (spec 29 — mirrors the parent workspace's org).</summary>
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Board> Boards { get; set; } = new List<Board>();
}
