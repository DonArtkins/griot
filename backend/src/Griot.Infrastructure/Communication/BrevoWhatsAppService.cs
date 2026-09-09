using System.Net.Http.Json;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Griot.Infrastructure.Communication;

/// <summary>
/// Brevo WhatsApp transport (https://api.brevo.com/v3/whatsapp/sendMessage).
/// Config: `Brevo:WhatsApp:SenderNumber` (REQUIRED, with country code),
/// `Brevo:WhatsApp:TemplateId` (default template when the message has no
/// TemplateId). Requires the WhatsApp feature enabled + WhatsApp Business
/// account linked in the Brevo dashboard (see docs/communication/COMMUNICATION-GUIDE.md).
/// Never throws — failures are logged and surfaced via the bool result.
/// </summary>
public sealed class BrevoWhatsAppService : IWhatsAppService
{
    private const string ApiUrl = "https://api.brevo.com/v3/whatsapp/sendMessage";
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<BrevoWhatsAppService> _logger;

    public BrevoWhatsAppService(HttpClient http, IConfiguration config, ILogger<BrevoWhatsAppService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendAsync(WhatsAppMessage message, CancellationToken ct = default)
    {
        var apiKey = _config["Brevo:ApiKey"] ?? _config["BREVO_API_KEY"] ?? _config["Brevo__ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Brevo API key is not configured (set Brevo:ApiKey / BREVO_API_KEY). WhatsApp to {To} skipped.", message.ToPhone);
            return false;
        }

        var senderNumber = _config["Brevo:WhatsApp:SenderNumber"] ?? _config["BREVO_WHATSAPP_SENDER_NUMBER"];
        if (string.IsNullOrWhiteSpace(senderNumber))
        {
            _logger.LogWarning("Brevo WhatsApp senderNumber is not configured (set Brevo:WhatsApp:SenderNumber). WhatsApp to {To} skipped.", message.ToPhone);
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.TryAddWithoutValidation("api-key", apiKey);

            if (message.TemplateId is not null)
            {
                request.Content = JsonContent.Create(new
                {
                    senderNumber,
                    contactNumbers = new string[] { message.ToPhone },
                    templateId = message.TemplateId
                });
            }
            else
            {
                var text = message.Text ?? "Griot notification.";
                request.Content = JsonContent.Create(new
                {
                    senderNumber,
                    contactNumbers = new string[] { message.ToPhone },
                    text
                });
            }

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Brevo rejected WhatsApp to {To}: {Status} {Body}", message.ToPhone, (int)response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("Brevo accepted WhatsApp to {To}", message.ToPhone);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo WhatsApp delivery to {To} failed (including cancellation)", message.ToPhone);
            return false;
        }
    }
}