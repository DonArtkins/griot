using Griot.Domain.Enums;

namespace Griot.Api.GraphQL.Types;

public class NotificationGraphQLType
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string TargetRef { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
