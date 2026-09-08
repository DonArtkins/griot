using Griot.Domain.Enums;

namespace Griot.Api.GraphQL.Types;

public class TaskItemType
{
    public Guid Id { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Griot.Domain.Enums.TaskStatus Status { get; set; }
    public Priority Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
