using System;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public virtual Workspace Workspace { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public WorkspaceRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
