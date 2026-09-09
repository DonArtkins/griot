using System;
using Griot.Domain.Enums;

namespace Griot.Application.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string TargetRef { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
