using System;

namespace Griot.Application.DTOs;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAvatarUrl { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCommentRequest
{
    public string Body { get; set; } = string.Empty;
}

public class UpdateCommentRequest
{
    public string Body { get; set; } = string.Empty;
}
