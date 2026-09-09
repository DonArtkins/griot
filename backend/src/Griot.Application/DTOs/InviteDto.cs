
public class InviteDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Griot.Domain.Enums.WorkspaceRole Role { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
