using System;

namespace Griot.Application.DTOs;

public class AttachmentDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UploaderId { get; set; }
    public string? UploaderName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
