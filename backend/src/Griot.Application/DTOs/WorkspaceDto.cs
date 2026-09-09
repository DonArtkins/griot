using System;
using System.Collections.Generic;
using Griot.Domain.Enums;

namespace Griot.Application.DTOs;

public class WorkspaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<WorkspaceMemberDto> Members { get; set; } = new();
}

public class WorkspaceMemberDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public WorkspaceRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class CreateWorkspaceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class UpdateWorkspaceRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
}

public class AddMemberRequest
{
    public Guid UserId { get; set; }
    public WorkspaceRole Role { get; set; }
}

public class UpdateMemberRoleRequest
{
    public WorkspaceRole Role { get; set; }
}

public class CreateInviteRequest
{
    public string Email { get; set; } = string.Empty;
    public WorkspaceRole Role { get; set; }
}
