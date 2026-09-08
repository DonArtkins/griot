using System.Security.Cryptography;
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

    private static AuthService BuildService(Mock<IAuthRepository> repoMock) =>
        new(repoMock.Object, BuildConfig(), Mock.Of<ILogger<AuthService>>(), BuildEmailService());

    private static AuthService BuildService(Mock<IAuthRepository> repoMock, IEmailService emailService) =>
        new(repoMock.Object, BuildConfig(), Mock.Of<ILogger<AuthService>>(), emailService);

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
        var result = await sut.RefreshAsync(rawToken);

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
        var result = await sut.RefreshAsync(rawToken);

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
        var result = await sut.RefreshAsync(rawToken);

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
        var result = await sut.RefreshAsync(rawToken);

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
        // OTP: registration persists the email-verify challenge (best-effort Resend send is not asserted here).
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
        Assert.Null(await service.RefreshAsync(token!));
        await service.LogoutAsync(token!);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Refresh_LostRotationRace_RevokesOnlyStoredFamily_AndIssuesNoPair()
    {
        var raw = MakeRawToken();
        var token = new RefreshToken { UserId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var repo = new Mock<IAuthRepository>(MockBehavior.Strict);
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(raw))).ReturnsAsync(token);
        repo.Setup(r => r.RotateRefreshTokenAsync(token, It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken?)null);
        repo.Setup(r => r.RevokeFamilyAsync(token.UserId, token.FamilyId, It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        Assert.Null(await BuildService(repo).RefreshAsync(raw));
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

}
