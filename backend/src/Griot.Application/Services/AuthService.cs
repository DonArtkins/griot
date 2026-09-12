using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Griot.Application.Authorization;
using Griot.Application.DTOs.Auth;
using Griot.Application.Email;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
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
    private readonly IAuditService _auditService;
    private readonly ITokenService _tokenService;

    public AuthService(
        IAuthRepository authRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IEmailService emailService,
        IAuditService auditService,
        ITokenService tokenService)
    {
        _authRepository = authRepository;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _auditService = auditService;
        _tokenService = tokenService;
    }

    // -----------------------------------------------------------------------------
    // Spec 30: JWT v2 tenant-session resolution (role/perms derive at issue time)
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Resolves the caller's tenant session: the given organization id is honored when
    /// the user is an Active member of an Active organization (or is the platform
    /// SuperAdmin, whose `org` selection is platform authority). A null/unresolvable
    /// organization id yields a platform-only session — the `org` claim is then omitted.
    /// Member roles derive from <see cref="RoleSelection"/>; SuperAdmin resolves first
    /// per tenancy-guide §3 precedence.
    /// </summary>
    private async Task<ActiveOrganization> ResolveSessionAsync(User user, Guid? requestedOrganizationId)
    {
        var isSuperAdmin = user.PlatformRole == PlatformRole.SuperAdmin;

        if (requestedOrganizationId is Guid organizationId)
        {
            var member = await _authRepository
                .FindActiveOrganizationMemberAsync(organizationId, user.Id)
                .ConfigureAwait(false);

            if (member is not null)
            {
                // Tenancy-guide §3: the platform role resolves first — a SuperAdmin's
                // session always carries the full permission set (with or without a
                // membership row); a Member membership must never produce
                // role=super_admin with empty perms.
                return new ActiveOrganization(
                    organizationId,
                    isSuperAdmin
                        ? RoleSelection.RoleSuperAdmin
                        : RoleSelection.EffectiveRole(isSuperAdmin: false, member),
                    isSuperAdmin
                        ? PermissionCatalogue.Join(PermissionCatalogue.All)
                        : RoleSelection.EffectivePerms(member));
            }

            // SuperAdmin may act on any company without a member row (platform authority).
            if (isSuperAdmin)
                return new ActiveOrganization(organizationId, RoleSelection.RoleSuperAdmin, PermissionCatalogue.Join(PermissionCatalogue.All));

            return new ActiveOrganization(null, RoleSelection.RoleMember, string.Empty);
        }

        // Platform-only session: SuperAdmin keeps its platform role with full perms;
        // everyone else falls back to an identity-only member session (no `org` claim).
        return isSuperAdmin
            ? new ActiveOrganization(null, RoleSelection.RoleSuperAdmin, PermissionCatalogue.Join(PermissionCatalogue.All))
            : new ActiveOrganization(null, RoleSelection.RoleMember, string.Empty);
    }

    // -----------------------------------------------------------------------------
    // Auth audit events (spec 20 pipeline §6)
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Spec 20 (pipeline §6): login success/fail, refresh rotate, replay-revoke and
    /// logout each write one durable <c>AuditLogs</c> row (<c>Auth.Login</c>,
    /// <c>Auth.Refresh</c>, <c>Auth.RefreshReplayRevoked</c>, <c>Auth.Logout</c>) so
    /// security incidents are reconstructable. Auth writes go through
    /// <see cref="IAuthRepository"/> (no shared tracked transaction), so the event
    /// commits its own transaction via <see cref="IAuditService.RecordAsync"/> and is
    /// BEST-EFFORT: if the durable audit store is unavailable the auth outcome stands
    /// (a 401 must never become a 500) and the coverage gap is logged for the
    /// observability pipeline (failure-isolation rule, "documented unavailable result").
    /// No secrets: only outcomes, family ids and user ids — never tokens, hashes or codes.
    /// </summary>
    private async Task RecordAuthEventAsync(Guid actorId, string action, Guid entityId, object? after = null, Guid? organizationId = null)
    {
        try
        {
            await _auditService.RecordAsync(new AuditEntry(
                actorId, action, "User", entityId,
                Before: null,
                After: after is null ? null : AuditService.Snapshot(after),
                OrganizationId: organizationId)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Spec-20 auth audit row could not be persisted (action={Action}, user={UserId}); the auth outcome is unaffected.",
                action, actorId);
        }
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
            if (user is not null)
            {
                // Attributable failure (account exists, credentials wrong). Unknown
                // emails have no actor id to audit and are already rate-limited/logged.
                await RecordAuthEventAsync(user.Id, "Auth.Login", user.Id, new { outcome = "failed" }).ConfigureAwait(false);
            }
            return null;
        }

        _logger.LogInformation("User logged in: {UserId}", user.Id);
        await RecordAuthEventAsync(user.Id, "Auth.Login", user.Id, new { outcome = "success" }).ConfigureAwait(false);
        return await IssueTokenPairAsync(user).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------------
    // Refresh (rotate)
    // -----------------------------------------------------------------------------

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, Guid? requestedOrganizationId)
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
            await RecordAuthEventAsync(
                storedToken.UserId, "Auth.RefreshReplayRevoked", storedToken.UserId,
                new { storedToken.FamilyId, outcome = "reuse-revoked" }).ConfigureAwait(false);
            return null;
        }

        // Expired token.
        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogInformation("Expired refresh token for user {UserId}.", storedToken.UserId);
            return null;
        }

        // Atomic rotation: revoke the presented token, link it to the replacement, persist both.
        // FamilyId is copied verbatim — the rotation chain IS the family (spec 07 reuse
        // detection revokes the whole chain on replay), so it never changes per org.
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

        // Spec 30 (Option A, stateless sessions): re-derive the JWT v2 tenant session on
        // every rotation (role changes land within one refresh, never mid-access-token).
        // The client pins the active org by re-sending its id on refresh; absent, a user
        // with exactly ONE active membership gets it auto-picked, otherwise the platform
        // view applies and the client re-selects via POST /api/auth/select-organization.
        // CodeRabbit fix: the session (including the auto-pick DB reads) resolves BEFORE
        // the rotation commits — a session-resolution failure then rolls nothing back,
        // and the replacement is never minted against an unresolvable session.
        var session = await ResolveSessionAsync(
            storedToken.User,
            requestedOrganizationId ?? await AutoPickOrganizationIdAsync(storedToken.User).ConfigureAwait(false))
            .ConfigureAwait(false);

        var rotated = await _authRepository
            .RotateRefreshTokenAsync(storedToken, replacement)
            .ConfigureAwait(false);

        if (rotated is null)
        {
            await _authRepository
                .RevokeFamilyAsync(storedToken.UserId, storedToken.FamilyId, DateTime.UtcNow)
                .ConfigureAwait(false);
            await RecordAuthEventAsync(
                storedToken.UserId, "Auth.RefreshReplayRevoked", storedToken.UserId,
                new { storedToken.FamilyId, outcome = "rotation-conflict-revoked" }).ConfigureAwait(false);
            return null;
        }

        var session = await ResolveSessionAsync(
            storedToken.User,
            requestedOrganizationId ?? await AutoPickOrganizationIdAsync(storedToken.User).ConfigureAwait(false))
            .ConfigureAwait(false);

        _logger.LogInformation("Refresh token rotated for user {UserId}.", storedToken.UserId);
        await RecordAuthEventAsync(
            storedToken.UserId, "Auth.Refresh", storedToken.UserId,
            new { storedToken.FamilyId, outcome = "rotated" },
            session.OrganizationId).ConfigureAwait(false);

        return BuildAuthResponse(storedToken.User, rawNewRefresh, session);
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
        await RecordAuthEventAsync(
            stored.UserId, "Auth.Logout", stored.UserId,
            new { stored.FamilyId, outcome = "revoked" }).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------------
    // Spec 30: organization session (list + select) & SuperAdmin bootstrap
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Stateless session convenience (spec 30): when the caller pins no organization,
    /// a user with EXACTLY ONE active membership gets it auto-picked. Zero memberships
    /// or more than one → null (platform view; the client re-selects explicitly).
    /// </summary>
    private async Task<Guid?> AutoPickOrganizationIdAsync(User user)
    {
        var memberships = await _authRepository
            .GetActiveOrganizationMembershipsAsync(user.Id)
            .ConfigureAwait(false);
        return memberships.Count == 1 ? memberships[0].OrganizationId : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrganizationMembershipDto>> ListOrganizationsAsync(
        Guid callerId, bool isSuperAdmin, Guid? activeOrganizationId)
    {
        var memberships = await _authRepository
            .GetActiveOrganizationMembershipsAsync(callerId)
            .ConfigureAwait(false);

        return memberships.Select(m => new OrganizationMembershipDto
        {
            OrganizationId = m.OrganizationId,
            OrganizationName = m.Organization.Name,
            Role = RoleSelection.EffectiveRole(isSuperAdmin, m),
            JoinedAt = m.JoinedAt,
            IsActive = activeOrganizationId.HasValue && m.OrganizationId == activeOrganizationId.Value
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<AuthResponse?> SelectOrganizationAsync(
        Guid callerId, SelectOrganizationRequest request, string? refreshToken)
    {
        if (callerId == Guid.Empty)
            return null;

        // CodeRabbit fix (CWE-613): the presented refresh token is REQUIRED and is
        // validated FIRST — before any organization lookup and before anything is
        // minted. Missing, unknown, revoked, expired or foreign tokens reject the
        // switch with 401, so a stolen ACCESS token alone can never create a
        // renewable session.
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new DomainError(DomainErrorKind.Validation,
                "A refresh token from the session being switched is required.");

        var presentedHash = HashToken(refreshToken);
        var presented = presentedHash is null
            ? null
            : await _authRepository
                .FindRefreshTokenByTokenHashAsync(presentedHash)
                .ConfigureAwait(false);
        if (presented is null
            || presented.RevokedAt is not null
            || presented.ExpiresAt < DateTime.UtcNow
            || presented.UserId != callerId)
            return null; // → 401 at the controller

        var organization = await _authRepository
            .FindOrganizationByIdAsync(request.OrganizationId)
            .ConfigureAwait(false);
        if (organization is null)
            throw new DomainError(DomainErrorKind.NotFound, "Organization not found.");

        var user = await _authRepository.FindUserByIdAsync(callerId).ConfigureAwait(false);
        if (user is null)
            return null;

        var isSuperAdmin = user.PlatformRole == PlatformRole.SuperAdmin;

        // SuperAdmin may select any Active company without holding a member row
        // (platform authority, spec 29/30). Everyone else must be an Active member
        // of the Active organization — anything else fails closed (403).
        var session = await ResolveSessionAsync(user, request.OrganizationId).ConfigureAwait(false);
        if (session.OrganizationId is null)
            throw new DomainError(DomainErrorKind.Forbidden, "Not an active member of this organization.");

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
        // Atomic swap: revoke the presented family + insert the replacement in one
        // transaction — no minted-but-unavailable token, no still-refreshable old family.
        await _authRepository
            .SwapRefreshFamilyAsync(refreshEntity, presented.FamilyId, DateTime.UtcNow)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Organization session switched for user {UserId}: org {OrganizationId} (role {Role}).",
            user.Id, organization.Id, session.Role);
        await RecordAuthEventAsync(
            user.Id, "Auth.SelectOrganization", user.Id,
            new { organizationId = organization.Id, role = session.Role, outcome = "switched" },
            organization.Id).ConfigureAwait(false);

        return BuildAuthResponse(user, rawRefresh, session);
    }

    /// <inheritdoc />
    public async Task<SuperAdminBootstrapResult> EnsureSuperAdminAsync()
    {
        var email = _configuration["SUPERADMIN:Email"]?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return new SuperAdminBootstrapResult(false, false);

        var password = _configuration["SUPERADMIN:Password"];
        var user = await _authRepository.FindUserByEmailAsync(email).ConfigureAwait(false);

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning(
                    "SUPERADMIN__Email is configured but no user exists and SUPERADMIN__Password is not set; bootstrap skipped.");
                return new SuperAdminBootstrapResult(false, false);
            }

            var (hash, salt) = HashPassword(password);
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = "SuperAdmin",
                PasswordHash = $"{Convert.ToHexString(salt)}:{hash}",
                PlatformRole = PlatformRole.SuperAdmin,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _authRepository.AddUserAsync(user).ConfigureAwait(false);
            await _authRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("SuperAdmin bootstrap: created user {UserId} ({Email}).", user.Id, email);
            await RecordAuthEventAsync(user.Id, "Auth.SuperAdminBootstrap", user.Id,
                new { outcome = "created" }).ConfigureAwait(false);
            return new SuperAdminBootstrapResult(true, false);
        }

        if (user.PlatformRole != PlatformRole.SuperAdmin)
        {
            // Upgrade in place: the existing account gains platform authority (idempotent).
            await _authRepository
                .SetPlatformRoleAsync(user.Id, PlatformRole.SuperAdmin)
                .ConfigureAwait(false);

            _logger.LogInformation("SuperAdmin bootstrap: upgraded existing user {UserId} ({Email}).", user.Id, email);
            await RecordAuthEventAsync(user.Id, "Auth.SuperAdminBootstrap", user.Id,
                new { outcome = "upgraded" }).ConfigureAwait(false);
            return new SuperAdminBootstrapResult(false, true);
        }

        return new SuperAdminBootstrapResult(false, false);
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
            new EmailMessage(email, BrandedEmailTemplate.OtpSubject(purpose), html), ct).ConfigureAwait(false);
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
            new EmailMessage(adminTo, "New user registered on Griot", html)).ConfigureAwait(false);
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

    /// <summary>
    /// Issue a new JWT access token + persist a new refresh token row. Returns the token pair.
    /// Spec 30: the tenant session resolves here (optional org pin / single-membership
    /// auto-pick / platform view) so every issued pair carries the same active org.
    /// </summary>
    private async Task<AuthResponse> IssueTokenPairAsync(User user, Guid? requestedOrganizationId = null)
    {
        var session = await ResolveSessionAsync(
            user,
            requestedOrganizationId ?? await AutoPickOrganizationIdAsync(user).ConfigureAwait(false))
            .ConfigureAwait(false);

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

        return BuildAuthResponse(user, rawRefresh, session);
    }

    /// <summary>
    /// Build the response DTO. The JWT is issued by <see cref="ITokenService"/> (spec 30
    /// v2 claim builder) — never inline, never in repositories or controllers.
    /// </summary>
    private AuthResponse BuildAuthResponse(User user, string rawRefreshToken, ActiveOrganization session)
    {
        var (accessToken, expiresAt) = _tokenService.IssueAccessToken(user.Id, user.Email, user.DisplayName, session);

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

    private const string CanonicalAdminInbox = "info.donartkins.ke@gmail.com";

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
