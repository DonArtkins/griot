using System;
using System.Collections.Generic;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Denormalized parent board id (legacy ERD column, kept NOT NULL by the
    /// InitialCreate migration). Always mirrors <see cref="Column"/>'s BoardId —
    /// set on create, kept in sync on move.
    /// </summary>
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public virtual Column Column { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Griot.Domain.Enums.TaskStatus Status { get; set; }
    public Priority Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public virtual User? Assignee { get; set; }
    public Guid CreatorId { get; set; }
    public virtual User Creator { get; set; } = null!;
    public DateTime? DueDate { get; set; }
    public decimal Position { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
