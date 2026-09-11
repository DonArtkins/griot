// ═══════════════════════════════════════════════════════════════════════════
// Observability — auth audit events (spec 20, pipeline §6)
//
// AuthService is not part of any shared tracked transaction (its writes go
// through IAuthRepository), so each auth event commits its OWN durable
// AuditLogs row via IAuditService.RecordAsync and is best-effort. These tests
// prove: the action vocabulary, the attributable-failure rule (known account
// only), the no-secrets rule, and failure isolation (audit outage never breaks
// the auth outcome).
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Griot.Application.DTOs.Auth;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Griot.Tests.Domain;

public class ObservabilityAuthAuditTests
{
    // ────────────────────────── fixtures ──────────────────────────

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Convert.FromHexString(rawToken)));

    /// <summary>64-char hex opaque token, exactly as AuthService.GenerateOpaqueToken produces.</summary>
    private static string MakeRawToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Not a valid Argon2 record: VerifyPassword always fails against it.
    /// Used for attributable-failure and never for success paths.
    /// </summary>
    private const string DummyPasswordHash = "not-a-valid-password-record";

    // Argon2id tuning — MUST mirror AuthService's private constants (t=3, m=64MB, p=4, 32 bytes).
    private const int Argon2Iterations = 3;
    private const int Argon2MemoryKb = 65536;
    private const int Argon2Lanes = 4;
    private const int Argon2HashLength = 32;

    /// <summary>
    /// Builds a real, verifiable Argon2id "hexSalt:hexHash" record (same parameters as
    /// AuthService.HashPassword) so success-path tests exercise the actual verify cost.
    /// </summary>
    private static string HashPasswordForTest(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            Iterations = Argon2Iterations,
            MemorySize = Argon2MemoryKb,
            DegreeOfParallelism = Argon2Lanes
        };
        var hash = argon2.GetBytes(Argon2HashLength);
        return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
    }

    private static User MakeUser() => new()
    {
        Email = $"auth-audit-{Guid.NewGuid():N}@griot.test",
        DisplayName = "Auth Audit User",
        PasswordHash = DummyPasswordHash
    };

    /// <summary>User whose stored record verifies for <paramref name="password"/>.</summary>
    private static User MakeUserWithPassword(string password) => new()
    {
        Email = $"auth-audit-{Guid.NewGuid():N}@griot.test",
        DisplayName = "Auth Audit User",
        PasswordHash = HashPasswordForTest(password)
    };

    private static IConfiguration BuildConfig()
    {
        var values = new Dictionary<string, string?>
        {
            ["JWT:Key"] = "spec-20-observability-test-key-0123456789abcdef0123456789abcdef",
            ["JWT:Issuer"] = "Griot",
            ["JWT:Audience"] = "GriotClient",
            ["JWT:ExpiresMinutes"] = "30"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static Mock<IAuthRepository> MakeRepo()
    {
        var repo = new Mock<IAuthRepository>();
        // Loose token persistence for the success paths (login/rotation pairs).
        repo.Setup(r => r.InsertRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken t) => t);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        // Spec 30: no seeded organization memberships in the observability fixtures —
        // sessions resolve to platform-only (no `org` claim), exactly like pre-spec-30.
        repo.Setup(r => r.GetActiveOrganizationMembershipsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((System.Collections.Generic.IReadOnlyList<OrganizationMember>)new System.Collections.Generic.List<OrganizationMember>());
        repo.Setup(r => r.FindActiveOrganizationMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((OrganizationMember?)null);
        return repo;
    }

    /// <summary>
    /// Records every AuditEntry the service hands to IAuditService so tests can
    /// verify the mock AND inspect the stored rows (no-secrets rule).
    /// </summary>
    private sealed class AuditRecorder
    {
        public List<AuditEntry> Rows { get; } = new();
        public Mock<IAuditService> Mock { get; }

        public AuditRecorder(bool throwing = false)
        {
            Mock = new Mock<IAuditService>();
            if (throwing)
            {
                Mock.Setup(a => a.RecordAsync(It.IsAny<AuditEntry>()))
                    .ThrowsAsync(new InvalidOperationException("SQL Server unavailable"));
            }
            else
            {
                Mock.Setup(a => a.RecordAsync(It.IsAny<AuditEntry>()))
                    .Callback<AuditEntry>(Rows.Add)
                    .Returns(Task.CompletedTask);
            }
        }
    }

    private static AuthService BuildService(Mock<IAuthRepository> repo, out AuditRecorder audit)
    {
        audit = new AuditRecorder();
        return new AuthService(
            repo.Object,
            BuildConfig(),
            Mock.Of<ILogger<AuthService>>(),
            Mock.Of<IEmailService>(),
            audit.Mock.Object,
            new TokenService(BuildConfig()));
    }

    // ────────────────────────── login ──────────────────────────

    [Fact]
    public async Task Login_Success_WritesOneDurableAuthLoginRow()
    {
        const string password = "whatever-password";
        var user = MakeUserWithPassword(password);
        var repo = MakeRepo();
        repo.Setup(r => r.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

        var sut = BuildService(repo, out var audit);

        var response = await sut.LoginAsync(new LoginRequest { Email = user.Email, Password = password });

        Assert.NotNull(response);
        audit.Mock.Verify(a => a.RecordAsync(It.Is<AuditEntry>(e =>
            e.Action == "Auth.Login"
            && e.EntityType == "User"
            && e.EntityId == user.Id
            && e.ActorId == user.Id
            && e.After != null
            && e.After.Contains("success"))), Times.Once);
        audit.Mock.Verify(a => a.RecordAsync(It.IsAny<AuditEntry>()), Times.Once);
    }

    [Fact]
    public async Task Login_AttributableFailure_KnownUserWritesFailedRow()
    {
        var user = MakeUser(); // invalid hash record -> password verify fails
        var repo = MakeRepo();
        repo.Setup(r => r.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

        var sut = BuildService(repo, out var audit);

        var response = await sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "wrong-password" });

        Assert.Null(response);
        audit.Mock.Verify(a => a.RecordAsync(It.Is<AuditEntry>(e =>
            e.Action == "Auth.Login"
            && e.EntityId == user.Id
            && e.After != null
            && e.After.Contains("failed"))), Times.Once);
    }

    [Fact]
    public async Task Login_UnknownEmail_IsNotAudited()
    {
        var repo = MakeRepo();
        repo.Setup(r => r.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var sut = BuildService(repo, out var audit);

        var response = await sut.LoginAsync(new LoginRequest { Email = "nobody@griot.test", Password = "wrong-password" });

        Assert.Null(response);
        audit.Mock.Verify(a => a.RecordAsync(It.IsAny<AuditEntry>()), Times.Never);
    }

    // ────────────────────────── refresh ──────────────────────────

    [Fact]
    public async Task Refresh_ReuseAttack_WritesReplayRevokedRow()
    {
        var user = MakeUser();
        var rawToken = MakeRawToken();
        var revokedToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = DateTime.UtcNow.AddHours(-1), // already rotated -> reuse
            User = user
        };

        var repo = MakeRepo();
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(rawToken))).ReturnsAsync(revokedToken);
        repo.Setup(r => r.RevokeFamilyAsync(user.Id, revokedToken.FamilyId, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var sut = BuildService(repo, out var audit);

        var response = await sut.RefreshAsync(rawToken, requestedOrganizationId: null);

        Assert.Null(response);
        repo.Verify(r => r.RevokeFamilyAsync(user.Id, revokedToken.FamilyId, It.IsAny<DateTime>()), Times.Once);
        audit.Mock.Verify(a => a.RecordAsync(It.Is<AuditEntry>(e =>
            e.Action == "Auth.RefreshReplayRevoked"
            && e.ActorId == user.Id
            && e.After != null
            && e.After.Contains("reuse-revoked"))), Times.Once);
    }

    [Fact]
    public async Task Refresh_ValidRotation_WritesRefreshRowWithoutSecrets()
    {
        var user = MakeUser();
        var rawToken = MakeRawToken();
        var activeToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null,
            User = user
        };

        var repo = MakeRepo();
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(rawToken))).ReturnsAsync(activeToken);
        repo.Setup(r => r.RotateRefreshTokenAsync(activeToken, It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken old, RefreshToken replacement) => replacement);

        var sut = BuildService(repo, out var audit);

        var response = await sut.RefreshAsync(rawToken, requestedOrganizationId: null);

        Assert.NotNull(response);
        Assert.Contains(audit.Rows, e => e.Action == "Auth.Refresh" && e.ActorId == user.Id);

        // No-secrets rule (spec 20): outcomes only — never the raw token or its hash.
        Assert.NotEmpty(audit.Rows);
        Assert.All(audit.Rows, e =>
        {
            Assert.DoesNotContain(rawToken, e.After);
            Assert.DoesNotContain(activeToken.TokenHash, e.After);
            Assert.DoesNotContain("rt-", e.After);
        });
    }

    // ────────────────────────── logout ──────────────────────────

    [Fact]
    public async Task Logout_Success_WritesOneDurableLogoutRow()
    {
        var user = MakeUser();
        var rawToken = MakeRawToken();
        var activeToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null,
            User = user
        };

        var repo = MakeRepo();
        repo.Setup(r => r.FindRefreshTokenByTokenHashAsync(HashToken(rawToken))).ReturnsAsync(activeToken);

        var sut = BuildService(repo, out var audit);

        await sut.LogoutAsync(rawToken);

        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        audit.Mock.Verify(a => a.RecordAsync(It.Is<AuditEntry>(e =>
            e.Action == "Auth.Logout"
            && e.EntityType == "User"
            && e.EntityId == user.Id
            && e.After != null
            && e.After.Contains("revoked"))), Times.Once);
    }

    // ────────────────────────── failure isolation ──────────────────────────

    [Fact]
    public async Task Login_Success_AuditStoreUnavailable_OutcomeStillSucceeds()
    {
        var user = MakeUserWithPassword("whatever-password");
        var repo = MakeRepo();
        repo.Setup(r => r.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

        // Best-effort contract: the durable audit writer throwing must never turn
        // the auth outcome into a 500 — the gap is logged, the login proceeds.
        var throwing = new AuditRecorder(throwing: true);
        var sut = new AuthService(
            repo.Object,
            BuildConfig(),
            Mock.Of<ILogger<AuthService>>(),
            Mock.Of<IEmailService>(),
            throwing.Mock.Object,
            new TokenService(BuildConfig()));

        var response = await sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "whatever-password" });

        Assert.NotNull(response);
        throwing.Mock.Verify(a => a.RecordAsync(It.IsAny<AuditEntry>()), Times.Once);
    }
}