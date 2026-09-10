using System;
using Griot.Domain.Enums;

namespace Griot.Application.DTOs;

public class TaskItemDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Griot.Domain.Enums.TaskStatus Status { get; set; }
    public Priority Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeAvatarUrl { get; set; }
    public Guid CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTaskRequest
{
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Griot.Domain.Enums.TaskStatus Status { get; set; }
    public Priority Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Position { get; set; }
}

public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Griot.Domain.Enums.TaskStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? Position { get; set; }
}

public class MoveTaskRequest
{
    public Guid ColumnId { get; set; }
    public int NewPosition { get; set; }
}
