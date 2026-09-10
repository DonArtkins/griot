using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Griot.Application.DTOs.Auth;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Griot.Infrastructure.Repositories;
using Griot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Griot.Tests.Auth;

public sealed class AuthSqlTests : IClassFixture<SqlAuthFixture>
{
    private readonly SqlAuthFixture _fixture;
    public AuthSqlTests(SqlAuthFixture fixture) => _fixture = fixture;

    private static User NewUser() => new()
    {
        Email = $"auth-test-{Guid.NewGuid():N}@example.com", DisplayName = "Auth test", PasswordHash = "test"
    };

    private static RefreshToken NewToken(User user, Guid? familyId = null) => new()
    {
        UserId = user.Id, FamilyId = familyId ?? Guid.NewGuid(),
        TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = DateTime.UtcNow.AddDays(30)
    };

    [SqlAuthFact]
    public async Task Migration_PreservesExistingChain_AndIndependentActiveSession()
    {
        await using var context = _fixture.CreateContext();
        var root = await context.RefreshTokens.SingleAsync(t => t.Id == _fixture.LegacyRootId);
        var replacement = await context.RefreshTokens.SingleAsync(t => t.Id == _fixture.LegacyReplacementId);
        var independent = await context.RefreshTokens.SingleAsync(t => t.Id == _fixture.LegacyIndependentId);
        var legacyUser = await context.Users.SingleAsync(user => user.Id == root.UserId);
        Assert.False(legacyUser.EmailVerified);
        Assert.Equal(Griot.Domain.Enums.TwoFactorMethod.None, legacyUser.TwoFactorMethod);
        Assert.Equal(root.Id, root.FamilyId);
        Assert.Equal(root.FamilyId, replacement.FamilyId);
        Assert.Equal(independent.Id, independent.FamilyId);
        Assert.NotEqual(root.FamilyId, independent.FamilyId);
        Assert.Null(replacement.RevokedAt);
        Assert.Null(independent.RevokedAt);
    }

    [SqlAuthFact]
    public async Task RegistrationRace_OnlyEmailUniqueViolationBecomesDuplicateEmail()
    {
        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        var user = NewUser();
        var competing = NewUser();
        competing.Email = user.Email;
        var repoA = new AuthRepository(first);
        var repoB = new AuthRepository(second);
        Assert.False(await repoA.UserExistsByEmailAsync(user.Email));
        Assert.False(await repoB.UserExistsByEmailAsync(user.Email));
        await repoA.CreateUserAsync(user);
        await Assert.ThrowsAsync<DuplicateEmailException>(() => repoB.CreateUserAsync(competing));
        Assert.Equal(1, await first.Users.CountAsync(u => u.Email == user.Email));

        // A different unique constraint must not be mislabeled as a duplicate email.
        var duplicateId = NewUser();
        duplicateId.Id = user.Id;
        await using var third = _fixture.CreateContext();
        await Assert.ThrowsAsync<DbUpdateException>(() => new AuthRepository(third).CreateUserAsync(duplicateId));
    }

    [SqlAuthFact]
    public async Task Rotation_ConcurrentStaleReads_OnlyOneReplacementCommits()
    {
        var user = NewUser();
        var original = NewToken(user);
        await using (var setup = _fixture.CreateContext())
        {
            setup.Users.Add(user);
            setup.RefreshTokens.Add(original);
            await setup.SaveChangesAsync();
        }
        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        var repoA = new AuthRepository(first);
        var repoB = new AuthRepository(second);
        var readA = await repoA.FindRefreshTokenByTokenHashAsync(original.TokenHash);
        var readB = await repoB.FindRefreshTokenByTokenHashAsync(original.TokenHash);
        Assert.Null(readA!.RevokedAt);
        Assert.Null(readB!.RevokedAt);
        var results = await Task.WhenAll(
            repoA.RotateRefreshTokenAsync(readA, NewToken(user, original.FamilyId)),
            repoB.RotateRefreshTokenAsync(readB, NewToken(user, original.FamilyId)));
        var winner = Assert.Single(results.Where(r => r is not null));
        await using var verify = _fixture.CreateContext();
        var rows = await verify.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(winner!.Id, rows.Single(t => t.Id == original.Id).ReplacedByTokenId);
    }

    [SqlAuthFact]
    public async Task Rotation_FailedReplacementInsert_RollsBackOriginalRevocation()
    {
        var user = NewUser();
        var original = NewToken(user);
        await using (var setup = _fixture.CreateContext())
        {
            setup.Users.Add(user);
            setup.RefreshTokens.Add(original);
            await setup.SaveChangesAsync();
        }
        await using (var context = _fixture.CreateContext())
        {
            var replacement = NewToken(user, original.FamilyId);
            replacement.TokenHash = original.TokenHash;
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                new AuthRepository(context).RotateRefreshTokenAsync(original, replacement));
        }
        await using var verify = _fixture.CreateContext();
        var stored = await verify.RefreshTokens.SingleAsync(t => t.Id == original.Id);
        Assert.Null(stored.RevokedAt);
        Assert.Null(stored.ReplacedByTokenId);
        Assert.Equal(1, await verify.RefreshTokens.CountAsync(t => t.UserId == user.Id));
    }

    private WebApplicationFactory<Program> CreateApi() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseContentRoot(_fixture.ApiDirectory)
            .UseEnvironment("Development")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _fixture.ConnectionString,
                ["JWT:Key"] = "integration-test-key-at-least-256-bits-long-padding",
                ["Redis:Connection"] = Environment.GetEnvironmentVariable("Redis__Connection") ?? "localhost:6380",
            ["Otp:Pepper"] = "test-otp-pepper"
            }))
            .ConfigureTestServices(services =>
            {
                // Final DI overrides prevent local runtime config from selecting the development DB.
                services.RemoveAll<GriotDbContext>();
                services.RemoveAll<IDbContextFactory<GriotDbContext>>();
                services.AddScoped(_ => _fixture.CreateContext());
                services.AddSingleton<IDbContextFactory<GriotDbContext>>(new TestContextFactory(_fixture));
            }));

    private sealed class TestContextFactory(SqlAuthFixture fixture) : IDbContextFactory<GriotDbContext>
    {
        public GriotDbContext CreateDbContext() => fixture.CreateContext();
    }

    private void VerifyTestDatabase(WebApplicationFactory<Program> app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GriotDbContext>();
        Assert.Equal(_fixture.ConnectionString, context.Database.GetConnectionString());
        Assert.StartsWith("GriotAuthTests_", context.Database.GetDbConnection().Database);
    }

    [SqlAuthFact]
    public async Task HttpAuth_PersistsUser_RejectsReplay_KeepsIndependentSession_AndLogsOut()
    {
        await using var app = CreateApi();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        VerifyTestDatabase(app);
        var request = new RegisterRequest { Email = NewUser().Email, DisplayName = "HTTP Auth", Password = "TestPassword123!" };
        var registration = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.True(registration.StatusCode == HttpStatusCode.Created, await registration.Content.ReadAsStringAsync());
        var first = (await registration.Content.ReadFromJsonAsync<AuthResponse>())!;
        await using (var context = _fixture.CreateContext())
        {
            var user = await context.Users.SingleAsync(u => u.Id == first.User.Id);
            Assert.Equal(request.Email, user.Email);
            Assert.NotEqual(request.Password, user.PasswordHash);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", request)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { request.Email, request.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var independent = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        var rotation = await client.PostAsJsonAsync("/api/auth/refresh", new { first.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, rotation.StatusCode);
        var replacement = (await rotation.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { first.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { replacement.RefreshToken })).StatusCode);
        var independentRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new { independent.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, independentRefresh.StatusCode);
        var active = (await independentRefresh.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "invalid" })).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", active.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", new { active.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", new { active.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { active.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { request.Email, password = "wrong-password" })).StatusCode);
    }

    [SqlAuthFact]
    public async Task HttpRefresh_TwoConcurrentRequests_ExactlyOneSucceeds()
    {
        await using var app = CreateApi();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        VerifyTestDatabase(app);
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = NewUser().Email, DisplayName = "Concurrent Auth", Password = "TestPassword123!"
        });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var pair = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        var results = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/refresh", new { pair.RefreshToken }),
            client.PostAsJsonAsync("/api/auth/refresh", new { pair.RefreshToken }));
        Assert.Single(results.Where(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Single(results.Where(r => r.StatusCode == HttpStatusCode.Unauthorized));
    }

    [SqlAuthFact]
    public async Task HttpOtp_VerifyConsumes_AndMarksEmailVerified_AndLocksAfterFive()
    {
        await using var app = CreateApi();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        VerifyTestDatabase(app);

        var user = NewUser();
        var code = "123456";
        var pepper = "test-otp-pepper";
        var challenge = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), Encoding.UTF8.GetBytes(code))),
            Purpose = "email_verify",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Consumed = false,
            CreatedAt = DateTime.UtcNow
        };
        await using (var setup = _fixture.CreateContext())
        {
            setup.Users.Add(user);
            setup.OtpChallenges.Add(challenge);
            await setup.SaveChangesAsync();
        }

        // Success consumes the challenge and marks the email verified.
        var verify = await client.PostAsJsonAsync("/api/auth/otp/verify", new { user.Email, Code = code, Purpose = "email_verify" });
        Assert.True(verify.StatusCode == HttpStatusCode.OK, await verify.Content.ReadAsStringAsync());

        await using (var db = _fixture.CreateContext())
        {
            Assert.True(await db.Users.Where(u => u.Id == user.Id).Select(u => u.EmailVerified).SingleAsync());
            Assert.True(await db.OtpChallenges.Where(o => o.Id == challenge.Id).Select(o => o.Consumed).SingleAsync());
        }

        // Lockout path — seeded challenge already at 3 attempts: first wrong code → 401,
        // second wrong code crosses the budget → 429  and consumes the challenge.
        var locked = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = "00", // never matched — this path only submits wrong codes
            Purpose = "email_verify",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            AttemptCount =3,
            Consumed = false,
            CreatedAt = DateTime.UtcNow.AddSeconds(30)
        };
        await using (var setup2 = _fixture.CreateContext())
        {
            setup2.OtpChallenges.Add(locked);
            await setup2.SaveChangesAsync();
        }

        // First wrong code: attempts →  ‏4  →  ‏401; second wrong code crosses  ‏5  →  429  lockout.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/otp/verify", new { user.Email, Code = "111111", Purpose = "email_verify" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/otp/verify", new { user.Email, Code = "222222", Purpose = "email_verify" })).StatusCode);
    }
}
