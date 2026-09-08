namespace Griot.Api.GraphQL.Types;

public class AttachmentType
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid UploaderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
