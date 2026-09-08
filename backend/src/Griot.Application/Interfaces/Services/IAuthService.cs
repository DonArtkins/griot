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

    /// <summary>
    /// Request a fresh 6-digit OTP code for the given purpose and deliver it via Resend
    /// using the branded email template. Returns null when the email is unknown;
    /// result.Success=false when Resend rejected the message (→ 502 at the controller).
    /// </summary>
    Task<OtpRequestResult?> RequestOtpAsync(OtpRequestRequest request, string? requestIp);

    /// <summary>
    /// Verify a submitted OTP code against the latest active challenge.
    /// Success consumes the challenge (and, for `email_verify`, marks the user's
    /// email verified). Repeated failures lock the challenge after 5 attempts.
    /// </summary>
    Task<OtpVerifyResult> VerifyOtpAsync(OtpVerifyRequest request);

    }
