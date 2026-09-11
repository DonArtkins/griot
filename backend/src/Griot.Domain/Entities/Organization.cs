using System;
using System.Collections.Generic;

namespace Griot.Domain.Entities;

/// <summary>
/// The tenant root of the platform (spec 29): a Company onboarded by the
/// SuperAdmin. Every tenant-owned row below this hangs carries OrganizationId
/// (Pool tenancy model — one schema, one database).
/// </summary>
public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>The company Admin — owner of the company.</summary>
    public Guid OwnerId { get; set; }
    public virtual User Owner { get; set; } = null!;

    public Griot.Domain.Enums.OrganizationStatus Status { get; set; } = Griot.Domain.Enums.OrganizationStatus.Active;
    /// <summary>
    /// Display/billing metadata (spec 29). Mapped to column <c>PlanName</c>
    /// because <c>Plan</c> is a T-SQL reserved keyword.
    /// </summary>
    public Griot.Domain.Enums.OrganizationPlan PlanName { get; set; } = Griot.Domain.Enums.OrganizationPlan.Free;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? OffboardedAt { get; set; }

    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    public virtual ICollection<OrganizationInvite> Invites { get; set; } = new List<OrganizationInvite>();
    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
    public virtual ICollection<OrganizationLifecycleEvent> LifecycleEvents { get; set; } = new List<OrganizationLifecycleEvent>();
}
