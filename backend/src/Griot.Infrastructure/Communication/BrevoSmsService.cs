using System.Net.Http.Json;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Griot.Infrastructure.Communication;

/// <summary>
/// Brevo Transactional SMS transport (https://api.brevo.com/v3/transactionalSMS/send).
/// Config: `Brevo:Sms:Sender` (≤11 alphanumeric chars; default `Griot`),
/// `Brevo:Sms:Tag` (default `griot-tx`). api-key from `Brevo:ApiKey` fallbacks.
/// Non-promotional messages use `type: "transactional"` (never marketing — no
/// [STOP CODE] requirement on transactional messages).
/// Never throws — failures are logged and surfaced via the bool result.
/// </summary>
public sealed class BrevoSmsService : ISmsService
{
    private const string ApiUrl = "https://api.brevo.com/v3/transactionalSMS/send";
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<BrevoSmsService> _logger;

    public BrevoSmsService(HttpClient http, IConfiguration config, ILogger<BrevoSmsService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendAsync(SmsMessage message, CancellationToken ct = default)
    {
        var apiKey = _config["Brevo:ApiKey"] ?? _config["BREVO_API_KEY"] ?? _config["Brevo__ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Brevo API key is not configured (set Brevo:ApiKey / BREVO_API_KEY). SMS to {To} skipped.", message.ToPhone);
            return false;
        }

        var sender = message.Sender ?? _config["Brevo:Sms:Sender"] ?? _config["BREVO_SMS_SENDER"] ?? "Griot";
        var tag = _config["Brevo:Sms:Tag"] ?? _config["BREVO_SMS_TAG"] ?? "griot-tx";

        var payload = new
        {
            sender,
            recipient = message.ToPhone,
            content = message.Content,
            type = "transactional",
            tag,
            unicodeEnabled = true
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
                _logger.LogWarning("Brevo rejected SMS to {To}: {Status} {Body}", message.ToPhone, (int)response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("Brevo accepted SMS to {To}", message.ToPhone);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo SMS delivery to {To} failed (including cancellation)", message.ToPhone);
            return false;
        }
    }
}