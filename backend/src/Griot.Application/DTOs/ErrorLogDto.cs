using System;

namespace Griot.Application.DTOs;

public class ErrorLogDto
{
    public Guid Id { get; set; }
    public Guid? RequestId { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Griot.Domain.Enums.ErrorFixStatus FixStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
