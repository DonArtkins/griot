using System;
using Griot.Domain.Enums;

namespace Griot.Application.DTOs;

public class ProjectDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; }
    public ProjectStatus? Status { get; set; }
}
