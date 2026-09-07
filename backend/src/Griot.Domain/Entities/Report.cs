using System;

namespace Griot.Domain.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string GeneratedBy { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? PromptContext { get; set; }

    public Workspace Workspace { get; set; } = null!;
}
