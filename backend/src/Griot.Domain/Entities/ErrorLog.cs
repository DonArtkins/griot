using System;
using Griot.Domain.Enums;

namespace Griot.Domain.Entities;

public class ErrorLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RequestId { get; set; }
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public virtual ErrorFixStatus FixStatus { get; set; }
    public Guid? SolvedByUserId { get; set; }
    public virtual User? SolvedByUser { get; set; }

    /// <summary>
    /// Tenant owner (spec 29) — nullable: null = platform-level event.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    public DateTime? FixedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
