using System;
using System.Collections.Generic;

namespace Griot.Domain.Entities;

/// <summary>
/// A named role inside one company (spec 29 / 31). System roles
/// (Owner/Admin/ProjectManager/Member/Client) are seeded per company
/// (`IsSystem = true`, undeletable); company Admin/PMs may create additional
/// custom roles composed ONLY from the fixed permission catalogue
/// (`PermissionCatalogue` in Application).
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }

    /// <summary>Comma-separated permission keys (catalogue only — validated on write).</summary>
    public string Permissions { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<OrganizationInvite> Invites { get; set; } = new List<OrganizationInvite>();
}
