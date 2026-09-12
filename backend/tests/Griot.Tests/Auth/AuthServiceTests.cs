using System.Security.Cryptography;
using Griot.Application.Authorization;
using Griot.Application.DTOs.Auth;
using Griot.Application.Interfaces.Services;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Griot.Tests.Auth;

/// <summary>
/// Unit tests for <see cref="AuthService"/>.
/// Covers: Refresh_Cannot_Be_Replayed (family revoke on token reuse),
///         expired refresh returns null, valid refresh rotates correctly,
///         duplicate email registration throws, and happy-path login + register.
/// </summary>
public class AuthServiceTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Key"]      = "test-signing-key-must-be-at-least-256bits-long-padding-here",
                ["JWT:Issuer"]   = "Griot",
                ["JWT:Audience"] = "GriotClients",
                ["Otp:Pepper"] = "test-otp-pepper"
            })
            .Build();

    private static IEmailService BuildEmailService()
    {
        var mock = new Mock<IEmailService>();
        mock.Setup(m => m.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return mock.Object;

    }

    private static IAuditService BuildAuditService(out Mock<IAuditService> auditMock)
    {
        auditMock = new Mock<IAuditService>();
        auditMock.Setup(a => a.RecordAsync(It.IsAny<AuditEntry>())).Returns(Task.CompletedTask);
        return auditMock.Object;
    }

    private static ITokenService BuildTokenService() =>
        new TokenService(BuildConfig());

    /// <summary>
    /// Spec 30: loose default stubs for the organization-session lookups so existing
    /// fixtures keep their legacy behavior (no memberships, no active member).
    /// Test-specific setups override these — last Moq setup wins.
    /// </summary>
    private static void ApplySpec30Defaults(Mock<IAuthRepository> repo)
    {
        repo.Setup(r => r.GetActiveOrganizationMembershipsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((System.Collections.Generic.IReadOnlyList<OrganizationMember>)new System.Collections.Generic.List<OrganizationMember>());
        repo.Setup(r => r.FindActiveOrganizationMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((OrganizationMember?)null);
    }

    private static AuthService BuildService(Mock<IAuthRepository> repoMock, bool applySpec30Defaults = true)
    {
        if (applySpec30Defaults) ApplySpec30Defaults(repoMock);
        return new(repoMock.Object, BuildConfig(), Mock.Of<ILogger<AuthService>>(), BuildEmailService(), BuildAuditService(out _), BuildTokenService());
    }

    private static AuthService BuildService(Mock<IAuthRepository> repoMock, IEmailService emailService, bool applySpec30Defaults = true)
    {
        if (applySpec30Defaults) ApplySpec30Defaults(repoMock);
        return new(repoMock.Object, BuildConfig(), Mock.Of<ILogger<AuthService>>(), emailService, BuildAuditService(out _), BuildTokenService());
    }

    private static AuthService BuildService(Mock<IAuthRepository> repoMock, out Mock<IAuditService> auditMock, bool applySpec30Defaults = true)
    {
        if (applySpec30Defaults) ApplySpec30Defaults(repoMock);
        return new(repoMock.Object, BuildConfig(), Mock.Of<ILogger<AuthService>>(), BuildEmailService(), BuildAuditService(out auditMock), BuildTokenService());
    }

    /// <summary>Spec 30: sha-256 → 64-hex → UTF-8 bytes ≥ 32; decodes verbatim with the configured key.</summary>
    private static TokenService MakeTokenService(IConfiguration config) => new(config);

    [Fact]
    public void BuildSuperAdminAccessToken_NoOrgClaim_RoundTripsWithConfiguredKey()
    {
        var config = BuildConfig();
        var (token, expiresAt) = MakeTokenService(config).IssueAccessToken(
            Guid.NewGuid(), "sa@example.com", "Platform Operator",
            new ActiveOrganization(null, "super_admin", "org.members.manage"));

        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        // Spec 30: assert the literal v2 claim names — the live JwtBearer pipeline
        // maps name/role/sub/email to ClaimTypes.* (the spec-29 middleware reads
        // both forms), so the test turns mapping off for exact-name lookups.
        handler.MapInboundClaims = false;
        var principal = handler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["JWT:Issuer"],
            ValidAudience = config["JWT:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(config["JWT:Key"]!))
        }, out _);

        Assert.Null(principal.FindFirst("org"));           // platform-only: claim omitted
        Assert.Equal("super_admin", principal.FindFirst("role")!.Value);
        Assert.Equal("Platform Operator", principal.FindFirst("name")!.Value);
        Assert.True(principal.FindFirst("perms")!.Value.Split(' ').Length >= 1);
    }

    /// <summary>Builds a raw opaque token (64-char hex of 32 random bytes) the same way AuthService does.</summary>
    private static string MakeRawToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>SHA-256 hash of a raw token hex string (same helper as in AuthService).</summary>
    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Convert.FromHexString(rawToken)));

    private static User MakeUser() => new()
    {
        Id          = Guid.NewGuid(),
        Email       = "test@example.com",
        DisplayName = "Test User",
        PasswordHash = "deadbeef:cafebabe" // intentionally invalid — not tested here
    };

    // ──────────────────────────────────────────────────────────────────────────
    // Refresh – Replay attack: revoke entire family
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spec 07 acceptance criterion: replay of a rotated refresh token must revoke the whole family
    /// and return null (401 at the controller level).
    /// </summary>
    [Fact]
    public async Task Refresh_Cannot_Be_Replayed_RevokesFamily_ReturnsNull()
    {
        // Arrange
        var rawToken = MakeRawToken();
        var tokenHash = HashToken(rawToken);
        var userId = Guid.NewGuid();

        // Simulate a token that was already rotated (RevokedAt is set = reuse attack).
        var revokedToken = new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = DateTime.UtcNow.AddHours(-1), // already rotated
            User      = MakeUser()
        };

        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.FindRefreshTokenByTokenHashAsync(tokenHash))
                .ReturnsAsync(revokedToken);

        // Family revoke must be called exactly once.
        repoMock.Setup(r => r.RevokeFamilyAsync(userId, revokedToken.FamilyId, It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

        var sut = BuildService(repoMock);

        // Act
        var result = await sut.RefreshAsync(rawToken, requestedOrganizationId: null);

        // Assert
        Assert.Null(result);
        repoMock.Verify(r => r.RevokeFamilyAsync(userId, revokedToken.FamilyId, It.IsAny<DateTime>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Refresh – Valid rotation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewPair()
    {
        // Arrange
        var rawToken  = MakeRawToken();
        var tokenHash = HashToken(rawToken);
        var user      = MakeUser();

        var activeToken = new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null, // not yet revoked
            User      = user
        };

        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.FindRefreshTokenByTokenHashAsync(tokenHash))
                .ReturnsAsync(activeToken);
        repoMock.Setup(r => r.RotateRefreshTokenAsync(activeToken, It.IsAny<RefreshToken>()))
                .ReturnsAsync((RefreshToken old, RefreshToken replacement) => replacement);

        var sut = BuildService(repoMock);

        // Act
        var result = await sut.RefreshAsync(rawToken, requestedOrganizationId: null);

        // Assert — new pair returned, rotation called once
        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotEqual(rawToken, result.RefreshToken); // new token != old token
        repoMock.Verify(r => r.RotateRefreshTokenAsync(activeToken,
            It.Is<RefreshToken>(t => t.FamilyId == activeToken.FamilyId && t.UserId == user.Id)), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Refresh – Expired token
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var rawToken  = MakeRawToken();
        var tokenHash = HashToken(rawToken);
        var user      = MakeUser();

        var expiredToken = new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // in the past
            CreatedAt = DateTime.UtcNow.AddDays(-31),
            RevokedAt = null,
            User      = user
        };

        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.FindRefreshTokenByTokenHashAsync(tokenHash))
                .ReturnsAsync(expiredToken);

        var sut = BuildService(repoMock);

        // Act
        var result = await sut.RefreshAsync(rawToken, requestedOrganizationId: null);

        // Assert
        Assert.Null(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Refresh – Unknown token
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsNull()
    {
        // Arrange
        var rawToken  = MakeRawToken();
        var tokenHash = HashToken(rawToken);

        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.FindRefreshTokenByTokenHashAsync(tokenHash))
                .ReturnsAsync((RefreshToken?)null);

        var sut = BuildService(repoMock);

        // Act
        var result = await sut.RefreshAsync(rawToken, requestedOrganizationId: null);

        // Assert
        Assert.Null(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Register – Duplicate email throws DuplicateEmailException
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        // Arrange
        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.UserExistsByEmailAsync("existing@example.com"))
                .ReturnsAsync(true);

        var sut = BuildService(repoMock);

        // Act + Assert
        await Assert.ThrowsAsync<DuplicateEmailException>(() =>
            sut.RegisterAsync(new RegisterRequest
            {
                Email       = "existing@example.com",
                DisplayName = "Dup User",
                Password    = "Password123!"
            }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Register – Happy path
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewUser_ReturnsTokenPair()
    {
        // Arrange
        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.UserExistsByEmailAsync("new@example.com"))
                .ReturnsAsync(false);
        repoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);
        repoMock.Setup(r => r.InsertRefreshTokenAsync(It.IsAny<RefreshToken>()))
                .ReturnsAsync((RefreshToken t) => t);
        // OTP: registration persists the email-verify challenge (best-effort email send is not asserted here).
        repoMock.Setup(r => r.InvalidateOtpChallengesAsync(It.IsAny<Guid>(), "email_verify"))
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.InsertOtpChallengeAsync(It.IsAny<OtpChallenge>()))
                .Returns(Task.CompletedTask);

        var sut = BuildService(repoMock);

        // Act
        var result = await sut.RegisterAsync(new RegisterRequest
        {
            Email       = "new@example.com",
            DisplayName = "New User",
            Password    = "Password123!"
        });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("new@example.com", result.User.Email);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Login – Wrong password returns null
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        // Arrange — user exists but stored hash won't match a wrong password
        var user = new User
        {
            Id           = Guid.NewGuid(),
            Email        = "user@example.com",
            DisplayName  = "User",
            // Stored as hexSalt:hexHash; arbitrary bytes that won't match "WrongPassword"
            PasswordHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
                           + ":" 
                           + Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
        };

        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.FindUserByEmailAsync("user@example.com"))
                .ReturnsAsync(user);

        var sut = BuildService(repoMock);

        // Act
        var result = await sut.LoginAsync(new LoginRequest
        {
            Email    = "user@example.com",
            Password = "WrongPassword!"
        });

        // Assert
        Assert.Null(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Logout – Idempotent (unknown token silently succeeds)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_UnknownToken_SilentlySucceeds()
    {
        // Arrange
        var rawToken  = MakeRawToken();
        var tokenHash = HashToken(rawToken);

        var repoMock = new Mock<IAuthRepository>(MockBehavior.Strict);
        repoMock.Setup(r => r.FindRefreshTokenByTokenHashAsync(tokenHash))
                .ReturnsAsync((RefreshToken?)null);

        var sut = BuildService(repoMock);

        // Act + Assert — should not throw
        await sut.LogoutAsync(rawToken);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("ABC")]
    [InlineData("000000000000000000000000000000000000000000000000000000000000000G")]
    [InlineData("000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("00000000000000000000000000000000000000000000000000000000000000000")]
    public async Task MalformedTokens_RefreshRejects_LogoutSucceeds_WithoutDatabaseAccess(string? token)
    {
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        var service = BuildService(repo);
        Assert.Null(await service.RefreshAsync(token!, requestedOrganizationId: null));
        await service.LogoutAsync(token!);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Refresh_LostRotationRace_RevokesOnlyStoredFamily_AndIssuesNoPair()
    {
        var raw = MakeRawToken();
        // storedToken.User is eager-loaded in production (FindRefreshTokenByTokenHashAsync
        // includes User) and the reordered session resolution reads it BEFORE rotation —
        // the fixture must mirror that shape.
        var token = new RefreshToken { UserId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(1), User = MakeUser() };
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(raw))).ReturnsAsync(token);
        // CodeRabbit fix ordering: session (incl. auto-pick) resolves BEFORE rotation.
        repo.Setup(r => r.GetActiveOrganizationMembershipsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new System.Collections.Generic.List<OrganizationMember>());
        repo.Setup(r => r.RotateRefreshTokenAsync(token, It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken?)null);
        repo.Setup(r => r.RevokeFamilyAsync(token.UserId, token.FamilyId, It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        Assert.Null(await BuildService(repo).RefreshAsync(raw, requestedOrganizationId: null));
        repo.Verify(r => r.RevokeFamilyAsync(token.UserId, token.FamilyId, It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("WrongPassword!")]
    [InlineData("Griot-dummy-login-record")]
    public async Task Login_UnknownUser_RejectsEvenWhenDummyPasswordMatches(string password)
    {
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindUserByEmailAsync("missing@example.com")).ReturnsAsync((User?)null);
        Assert.Null(await BuildService(repo).LoginAsync(new LoginRequest
        {
            Email = "missing@example.com", Password = password
        }));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Spec 30 – organization session (list + select) + SuperAdmin bootstrap + key policy
    // ──────────────────────────────────────────────────────────────────────────

    private static OrganizationMember MakeMember(Guid orgId, Guid userId, Griot.Domain.Enums.OrganizationRole role, string orgName = "Acme")
    {
        return new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Organization = new Organization { Id = orgId, Name = orgName, Slug = "acme", OwnerId = userId },
            UserId = userId,
            User = MakeUser(),
            Role = role,
            Status = Griot.Domain.Enums.OrganizationMemberStatus.Active,
            JoinedAt = DateTime.UtcNow.AddDays(-10)
        };
    }

    [Fact]
    public async Task ListOrganizations_ReturnsOnlyActiveMemberships_WithActiveFlag()
    {
        var caller = Guid.NewGuid();
        var activeOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var member = MakeMember(activeOrg, caller, Griot.Domain.Enums.OrganizationRole.Admin);
        var other = MakeMember(otherOrg, caller, Griot.Domain.Enums.OrganizationRole.Member, "Other");
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        // Loose default returns empty; test-specific setup wins (last setup).
        repo.Setup(r => r.GetActiveOrganizationMembershipsAsync(caller))
            .ReturnsAsync(new System.Collections.Generic.List<OrganizationMember> { member, other });
        var result = await BuildService(repo, applySpec30Defaults: false).ListOrganizationsAsync(caller, isSuperAdmin: false, activeOrganizationId: activeOrg);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.OrganizationId == activeOrg && m.IsActive && m.Role == "admin");
        Assert.Contains(result, m => m.OrganizationId == otherOrg && !m.IsActive);
    }

    [Fact]
    public async Task SelectOrganization_MissingRefreshToken_ThrowsValidation()
    {
        var caller = MakeUser();
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            BuildService(repo, applySpec30Defaults: false).SelectOrganizationAsync(
                caller.Id, new SelectOrganizationRequest { OrganizationId = Guid.NewGuid() }, refreshToken: null));
        Assert.Equal(DomainErrorKind.Validation, error.Kind); // CodeRabbit CWE-613 fix
    }

    [Fact]
    public async Task SelectOrganization_UnknownRefreshToken_ReturnsNull()
    {
        var caller = MakeUser();
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);
        Assert.Null(await BuildService(repo, applySpec30Defaults: false).SelectOrganizationAsync(
            caller.Id, new SelectOrganizationRequest { OrganizationId = Guid.NewGuid() },
            refreshToken: MakeRawToken()));
    }

    [Fact]
    public async Task SelectOrganization_ForeignRefreshToken_ReturnsNull()
    {
        var caller = MakeUser();
        var otherUser = Guid.NewGuid();
        var presentedRaw = MakeRawToken();
        var presented = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = otherUser, // belongs to ANOTHER caller → mismatch → 401
            FamilyId = Guid.NewGuid(),
            TokenHash = HashToken(presentedRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(presentedRaw))).ReturnsAsync(presented);
        Assert.Null(await BuildService(repo, applySpec30Defaults: false).SelectOrganizationAsync(
            caller.Id, new SelectOrganizationRequest { OrganizationId = Guid.NewGuid() }, presentedRaw));
    }

    [Fact]
    public async Task SelectOrganization_RevokedRefreshToken_ReturnsNull()
    {
        var caller = MakeUser();
        var presentedRaw = MakeRawToken();
        var presented = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = caller.Id,
            FamilyId = Guid.NewGuid(),
            TokenHash = HashToken(presentedRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = DateTime.UtcNow.AddHours(-1) // already used/revoked
        };
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(presentedRaw))).ReturnsAsync(presented);
        Assert.Null(await BuildService(repo, applySpec30Defaults: false).SelectOrganizationAsync(
            caller.Id, new SelectOrganizationRequest { OrganizationId = Guid.NewGuid() }, presentedRaw));
    }

    [Fact]
    public async Task SelectOrganization_NonMember_ThrowsForbidden()
    {
        var caller = MakeUser();
        var orgId = Guid.NewGuid();
        var presentedRaw = MakeRawToken();
        var presented = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = caller.Id,
            FamilyId = Guid.NewGuid(),
            TokenHash = HashToken(presentedRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindOrganizationByIdAsync(orgId))
            .ReturnsAsync(new Organization { Id = orgId, Name = "Acme", Slug = "acme", OwnerId = Guid.NewGuid() });
        repo.Setup(r => r.FindUserByIdAsync(caller.Id)).ReturnsAsync(caller);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(presentedRaw))).ReturnsAsync(presented);
        repo.Setup(r => r.FindActiveOrganizationMemberAsync(orgId, caller.Id)).ReturnsAsync((OrganizationMember?)null);
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            BuildService(repo, applySpec30Defaults: false).SelectOrganizationAsync(
                caller.Id, new SelectOrganizationRequest { OrganizationId = orgId, RefreshToken = presentedRaw }, presentedRaw));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task SelectOrganization_UnknownOrg_ThrowsNotFound()
    {
        var caller = MakeUser();
        var presentedRaw = MakeRawToken();
        var presented = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = caller.Id,
            FamilyId = Guid.NewGuid(),
            TokenHash = HashToken(presentedRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindOrganizationByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Organization?)null);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(presentedRaw))).ReturnsAsync(presented);
        await Assert.ThrowsAsync<DomainError>(() =>
            BuildService(repo, applySpec30Defaults: false).SelectOrganizationAsync(
                caller.Id, new SelectOrganizationRequest { OrganizationId = Guid.NewGuid(), RefreshToken = presentedRaw }, presentedRaw));
    }

    [Fact]
    public async Task SelectOrganization_Member_ReissuesPairAndRevokesPresentedFamily()
    {
        var caller = MakeUser();
        var orgId = Guid.NewGuid();
        var member = MakeMember(orgId, caller.Id, Griot.Domain.Enums.OrganizationRole.Owner);
        var presentedRaw = MakeRawToken();
        var presented = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = caller.Id,
            FamilyId = Guid.NewGuid(),
            TokenHash = HashToken(presentedRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            User = caller
        };
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindOrganizationByIdAsync(orgId)).ReturnsAsync(member.Organization);
        repo.Setup(r => r.FindUserByIdAsync(caller.Id)).ReturnsAsync(caller);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(presentedRaw))).ReturnsAsync(presented);
        repo.Setup(r => r.FindActiveOrganizationMemberAsync(orgId, caller.Id)).ReturnsAsync(member);
        // Atomic swap (CodeRabbit fix): revoke family + insert replacement in ONE op.
        repo.Setup(r => r.SwapRefreshFamilyAsync(It.IsAny<RefreshToken>(), presented.FamilyId, It.IsAny<DateTime>()))
            .ReturnsAsync((RefreshToken t, Guid _, DateTime _) => t);
        var response = await BuildService(repo, applySpec30Defaults: false).SelectOrganizationAsync(
            caller.Id, new SelectOrganizationRequest { OrganizationId = orgId, RefreshToken = presentedRaw }, presentedRaw);
        Assert.NotNull(response);
        repo.Verify(r => r.SwapRefreshFamilyAsync(It.IsAny<RefreshToken>(), presented.FamilyId, It.IsAny<DateTime>()), Times.Once);
        repo.Verify(r => r.InsertRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureSuperAdmin_NoConfig_NoOps()
    {
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var sut = new AuthService(repo.Object, config, Mock.Of<ILogger<AuthService>>(), BuildEmailService(), BuildAuditService(out _), new TokenService(config));
        var result = await sut.EnsureSuperAdminAsync();
        Assert.False(result.Created || result.Upgraded);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureSuperAdmin_ExistingUser_UpgradesIdempotently()
    {
        var user = MakeUser();
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindUserByEmailAsync("boss@griot.test")).ReturnsAsync(user);
        repo.Setup(r => r.SetPlatformRoleAsync(user.Id, Griot.Domain.Enums.PlatformRole.SuperAdmin)).Returns(Task.CompletedTask);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SUPERADMIN:Email"] = "boss@griot.test"
        }).Build();
        var sut = new AuthService(repo.Object, config, Mock.Of<ILogger<AuthService>>(), BuildEmailService(), BuildAuditService(out _), new TokenService(config));
        var result = await sut.EnsureSuperAdminAsync();
        Assert.True(result.Upgraded && !result.Created);
    }

    [Theory]
    [InlineData(false, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]   // 32-byte dev floor passes
    [InlineData(false, "short", false)]                            // below HS256 floor
    [InlineData(true, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)] // 64-byte prod floor passes
    [InlineData(true, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]  // 32 bytes < prod floor
    public void ValidateKeyPolicy_EnforcesDevAndProductionFloors(bool production, string key, bool valid)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT:Key"] = key
        }).Build();
        var svc = new TokenService(config);
        if (valid) svc.ValidateKeyPolicy(production);
        else Assert.Throws<InvalidOperationException>(() => svc.ValidateKeyPolicy(production));
    }

    [Fact]
    public void RoleSelection_SuperAdminWins_AndCustomRoleFormats()
    {
        var customId = Guid.NewGuid();
        var custom = MakeMember(Guid.NewGuid(), Guid.NewGuid(), Griot.Domain.Enums.OrganizationRole.Custom);
        custom.CustomRoleId = customId;
        custom.CustomRole = new Role { Id = customId, Name = "Auditor", Permissions = "org.read, report.generate" };
        Assert.Equal("super_admin", RoleSelection.EffectiveRole(true, custom));
        Assert.Equal($"custom:{customId}", RoleSelection.EffectiveRole(false, custom));
        Assert.Equal("org.read report.generate", RoleSelection.EffectivePerms(custom));
        Assert.Equal(string.Empty, RoleSelection.EffectivePerms(null));
        Assert.DoesNotContain(PermissionCatalogue.LogReadTier, PermissionCatalogue.PermsForSystemRole(Griot.Domain.Enums.OrganizationRole.Owner));
    }
}
