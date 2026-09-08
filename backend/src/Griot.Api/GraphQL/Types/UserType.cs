using Griot.Domain.Entities;
using Griot.Domain.Enums;

namespace Griot.Api.GraphQL.Types;

public class UserType
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public TwoFactorMethod TwoFactorMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
