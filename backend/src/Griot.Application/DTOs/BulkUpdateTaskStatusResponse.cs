namespace Griot.Application.DTOs;

public class BulkUpdateTaskStatusResponse
{
    public bool Success { get; set; }
    public int UpdatedCount { get; set; }
    public string? Message { get; set; }
}
