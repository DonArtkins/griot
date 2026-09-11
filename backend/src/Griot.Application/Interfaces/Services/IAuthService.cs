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
    /// Spec 30: <paramref name="requestedOrganizationId"/> (optional) pins the active
    /// organization of the re-issued access token (stateless tenant session — the client
    /// re-sends the active org id); when null, a single active membership is auto-picked,
    /// otherwise the platform view applies.
    /// </summary>
    Task<AuthResponse?> RefreshAsync(string refreshToken, Guid? requestedOrganizationId);

    /// <summary>
    /// Revoke a specific refresh token (logout).
    /// Silently succeeds if the token is unknown (idempotent).
    /// </summary>
    Task LogoutAsync(string refreshToken);

    /// <summary>
    /// Request a fresh 6-digit OTP code for the given purpose and deliver it via Brevo
    /// using the branded email template. Returns null when the email is unknown;
    /// result.Success=false when the email could not be delivered or configured
    /// (missing Brevo API key or verified sender, network failure, or Brevo rejection) → 502 at the controller.
    /// </summary>
    Task<OtpRequestResult?> RequestOtpAsync(OtpRequestRequest request, string? requestIp);

    /// <summary>
    /// Verify a submitted OTP code against the latest active challenge.
    /// Success consumes the challenge (and, for `email_verify`, marks the user's
    /// email verified). Repeated failures lock the challenge after 5 attempts.
    /// </summary>
    Task<OtpVerifyResult> VerifyOtpAsync(OtpVerifyRequest request);

    // ── Spec 30 (Auth & JWT v2): organization session + SuperAdmin bootstrap ──

    /// <summary>
    /// The caller's active memberships (GET /api/auth/organizations). Only memberships
    /// whose organization exists with Status == Active AND whose member Status == Active
    /// are listed. Returns an empty list for unknown callers — never throws.
    /// </summary>
    Task<IReadOnlyList<OrganizationMembershipDto>> ListOrganizationsAsync(
        Guid callerId, bool isSuperAdmin, Guid? activeOrganizationId);

    /// <summary>
    /// Switch the active organization (POST /api/auth/select-organization). Validates
    /// membership (404 unknown org / 403 not an Active member of an Active org), then
    /// issues a fresh token pair carrying the new `org`/`role`/`perms` claims. When
    /// <paramref name="refreshToken"/> is presented its family is revoked after the new
    /// pair is minted (single active session per family). SuperAdmin callers may select
    /// an org without holding a member row (platform authority). Returns null when the
    /// caller id could not be resolved (401 at the controller).
    /// </summary>
    /// <exception cref="Griot.Application.Services.DomainError">NotFound (unknown org) / Forbidden (not an active member).</exception>
    Task<AuthResponse?> SelectOrganizationAsync(
        Guid callerId, SelectOrganizationRequest request, string? refreshToken);

    /// <summary>
    /// SuperAdmin bootstrap (spec 30): when <c>SUPERADMIN__EMAIL</c> is configured, the
    /// matching user is created (Argon2id password from <c>SUPERADMIN__PASSWORD</c>) or
    /// upgraded (<c>PlatformRole=SuperAdmin</c>) idempotently — never duplicates, never
    /// throws on audit failure, and no-ops without configuration.
    /// </summary>
    Task<SuperAdminBootstrapResult> EnsureSuperAdminAsync();
    }
