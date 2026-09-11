using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Griot.Api.Auth;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Griot.Tests.Ai;

public sealed class AiHttpBoundaryTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;
    private readonly string _webhookKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly Guid _workspace = Guid.NewGuid();

    public AiHttpBoundaryTests()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var userId = TestUserRepositoryStub.TestOboUserId;
        var prefix = $"ServiceToken:Delegations:{userId:D}";
        var config = new Dictionary<string, string?>
        {
            ["ServiceToken:Key"] = token, ["Webhook:Secret"] = _webhookKey,
            ["JWT:Key"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            ["ConnectionStrings:Default"] = "Server=localhost;Database=UnusedAiBoundaryTests;Integrated Security=true;TrustServerCertificate=true",
            ["Redis:Connection"] = "localhost:6380,abortConnect=false",
            [$"{prefix}:ExpiresAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(15).ToString("O"),
            [$"{prefix}:WorkspaceIds:0"] = _workspace.ToString("D")
        };
        for (var i = 0; i < AiAccess.Scopes.Count; i++) config[$"{prefix}:Scopes:{i}"] = AiAccess.Scopes[i];
        var domain = new Mock<IDomainService>(MockBehavior.Strict);
        domain.Setup(d => d.GetWorkspacesAsync(userId)).ReturnsAsync(new List<WorkspaceDto>
        {
            new() { Id = _workspace, Name = "Granted workspace" },
            new() { Id = Guid.NewGuid(), Name = "Other workspace" }
        });
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Griot.sln"))) dir = dir.Parent;
        var contentRoot = Path.Combine(dir!.FullName, "src", "Griot.Api");
        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseContentRoot(contentRoot).UseEnvironment("Production")
            .ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(config))
            .ConfigureTestServices(services =>
            {
                services.RemoveAll<IGenericRepository<User>>();
                services.AddScoped<IGenericRepository<User>, TestUserRepositoryStub>();
                services.RemoveAll<IDomainService>();
                services.AddSingleton(domain.Object);
            }));
        _client = _app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        _client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        _client.DefaultRequestHeaders.Add(ServiceTokenHandler.OnBehalfOfHeaderName, userId.ToString("D"));
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/refresh")]
    [InlineData("/api/auth/logout")]
    [InlineData("/api/auth/otp/request")]
    [InlineData("/api/auth/otp/verify")]
    [InlineData("/api/workspaces")]
    [InlineData("/api/notifications/read-all")]
    public async Task HumanOnlyAction_Returns403BeforeModelBinding(string path)
    {
        using var result = await _client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task UnlistedTaskUpdate_Returns403()
    {
        using var result = await _client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", new { title = "No write" });
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData("deleteWorkspace")]
    [InlineData("deleteProject")]
    [InlineData("deleteTask")]
    public async Task GraphqlDeletes_ReturnErrorWith200JsonTransport(string mutation)
    {
        var result = await _client.PostAsJsonAsync("/graphql", new { query = $"mutation {{ {mutation}(id: \"{Guid.NewGuid()}\") }}" });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
        Assert.NotEmpty(json.RootElement.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task GraphqlUnlistedCreation_IsDeniedBeforeDatabaseAccess()
    {
        using var result = await _client.PostAsJsonAsync("/graphql", new { query = "mutation { createWorkspace(name: \"No write\", slug: \"no-write\") { id } }" });
        using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
        var error = json.RootElement.GetProperty("errors")[0];
        Assert.Equal("FORBIDDEN", error.GetProperty("extensions").GetProperty("code").GetString());
    }

    [Fact]
    public async Task WorkspaceList_IsFilteredByDelegation()
    {
        using var result = await _client.GetAsync("/api/workspaces");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var rows = await result.Content.ReadFromJsonAsync<List<WorkspaceDto>>();
        Assert.Equal(_workspace, Assert.Single(rows!).Id);
    }

    [Fact]
    public async Task CrossWorkspaceAndRawLogs_AreDenied()
    {
        using var other = await _client.GetAsync($"/api/workspaces/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        using var raw = await _client.GetAsync($"/api/logs/errors?workspaceId={_workspace}");
        Assert.Equal(HttpStatusCode.Forbidden, raw.StatusCode);
    }

    [Fact]
    public async Task VerifiedCallbackAndReplay_StayRetryableUntilDurableDispatchExists()
    {
        const string body = "{}";
        var signature = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_webhookKey), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        for (var i = 0; i < 2; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/trigger") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            request.Headers.Add("X-Trigger-Signature", signature);
            // Callbacks use HMAC, not the restricted data-plane principal.
            request.Headers.Authorization = new("Bearer", "invalid-test-token");
            using var result = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        }
    }

    public void Dispose() { _client.Dispose(); _app.Dispose(); }
}
