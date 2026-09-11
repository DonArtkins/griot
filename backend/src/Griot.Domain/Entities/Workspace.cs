using System;
using System.Collections.Generic;

namespace Griot.Domain.Entities;

public class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public virtual User Owner { get; set; } = null!;

    /// <summary>
    /// Tenant owner (spec 29 — Pool model). Every workspace belongs to exactly
    /// one company; global query filters scope reads to the request tenant.
    /// </summary>
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public virtual ICollection<Invite> Invites { get; set; } = new List<Invite>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
