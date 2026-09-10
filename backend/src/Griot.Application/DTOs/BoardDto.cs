using System;
using System.Collections.Generic;

namespace Griot.Application.DTOs;

public class BoardDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ColumnDto> Columns { get; set; } = new();
}

public class CreateBoardRequest
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateBoardRequest
{
    public string? Name { get; set; }
    public int? Order { get; set; }
}
