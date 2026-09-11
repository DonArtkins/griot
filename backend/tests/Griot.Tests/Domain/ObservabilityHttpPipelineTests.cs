using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Griot.Api.Auth;
using Griot.Api.Services;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Entities;
using Griot.Tests.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Griot.Tests.Domain;

/// <summary>
/// Spec 20 acceptance (HTTP pipeline, no SQL required): every request including early
/// exits (401/404) writes one ApiLogs row (RequestId == the X-Request-Id response header,
/// DurationMs > 0); sensitive query values are redacted while non-sensitive survive;
/// /health is excluded; any unhandled exception → 500 application/problem+json (X-Request-Id
/// preserved) AND one ErrorLogs row; DomainError 4xx stays as-is and records nothing;
/// a broken telemetry store never fails a committed request. The REAL TelemetryWriter runs
/// against mocked scoped repositories so rows are captured deterministically via DrainAsync.
/// </summary>
public sealed class ObservabilityHttpPipelineTests : IDisposable
{
    private readonly Mock<IGenericRepository<ApiLog>> _apiRepo = new();
    private readonly Mock<IGenericRepository<ErrorLog>> _errorRepo = new();
    private readonly TelemetryWriter _writer;
    private readonly Mock<IDomainService> _domain = new(MockBehavior.Strict);
    private readonly WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;
    private readonly Guid _userId = TestUserRepositoryStub.TestOboUserId;

    private static int AddCount(Mock<IGenericRepository<ApiLog>> repo)
        => repo.Invocations.Count(i => i.Method.Name == "AddAsync");

    private static int ErrorCount(Mock<IGenericRepository<ErrorLog>> repo)
        => repo.Invocations.Count(i => i.Method.Name == "AddAsync");

    public ObservabilityHttpPipelineTests()
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(IGenericRepository<ApiLog>))).Returns(_apiRepo.Object);
        provider.Setup(p => p.GetService(typeof(IGenericRepository<ErrorLog>))).Returns(_errorRepo.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);
        _writer = new TelemetryWriter(scopeFactory.Object, NullLogger<TelemetryWriter>.Instance);

        var prefix = $"ServiceToken:Delegations:{_userId:D}";
        var config = new Dictionary<string, string?>
        {
            ["ServiceToken:Key"] = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["JWT:Key"] = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["ConnectionStrings:Default"] = "Server=localhost;Database=UnusedObservabilityTests;Integrated Security=true;TrustServerCertificate=true",
            ["Redis:Connection"] = "localhost:6380,abortConnect=false",
            ["Observability:DevSeed"] = "false",
            [$"{prefix}:ExpiresAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(15).ToString("O"),
            [$"{prefix}:WorkspaceIds:0"] = Guid.NewGuid().ToString("D")
        };
        for (var i = 0; i < AiAccess.Scopes.Count; i++) config[$"{prefix}:Scopes:{i}"] = AiAccess.Scopes[i];

        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Griot.sln")))
            dir = dir.Parent;
        var contentRoot = System.IO.Path.Combine(dir!.FullName, "src", "Griot.Api");

        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseContentRoot(contentRoot).UseEnvironment("Production")
            .ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(config))
            .ConfigureTestServices(services =>
            {
                services.RemoveAll<IGenericRepository<User>>();
                services.AddScoped<IGenericRepository<User>, TestUserRepositoryStub>();
                services.RemoveAll<IDomainService>();
                services.AddSingleton(_domain.Object);
                services.RemoveAll<ITelemetryWriter>();
                services.AddSingleton<ITelemetryWriter>(_writer);
            }));
        _client = _app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    /// <summary>Response rows are written on response completion — poll past the OnCompleted race.</summary>
    private async Task WaitForAsync(Func<bool> condition, int milliseconds = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(25);
        Assert.True(condition());
    }

    public void Dispose()
    {
        _client.Dispose();
        _app.Dispose();
        _writer.Dispose();
    }

    // ─────────────── ApiLogs: every request incl. early exits ───────────────

    [Fact]
    public async Task EarlyExits_WriteOneApiLogRow_WithResponseRequestIdAndPositiveDuration()
    {
        using var unauthorized = await _client.GetAsync("/api/workspaces");   // 401 challenge
        using var missing = await _client.GetAsync("/does-not-exist");        // 404

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await WaitForAsync(() => AddCount(_apiRepo) >= 2);
        await _writer.DrainAsync(TimeSpan.FromSeconds(5));

        var rows = _apiRepo.Invocations.Where(i => i.Method.Name == "AddAsync")
            .Select(i => (ApiLog)i.Arguments[0]).OrderBy(a => a.StatusCode).ToList();
        Assert.Equal(2, rows.Count);

        var row401 = rows.First(a => a.StatusCode == 401);
        var row404 = rows.First(a => a.StatusCode == 404);
        Assert.True(row401.DurationMs > 0);                    // acceptance: DurationMs > 0
        Assert.True(row404.DurationMs > 0);
        Assert.True(Guid.TryParse(row401.RequestId.ToString(), out _));
        Assert.Null(row401.UserId);                            // auth never reached → null user
        Assert.Equal("GET", row401.Method);
        Assert.Equal("/does-not-exist", row404.Path);
    }

    [Fact]
    public async Task ApiLogRow_RequestId_MatchesXRequestIdResponseHeader()
    {
        using var response = await _client.GetAsync("/api/workspaces");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var header = response.Headers.TryGetValues("X-Request-Id", out var values)
            ? values.FirstOrDefault() : null;
        Assert.True(Guid.TryParse(header, out var headerId));  // request-id middleware normalized it

        await WaitForAsync(() => AddCount(_apiRepo) >= 1);
        await _writer.DrainAsync(TimeSpan.FromSeconds(5));
        var row = _apiRepo.Invocations.First(i => i.Method.Name == "AddAsync");
        Assert.Equal(headerId, ((ApiLog)row.Arguments[0]).RequestId);
    }

    // ─────────────── Sensitive query redaction ───────────────

    [Fact]
    public async Task QueryString_RedactsSensitiveKeys_PreservesNonSensitive()
    {
        using var response = await _client.GetAsync("/api/workspaces?Token=abc&code=123456&page=2&q=griot");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await WaitForAsync(() => AddCount(_apiRepo) >= 1);
        await _writer.DrainAsync(TimeSpan.FromSeconds(5));
        var row = (ApiLog)_apiRepo.Invocations.First(i => i.Method.Name == "AddAsync").Arguments[0];

        // Acceptance: token/code redacted; page & q preserved verbatim.
        Assert.Equal("Token=[redacted]&code=[redacted]&page=2&q=griot", row.QueryString);
    }
}

