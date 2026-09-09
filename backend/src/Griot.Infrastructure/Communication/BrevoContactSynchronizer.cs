using System.Net.Http.Json;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Griot.Infrastructure.Communication;

/// <summary>
/// Brevo Contacts API glue (https://api.brevo.com/v3/contacts).
/// Upsert: POST /v3/contacts { email, attributes, updateEnabled:true }.
/// Attributes: FIRSTNAME, LASTNAME (from displayName), ACCOUNT_STATUS
/// (UNVERIFIED|VERIFIED), EMAIL_VERIFIED (bool).
/// Mark verified: PUT /v3/contacts/{email} { attributes }.
/// This is the programmatic hook Brevo Automations react to (contact created /
/// attribute updated) — e.g. Welcome/Onboarding workflows.
/// Never throws — failures are logged and surfaced via the bool result.
/// </summary>
public sealed class BrevoContactSynchronizer : IContactSynchronizer
{
    private const string ApiUrl = "https://api.brevo.com/v3/contacts";
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<BrevoContactSynchronizer> _logger;

    public BrevoContactSynchronizer(HttpClient http, IConfiguration config, ILogger<BrevoContactSynchronizer> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> UpsertContactAsync(string email, string displayName, bool emailVerified, CancellationToken ct = default)
    {
        var apiKey = ApiKey();
        if (apiKey is null) return false;

        var (firstName, lastName) = SplitName(displayName);
        var payload = new
        {
            email,
            attributes = new
            {
                FIRSTNAME = firstName,
                LASTNAME = lastName,
                ACCOUNT_STATUS = emailVerified ? "VERIFIED" : "UNVERIFIED",
                EMAIL_VERIFIED = emailVerified
            },
            updateEnabled = true
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.TryAddWithoutValidation("api-key", apiKey);
            request.Content = JsonContent.Create(payload);

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Brevo rejected contact upsert {To}: {Status} {Body}", email, (int)response.StatusCode, body);
                return false;
            }
            _logger.LogInformation("Brevo contacted upserted: {To}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo contact upsert {To} failed (including cancellation)", email);
            return false;
        }
    }

    public async Task<bool> MarkVerifiedAsync(string email, CancellationToken ct = default)
    {
        var apiKey = ApiKey();
        if (apiKey is null) return false;

        var payload = new
        {
            attributes = new
            {
                ACCOUNT_STATUS = "VERIFIED",
                EMAIL_VERIFIED = true
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiUrl}/{email}");
            request.Headers.TryAddWithoutValidation("api-key", apiKey);
            request.Content = JsonContent.Create(payload);

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Brevo rejected contact update {To}: {Status} {Body}", email, (int)response.StatusCode, body);
                return false;
            }
            _logger.LogInformation("Brevo contact marked verified: {To}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo contact update {To} failed (including cancellation)", email);
            return false;
        }
    }

    private string? ApiKey()
    {
        var apiKey = _config["Brevo:ApiKey"] ?? _config["BREVO_API_KEY"] ?? _config["Brevo__ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Brevo API key is not configured (set Brevo:ApiKey / BREVO_API_KEY). Contact sync skipped.");
            return null;
        }
        return apiKey;
    }

    private static (string First, string Last) SplitName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return ("", "");
        var parts = displayName.Trim().Split(' ');
        if (parts.Length == 0) return ("", "");
        if (parts.Length == 1) return (parts[0], "");
        return (parts[0], parts[1]);
    }
}