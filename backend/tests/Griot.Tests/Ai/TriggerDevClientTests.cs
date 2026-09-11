using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Griot.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Griot.Tests.Ai;

public sealed class TriggerDevClientTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Uri? Url { get; private set; }
        public string? Body { get; private set; }
        public string? Token { get; private set; }
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public bool Throw { get; init; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Url = request.RequestUri;
            Token = request.Headers.Authorization?.Parameter;
            Body = await request.Content!.ReadAsStringAsync(ct);
            if (Throw) throw new HttpRequestException("Simulated outage");
            return new HttpResponseMessage(Status);
        }
    }

    [Theory]
    [InlineData("http://api.trigger.dev")]
    [InlineData("https://")]
    [InlineData("/relative")]
    [InlineData("https://user:password@example.com")]
    [InlineData("https://api.trigger.dev?redirect=example.com")]
    [InlineData("https://api.trigger.dev#fragment")]
    public async Task InvalidEndpoint_NeverAttachesOrSendsCredentials(string endpoint)
    {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var client = Create(http, endpoint, out _);
        Assert.False(await client.EnqueueAsync("test-task", new { message = "test" }));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task BlankPrimaryKey_UsesFallback_AndSendsPayloadEnvelope()
    {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var client = Create(http, "https://api.trigger.dev", out var key);
        Assert.True(await client.EnqueueAsync("test-task", new { message = "test" }));
        Assert.Equal(key, handler.Token);
        Assert.Equal("https://api.trigger.dev/api/v1/tasks/test-task/trigger", handler.Url!.AbsoluteUri);
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("test", json.RootElement.GetProperty("payload").GetProperty("message").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, false)]
    [InlineData(HttpStatusCode.Found, false)]
    [InlineData(HttpStatusCode.OK, true)]
    public async Task Failure_IsReturnedToRecoveryCaller(HttpStatusCode status, bool networkFailure)
    {
        using var handler = new RecordingHandler { Status = status, Throw = networkFailure };
        using var http = new HttpClient(handler);
        Assert.False(await Create(http, null, out _).EnqueueAsync("test-task"));
        Assert.Equal(1, handler.Calls);
    }

    private static TriggerDevClient Create(HttpClient http, string? url, out string key)
    {
        key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Trigger:ApiUrl"] = url, ["Trigger:SecretKey"] = "   ", ["TRIGGER_SECRET_KEY"] = key
        }).Build();
        return new TriggerDevClient(http, config, NullLogger<TriggerDevClient>.Instance);
    }
}
