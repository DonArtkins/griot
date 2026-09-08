namespace Griot.Application.DTOs.Auth;

/// <summary>
/// Returned on successful register / login / refresh.
/// Contains a short-lived JWT access token + opaque refresh token.
/// </summary>
public class AuthResponse
{
    /// <summary>JWT access token (15-minute TTL). Include as `Authorization: Bearer &lt;token&gt;`.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Opaque refresh token. Store securely; submit to /api/auth/refresh.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Access token expiry (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Authenticated user summary.</summary>
    public AuthUserDto User { get; set; } = null!;
}

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
