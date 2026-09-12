using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Griot.Application.Authorization;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Griot.Application.Services;

/// <summary>
/// Spec 30 — JWT v2 access-token issuer (claim builder). Claim set per
/// <c>docs/api/auth-contract.md</c>: <c>sub, email, name, jti, iss=Griot,
/// aud=GriotClients, org?, role, perms</c> (space-separated permission keys).
/// HS256, 15-minute TTL — lifetimes unchanged from implemented spec 07.
/// Refresh tokens stay opaque by design (64-hex, SHA-256 at rest) — this service
/// never sees them.
/// </summary>
public sealed class TokenService : ITokenService
{
    private static readonly TimeSpan AccessTokenTtl = TimeSpan.FromMinutes(15);

    /// <summary>SuperAdmin role string (platform operator — spec 29/30).</summary>
    public const string SuperAdminRole = "super_admin";

    public const string ClaimName = "name";
    public const string ClaimOrg = "org";
    public const string ClaimRole = "role";
    public const string ClaimPerms = "perms";

    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration) => _configuration = configuration;

    /// <inheritdoc />
    public (string Token, DateTime ExpiresAt) IssueAccessToken(
        Guid userId,
        string email,
        string displayName,
        ActiveOrganization session)
    {
        var (keyBytes, issuer, audience) = ResolveSigningMaterial();

        var expiresAt = DateTime.UtcNow.Add(AccessTokenTtl);

        // A `wid` (workspace id) claim is intentionally NOT embedded — workspace context
        // is selected per-request and checked against membership (see api-surface.md).
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimName, displayName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (session.OrganizationId is Guid organizationId)
            claims.Add(new Claim(ClaimOrg, organizationId.ToString()));

        claims.Add(new Claim(ClaimRole, session.Role));
        claims.Add(new Claim(ClaimPerms, session.Perms));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <inheritdoc />
    public void ValidateKeyPolicy(bool isProduction)
    {
        var key = _configuration["JWT:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT:Key is not configured (set JWT__Key).");

        var length = Encoding.UTF8.GetBytes(key).Length;
        var minimum = isProduction ? 64 : 32;
        if (length < minimum)
            throw new InvalidOperationException(
                $"JWT:Key must be at least {minimum} UTF-8 bytes ({(isProduction ? "512-bit production policy, CSPRNG-generated, secret-store only" : "HS256 minimum")}); configured key is {length} bytes. Set a longer JWT__Key.");
    }

    private (byte[] Key, string Issuer, string Audience) ResolveSigningMaterial()
    {
        var key = _configuration["JWT:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT:Key is not configured (set JWT__Key).");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException(
                $"JWT:Key must be at least 32 UTF-8 bytes (HS256 minimum); configured key is {keyBytes.Length} bytes. Set a longer JWT__Key.");

        return (keyBytes,
            _configuration["JWT:Issuer"] ?? "Griot",
            _configuration["JWT:Audience"] ?? "GriotClients");
    }
}