using Griot.Application.Authorization;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Spec 30: single owner of JWT v2 access-token claim construction, key policy and
/// issuance. Middleware only validates; controllers and services never build claims
/// themselves (separation of concerns — the spec-30 claim builder).
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Builds and signs a 15-minute HS256 access token with the v2 claim set:
    /// <c>sub, email, name, jti, iss, aud, org?, role, perms</c> (space-separated
    /// permission keys). The `org` claim is omitted when
    /// <see cref="ActiveOrganization.OrganizationId"/> is null (platform-only session).
    /// Returns the compact JWT and its UTC expiry.
    /// </summary>
    (string Token, DateTime ExpiresAt) IssueAccessToken(
        Guid userId,
        string email,
        string displayName,
        ActiveOrganization session);

    /// <summary>
    /// Signing-key policy (spec 30): <c>JWT:Key</c> must be at least 32 UTF-8 bytes
    /// (HS256 floor) everywhere and at least 64 UTF-8 bytes (512 bits) when
    /// <paramref name="isProduction"/> is true. Throws
    /// <see cref="InvalidOperationException"/> on violation.
    /// </summary>
    void ValidateKeyPolicy(bool isProduction);
}