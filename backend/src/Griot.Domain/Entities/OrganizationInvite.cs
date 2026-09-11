using System;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

/// <summary>
/// Company-scoped invite chain (spec 29 / 32). Workspace invites remain on
/// <see cref="Invite"/>; these provision org membership (incl. the company
/// owner during onboarding) via Brevo email.
/// </summary>
public class OrganizationInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; }
    public Guid? CustomRoleId { get; set; }
    public virtual Role? CustomRole { get; set; }
    public string Token { get; set; } = string.Empty;
    /// <summary>Lifecycle of the org invite; resolves into an org membership on accept (spec 32).</summary>
    public OrganizationMemberStatus Status { get; set; } = OrganizationMemberStatus.Invited;
    public Guid InvitedById { get; set; }
    public virtual User InvitedBy { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
