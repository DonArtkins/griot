using System.Net.Http.Headers;
using System.Net.Http.Json;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Griot.Infrastructure.Email;

/// <summary>
/// Resend transactional email transport (https://api.resend.com/emails).
/// Reads `Resend:ApiKey` (fallbacks: `RESEND_API_KEY`, `Resend__ApiKey`),
/// `Resend:FromEmail` (fallback `RESEND_FROM_EMAIL`; default `Griot &lt;onboarding@resend.dev&gt;`),
/// and `Resend:ContactToEmail` (fallback `CONTACT_TO_EMAIL`; admin notification inbox).
/// Never throws — delivery failures are logged and surfaced via the bool result.
/// </summary>
public sealed class ResendEmailService : IEmailService
{
    private const string ApiUrl = "https://api.resend.com/emails";
    private const string DefaultFrom = "Griot <onboarding@resend.dev>";
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(HttpClient http, IConfiguration config, ILogger<ResendEmailService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var apiKey = _config["Resend:ApiKey"] ?? _config["RESEND_API_KEY"] ?? _config["Resend__ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Resend API key is not configured (set Resend:ApiKey / RESEND_API_KEY). Email to {To} skipped.", message.To);
            return false;
        }

        var from = message.From ?? _config["Resend:FromEmail"] ?? _config["RESEND_FROM_EMAIL"] ?? DefaultFrom;
        var payload = new
        {
            from,
            to = new string[] { message.To },
            subject = message.Subject,
            html = message.HtmlBody,
            text = PlainText(message.HtmlBody),
            tags = new Object[] { new { name = "system", value = "griot" }, new { name = "type", value = "auth" } }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(payload);

            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Resend rejected email to {To}: {Status} {Body}", message.To, (int)response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("Resend accepted email to {To} subject={Subject}", message.To, message.Subject);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Resend delivery to {To} failed", message.To);
            return false;
        }
    }

    /// <summary>Rough HTML→plaintext fallback for the email text part (code stays readable).</summary>
    private static string PlainText(string html)
        => System.Net.WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
}