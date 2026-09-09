using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Griot.Application.DTOs.Auth;
using Griot.Application.Email;
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

    // Fixed valid Argon2id record at the same cost as registered users.
    private const string DummyPasswordHash =
        "000102030405060708090A0B0C0D0E0F:7E3ED151DF93C2D532176F71C174B83C3508E541010C86D28579A04BAC9FD517";

    // Refresh token: 32 raw bytes -> 64-char hex; stored as SHA-256 hash.
    private const int RefreshTokenBytes = 32;
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan AccessTokenTtl = TimeSpan.FromMinutes(15);

    // OTP 2FA (research/ai-features-research.md §1): crypto-secure 6-digit codes,
    // HMAC-SHA256 hashed at rest with a server-side pepper, 10-minute expiry,and
    // a 5-attempt lockout per challenge. Delivered via Brevo using the branded email template.

    private const int OtpCodeLength = 6;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);
    private const int OtpMaxAttempts = 5;
    private static readonly string[] OtpPurposes = { "email_verify", "login_2fa", "password_reset" };
    private const string OtpPepperDefault = "griot-dev-otp-pepper-change-me";

    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;
    private readonly IContactSynchronizer _contactSynchronizer;

    public AuthService(
        IAuthRepository authRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IEmailService emailService,
        IContactSynchronizer contactSynchronizer)
    {
        _authRepository = authRepository;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _contactSynchronizer = contactSynchronizer;
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

        // Email-OTP 2FA (research/ai-features-research.md §1.3): send the verification code
        // automatically with a branded Brevo email; delivery is best-effort so an email outage
        // never fails registration — the recipient can re-request via POST /api/auth/otp/request..
        await CreateOtpChallengeAndEmailAsync(
            user.Id, user.Email, user.DisplayName, "email_verify", requestIp: null, CancellationToken.None).ConfigureAwait(false);
        await SendNewAccountAdminEmailAsync(user).ConfigureAwait(false);

        // Synchronize the new user into Brevo as a contact (spec 12). This is the
        // trigger hook for the Welcome / Onboarding automations. Best-effort: a
        // Brevo outage must never fail registration.
        await _contactSynchronizer.UpsertContactAsync(user.Email, user.DisplayName, false, CancellationToken.None)
            .ConfigureAwait(false);

        return await IssueTokenPairAsync(user).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------------
    // Login
    // -----------------------------------------------------------------------------

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _authRepository.FindUserByEmailAsync(email).ConfigureAwait(false);

        // Unknown accounts must pay the same password hashing cost as existing accounts.
        var passwordValid = VerifyPassword(request.Password, user?.PasswordHash ?? DummyPasswordHash);
        if (user is null || !passwordValid)
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
        if (tokenHash is null)
            return null;

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
                .RevokeFamilyAsync(storedToken.UserId, storedToken.FamilyId, DateTime.UtcNow)
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
            FamilyId = storedToken.FamilyId,
            TokenHash = HashToken(rawNewRefresh)!,
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenTtl),
            CreatedAt = DateTime.UtcNow
        };

        var rotated = await _authRepository
            .RotateRefreshTokenAsync(storedToken, replacement)
            .ConfigureAwait(false);

        if (rotated is null)
        {
            await _authRepository
                .RevokeFamilyAsync(storedToken.UserId, storedToken.FamilyId, DateTime.UtcNow)
                .ConfigureAwait(false);
            return null;
        }

        _logger.LogInformation("Refresh token rotated for user {UserId}.", storedToken.UserId);

        return BuildAuthResponse(storedToken.User, rawNewRefresh);
    }

    // -----------------------------------------------------------------------------
    // Logout (revoke)
    // -----------------------------------------------------------------------------

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        if (tokenHash is null)
            return;
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

    public async Task<OtpRequestResult?> RequestOtpAsync(OtpRequestRequest request, string? requestIp)
    {
        if (!OtpPurposes.Contains(request.Purpose, StringComparer.Ordinal))
            return new OtpRequestResult { Success = false, Message = $"Unsupported purpose '{request.Purpose}'. Supported: email_verify, login_2fa, password_reset." };

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _authRepository.FindUserByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
            return null;

        var created = await CreateOtpChallengeAndEmailAsync(
            user.Id, user.Email, user.DisplayName, request.Purpose, requestIp, CancellationToken.None).ConfigureAwait(false);

        return created
            ? new OtpRequestResult { Success = true, Message = $"A 6-digit code has been sent to {email}." }
            : new OtpRequestResult { Success = false, Message = "Email delivery failed. Verify Brevo:ApiKey / BREVO_API_KEY and the sender address (verified in the Brevo dashboard)." };
    }

    public async Task<OtpVerifyResult> VerifyOtpAsync(OtpVerifyRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _authRepository.FindUserByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
            return new OtpVerifyResult { Verified = false, Message = "Unknown email address." };

        var challenge = await _authRepository.FindActiveOtpChallengeAsync(user.Id, request.Purpose).ConfigureAwait(false);
        if (challenge is null)
            return new OtpVerifyResult { Verified = false, Message = "No active code for this purpose. Request a new code." };

        if (challenge.AttemptCount >= OtpMaxAttempts)
        {
            await _authRepository.MarkOtpChallengeConsumedAsync(challenge.Id).ConfigureAwait(false);
            return new OtpVerifyResult { Verified = false, Message = "Too many attempts. Request a new code.", LockedOut = true };
        }

        var pepper = _configuration["Otp:Pepper"] ?? OtpPepperDefault;
        var candidateHash = HashOtpCode(request.Code, pepper);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(candidateHash),
                Encoding.UTF8.GetBytes(challenge.CodeHash)))
        {
            await _authRepository.AddOtpAttemptAsync(challenge.Id).ConfigureAwait(false);
            var attempts = challenge.AttemptCount + 1;
            if (attempts >= OtpMaxAttempts)
            {
                await _authRepository.MarkOtpChallengeConsumedAsync(challenge.Id).ConfigureAwait(false);
                return new OtpVerifyResult { Verified = false, Message = "Too many attempts. The code has been invalidated. Request a new one.", LockedOut = true };
            }
            return new OtpVerifyResult { Verified = false, Message = "Invalid code." };
        }

        await _authRepository.MarkOtpChallengeConsumedAsync(challenge.Id).ConfigureAwait(false);

        var emailVerified = false;
        if (request.Purpose == "email_verify")
        {
            await _authRepository.MarkEmailVerifiedAsync(user.Id).ConfigureAwait(false);
            emailVerified = true;

            // Flip the Brevo contact lifecycle attribute (ACCOUNT_STATUS=VERIFIED).
            // Automations listening on the attribute change fire here (spec 12).
            await _contactSynchronizer.MarkVerifiedAsync(user.Email, CancellationToken.None).ConfigureAwait(false);
        }

        _logger.LogInformation("OTP verified: {UserId} purpose={Purpose}", user.Id, request.Purpose);
        return new OtpVerifyResult { Verified = true, Message = "Code verified.", EmailVerified = emailVerified };
    }

    private async Task<bool> CreateOtpChallengeAndEmailAsync(
        Guid userId, string email, string displayName, string purpose, string? requestIp, CancellationToken ct)
    {
        var code = GenerateOtpCode();
        var pepper = _configuration["Otp:Pepper"] ?? OtpPepperDefault;

        await _authRepository.InvalidateOtpChallengesAsync(userId, purpose).ConfigureAwait(false);

        var challenge = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = HashOtpCode(code, pepper),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.Add(OtpTtl),
            AttemptCount = 0,
            Consumed = false,
            CreatedAt = DateTime.UtcNow,
            RequestIp = requestIp
        };
        await _authRepository.InsertOtpChallengeAsync(challenge).ConfigureAwait(false);

        var html = BrandedEmailTemplate.RenderOtpEmail(purpose, code, displayName, GetSiteUrl());
        var ok = await _emailService.SendAsync(
            new EmailMessage(email, BrandedEmailTemplate.OtpSubject(purpose), html, SenderKey: "security"), ct).ConfigureAwait(false);
        if (!ok)
            _logger.LogWarning("OTP email delivery failed for {Email} purpose={Purpose}", email, purpose);

        return ok;
    }

    private async Task SendNewAccountAdminEmailAsync(User user)
    {
        var adminTo = new[]
        {
            _configuration["Brevo:ContactToEmail"],
            _configuration["CONTACT_TO_EMAIL"]
        }.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
         ?? CanonicalAdminInbox;

        var html = BrandedEmailTemplate.RenderNewAccountAdminEmail(user.DisplayName, user.Email, GetSiteUrl());
        await _emailService.SendAsync(
            new EmailMessage(adminTo, "New user registered on Griot", html, SenderKey: "admin")).ConfigureAwait(false);
    }

    private static string GenerateOtpCode()
    {
        // Crypto-secure RNG (research §1.3) — never System.Random..

        var bytes = RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }

    private static string HashOtpCode(string code, string pepper)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), Encoding.UTF8.GetBytes(code)));

    private string GetSiteUrl()
        => (_configuration["SITE_URL"] ?? "https://griot.app").TrimEnd('/');


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
            FamilyId = Guid.NewGuid(),
            TokenHash = HashToken(rawRefresh)!,
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
    private const string CanonicalAdminInbox = "info.donartkins.ke@gmail.com";

    private (string Token, DateTime ExpiresAt) IssueJwt(User user)
    {
        var key = _configuration["JWT:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT:Key is not configured.");
        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException($"JWT:Key must be at least 32 UTF-8 bytes (HS256 minimum); configured key is {keyBytes.Length} bytes. Set a longer JWT__Key.");
        var issuer = _configuration["JWT:Issuer"] ?? "Griot";
        var audience = _configuration["JWT:Audience"] ?? "GriotClients";

        var signingKey = new SymmetricSecurityKey(keyBytes);
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

    /// <summary>Hash a 256-bit hexadecimal token, or reject malformed input.</summary>
    private static string? HashToken(string? token)
    {
        if (token is null || token.Length != RefreshTokenBytes * 2 || !token.All(Uri.IsHexDigit))
            return null;

        return Convert.ToHexString(SHA256.HashData(Convert.FromHexString(token)));
    }
}

/// <summary>Thrown by RegisterAsync when the email is already taken.</summary>
public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string message) : base(message) { }
}
