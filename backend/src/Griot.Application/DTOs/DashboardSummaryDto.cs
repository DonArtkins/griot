using System;
using System.Collections.Generic;

namespace Griot.Application.DTOs;

public class DashboardSummaryDto
{
    public Dictionary<string, int> TaskCountsByStatus { get; set; } = new();
    public int UrgentOpenCount { get; set; }
    public List<ActivityLogDto> RecentActivity { get; set; } = new();
}

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActorName { get; set; }
    public string? ActorAvatarUrl { get; set; }
}
