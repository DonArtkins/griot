using System;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

/// <summary>
/// A user's membership in one company (spec 29 / 31). A user may belong to
/// many companies; the active one travels in the JWT v2 `org` claim (spec 30).
/// </summary>
public class OrganizationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public OrganizationRole Role { get; set; }

    /// <summary>
    /// Non-null only when <see cref="Role"/> == Custom — points at the
    /// company-defined <see cref="Role"/> row.
    /// </summary>
    public Guid? CustomRoleId { get; set; }
    public virtual Role? CustomRole { get; set; }

    public OrganizationMemberStatus Status { get; set; } = OrganizationMemberStatus.Invited;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
