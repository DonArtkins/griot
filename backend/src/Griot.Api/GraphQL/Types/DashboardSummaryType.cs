namespace Griot.Api.GraphQL.Types;

public class DashboardSummaryType
{
    public Dictionary<string, int> TaskCountsByStatus { get; set; } = new();
    public int UrgentOpenCount { get; set; }
    public List<ActivityLogType> RecentActivity { get; set; } = new();
}
