using System;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

public class Invite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public WorkspaceRole Role { get; set; }
    public string Token { get; set; } = string.Empty;
    public InviteStatus Status { get; set; }
    public Guid InvitedById { get; set; }
    public User InvitedBy { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
