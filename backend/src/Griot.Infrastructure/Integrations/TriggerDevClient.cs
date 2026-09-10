using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Griot.Infrastructure.Integrations;

/// <summary>
/// Server-to-server client for enqueuing Trigger.dev tasks from the .NET backend.
///
/// Auth: static <c>TRIGGER_SECRET_KEY</c> API key (Bearer); never exposed to web/mobile.
/// Enqueue-after-persist pattern: call ONLY after the domain write commits. If the enqueue
/// fails, the domain write stands — Trigger.dev is a compute adapter, not a transaction participant.
///
/// Config lookup order:
///   1. Configuration["Trigger:SecretKey"]
///   2. env: TRIGGER_SECRET_KEY
/// </summary>
public sealed class TriggerDevClient
{
    private readonly HttpClient      _http;
    private readonly IConfiguration  _config;
    private readonly ILogger<TriggerDevClient> _logger;

    // Default Trigger.dev REST API base.  Override via Configuration["Trigger:ApiUrl"] for self-hosted.
    private const string DefaultApiUrl = "https://api.trigger.dev";

    public TriggerDevClient(HttpClient http, IConfiguration config, ILogger<TriggerDevClient> logger)
    {
        _http   = http;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a Trigger.dev task by task ID, passing an optional payload.
    /// Returns <c>true</c> on success (2xx), <c>false</c> on any network or API error.
    /// Failures are logged but NOT thrown — the caller's domain write must not be rolled back.
    /// </summary>
    /// <param name="taskId">Trigger.dev task identifier (e.g. "due-reminders").</param>
    /// <param name="payload">Optional payload serialized as the task input.</param>
    public async Task<bool> EnqueueAsync(string taskId, object? payload = null)
    {
        var secretKey = _config["Trigger:SecretKey"] ?? _config["TRIGGER_SECRET_KEY"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogWarning("Trigger.dev secret key not configured; task '{TaskId}' not enqueued.", taskId);
            return false;
        }

        var baseUrl = _config["Trigger:ApiUrl"]?.TrimEnd('/') ?? DefaultApiUrl;
        var url     = $"{baseUrl}/api/v1/tasks/{Uri.EscapeDataString(taskId)}/trigger";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = payload is not null
                    ? JsonContent.Create(payload)
                    : new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Trigger.dev task '{TaskId}' enqueued successfully.", taskId);
                return true;
            }

            _logger.LogWarning(
                "Trigger.dev enqueue for '{TaskId}' returned {StatusCode}.",
                taskId, (int)response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            // Failure must NOT propagate — domain write is already committed.
            _logger.LogError(ex, "Trigger.dev enqueue for '{TaskId}' failed with exception.", taskId);
            return false;
        }
    }
}
