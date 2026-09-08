namespace Griot.Api.GraphQL.Types;

public class ActivityLogType
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ActorId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}
