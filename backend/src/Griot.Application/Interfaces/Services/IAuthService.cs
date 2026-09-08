using Griot.Application.DTOs.Auth;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Auth lifecycle: register (Argon2 hash), login (verify + issue), refresh (rotate), logout (revoke).
/// All token policy lives here; middleware/controllers only call this.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user. Returns 409 if email already exists.
    /// Password is hashed with Argon2id before storage.
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Validate credentials and issue an access + refresh token pair.
    /// Returns null when email/password mismatch (caller maps to 401).
    /// </summary>
    Task<AuthResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Rotate a refresh token: revoke the presented token, issue a new pair.
    /// Returns null if the token is invalid / expired / revoked.
    /// If the token has already been used (reuse attack) the whole family is revoked and null is returned.
    /// </summary>
    Task<AuthResponse?> RefreshAsync(string refreshToken);

    /// <summary>
    /// Revoke a specific refresh token (logout).
    /// Silently succeeds if the token is unknown (idempotent).
    /// </summary>
    Task LogoutAsync(string refreshToken);
}
