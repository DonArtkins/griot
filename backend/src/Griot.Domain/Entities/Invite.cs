using System;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

public class Invite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public virtual Workspace Workspace { get; set; } = null!;

    /// <summary>Tenant owner (spec 29 — mirrors the parent workspace's org).</summary>
    public Guid OrganizationId { get; set; }

    public string Email { get; set; } = string.Empty;
    public virtual WorkspaceRole Role { get; set; }
    public string Token { get; set; } = string.Empty;
    public InviteStatus Status { get; set; }
    public Guid InvitedById { get; set; }
    public virtual User InvitedBy { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
