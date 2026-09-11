using System;

namespace Griot.Domain.Entities;

public class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public virtual TaskItem TaskItem { get; set; } = null!;
    public Guid UploaderId { get; set; }
    public virtual User Uploader { get; set; } = null!;

    /// <summary>Tenant owner (spec 29 — mirrors the parent task's org).</summary>
    public Guid OrganizationId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
