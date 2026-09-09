using System;
using System.Collections.Generic;

namespace Griot.Application.DTOs;

public class ColumnDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? WipLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TaskItemDto> TaskItems { get; set; } = new();
}

public class CreateColumnRequest
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? WipLimit { get; set; }
}

public class UpdateColumnRequest
{
    public string? Name { get; set; }
    public int? Order { get; set; }
    public int? WipLimit { get; set; }
}
