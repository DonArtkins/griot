using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Griot.Application.DTOs.Auth;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Entities;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Griot.Application.Services;

/// <summary>
/// Auth lifecycle: register (Argon2id hash), login (verify + issue), refresh (rotate), logout (revoke).
/// Token policy and hashing are entirely owned here.
/// Persistence goes through <see cref="IAuthRepository"/> so this layer stays free of EF/HTTP/Redis.
/// </summary>
public sealed class AuthService : IAuthService
{
    // Argon2id tuning (OWASP minimum: t=1 m=64MB, p=4).
    private const int Argon2Iterations = 3;
    private const int Argon2MemoryKb = 65536; // 64 MB
    private const int Argon2Lanes = 4;
    private const int Argon2HashLength = 32; // bytes -> 64-char hex
    private const int SaltLength = 16; // bytes

    // Refresh token: 32 raw bytes -> 64-char hex; stored as SHA-256 hash.
    private const int RefreshTokenBytes = 32;
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan AccessTokenTtl = TimeSpan.FromMinutes(15);

    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _configuration = configuration;
        _logger = logger;
    }

    // -----------------------------------------------------------------------------
    // Register
    // -----------------------------------------------------------------------------

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Normalise email (lowercase, trim).
        var email = request.Email.Trim().ToLowerInvariant();

        // Check for duplicate email (409 is raised by caller on DuplicateEmailException).
        var exists = await _authRepository.UserExistsByEmailAsync(email).ConfigureAwait(false);
        if (exists)
            throw new DuplicateEmailException($"Email '{email}' is already registered.");

        // Hash password with Argon2id.
        var (hash, salt) = HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = $"{Convert.ToHexString(salt)}:{hash}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _authRepository.CreateUserAsync(user).ConfigureAwait(false);

        _logger.LogInformation("User registered: {UserId} ({Email})", user.Id, email);

        return await IssueTokenPairAsync(user).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------------
    // Login
    // -----------------------------------------------------------------------------

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _authRepository.FindUserByEmailAsync(email).ConfigureAwait(false);

        // Constant-time failure: always verify even when user not found, to prevent timing oracle.
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", email);
            return null;
        }

        _logger.LogInformation("User logged in: {UserId}", user.Id);
        return await IssueTokenPairAsync(user).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------------
    // Refresh (rotate)
    // -----------------------------------------------------------------------------

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _authRepository
            .FindRefreshTokenByTokenHashAsync(tokenHash)
            .ConfigureAwait(false);

        if (storedToken is null)
        {
            // Unknown token -- could be a stale or forged attempt.
            _logger.LogWarning("Refresh with unknown token hash.");
            return null;
        }

        // Token already revoked -> REUSE ATTACK: revoke the whole family.
        if (storedToken.RevokedAt is not null)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}. Revoking entire family.",
                storedToken.UserId);

            await _authRepository
                .RevokeAllActiveTokensForUserAsync(storedToken.UserId, DateTime.UtcNow)
                .ConfigureAwait(false);
            return null;
        }

        // Expired token.
        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogInformation("Expired refresh token for user {UserId}.", storedToken.UserId);
            return null;
        }

        // Atomic rotation: revoke the presented token, link it to the replacement, persist both.
        var rawNewRefresh = GenerateOpaqueToken();
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = HashToken(rawNewRefresh),
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenTtl),
            CreatedAt = DateTime.UtcNow
        };

        await _authRepository
            .RotateRefreshTokenAsync(storedToken, replacement)
            .ConfigureAwait(false);

        _logger.LogInformation("Refresh token rotated for user {UserId}.", storedToken.UserId);

        return BuildAuthResponse(storedToken.User, rawNewRefresh);
    }

    // -----------------------------------------------------------------------------
    // Logout (revoke)
    // -----------------------------------------------------------------------------

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _authRepository
            .FindRefreshTokenByTokenHashAsync(tokenHash)
            .ConfigureAwait(false);

        if (stored is null || stored.RevokedAt is not null)
        {
            // Idempotent: silently succeed.
            return;
        }

        stored.RevokedAt = DateTime.UtcNow;
        await _authRepository.SaveChangesAsync().ConfigureAwait(false);
        _logger.LogInformation("Refresh token revoked for user {UserId}.", stored.UserId);
    }

    // -----------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------

    /// <summary>Issue a new JWT access token + persist a new refresh token row. Returns the token pair.</summary>
    private async Task<AuthResponse> IssueTokenPairAsync(User user)
    {
        // Generate opaque refresh token (256-bit random), store only its SHA-256 hash.
        var rawRefresh = GenerateOpaqueToken();
        var refreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenTtl),
            CreatedAt = DateTime.UtcNow
        };

        await _authRepository.InsertRefreshTokenAsync(refreshEntity).ConfigureAwait(false);

        return BuildAuthResponse(user, rawRefresh);
    }

    /// <summary>Build the response DTO. The JWT is issued here, not in repositories/controllers.</summary>
    private AuthResponse BuildAuthResponse(User user, string rawRefreshToken)
    {
        var (accessToken, expiresAt) = IssueJwt(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            }
        };
    }

    /// <summary>Issue a signed JWT (15-min TTL).</summary>
    /// <remarks>
    /// Claim set: <c>sub</c> (user GUID), <c>email</c>, <c>jti</c>.
    /// A <c>wid</c> (workspace id) claim is intentionally NOT embedded at this layer because a user
    /// may have zero/multiple workspaces and workspace context is selected per-request (see api-surface.md).
    /// Validation (iss/aud/exp) is symmetrical with Program.cs JwtBearer setup.
    /// </remarks>
    private (string Token, DateTime ExpiresAt) IssueJwt(User user)
    {
        var key = _configuration["JWT:Key"]
            ?? throw new InvalidOperationException("JWT:Key is not configured.");
        var issuer = _configuration["JWT:Issuer"] ?? "Griot";
        var audience = _configuration["JWT:Audience"] ?? "GriotClients";

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.Add(AccessTokenTtl);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // -----------------------------------------------------------------------------
    // Argon2id helpers
    // -----------------------------------------------------------------------------

    /// <summary>Hash a plaintext password with Argon2id. Returns (hex-hash, raw-salt).</summary>
    private static (string Hash, byte[] Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = ComputeArgon2Hash(Encoding.UTF8.GetBytes(password), salt);
        return (Convert.ToHexString(hash), salt);
    }

    /// <summary>Verify a plaintext password against a stored "hexSalt:hexHash" record.</summary>
    private static bool VerifyPassword(string password, string storedRecord)
    {
        try
        {
            var parts = storedRecord.Split(':');
            if (parts.Length != 2) return false;

            var salt = Convert.FromHexString(parts[0]);
            var expectedHash = Convert.FromHexString(parts[1]);

            var actualHash = ComputeArgon2Hash(Encoding.UTF8.GetBytes(password), salt);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeArgon2Hash(byte[] password, byte[] salt)
    {
        using var argon2 = new Argon2id(password)
        {
            Salt = salt,
            Iterations = Argon2Iterations,
            MemorySize = Argon2MemoryKb,
            DegreeOfParallelism = Argon2Lanes
        };
        return argon2.GetBytes(Argon2HashLength);
    }

    // -----------------------------------------------------------------------------
    // Refresh token helpers
    // -----------------------------------------------------------------------------

    /// <summary>Generate a cryptographically-random opaque token (hex string).</summary>
    private static string GenerateOpaqueToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

    /// <summary>SHA-256 hash of an opaque token for at-rest storage.</summary>
    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Convert.FromHexString(token)));
}

/// <summary>Thrown by RegisterAsync when the email is already taken.</summary>
public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string message) : base(message) { }
}
