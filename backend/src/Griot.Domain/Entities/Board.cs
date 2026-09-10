using System;
using System.Collections.Generic;

namespace Griot.Domain.Entities;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Column> Columns { get; set; } = new List<Column>();
}
