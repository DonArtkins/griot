using System.Net.Http.Headers;
using System.Net.Http.Json;
using Griot.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Griot.Infrastructure.Email;

/// <summary>
/// Brevo transactional email transport (https://api.brevo.com/v3/smtp/email).
/// Reads `Brevo:ApiKey` (fallbacks: `BREVO_API_KEY`, `Brevo__ApiKey`),
/// `Brevo:FromEmail` (fallback `BREVO_FROM_EMAIL`; REQUIRED — Brevo only delivers
/// from a sender email/domain you verified in the Brevo dashboard),
/// `Brevo:FromName` (fallback `BREVO_FROM_NAME`; default `Griot`).
/// Replaces the previous Resend transport (Resend sandbox restrictions blocked
/// sending OTP to arbitrary test recipients on the free `vercel.app` domain;
/// Brevo has no recipient sandbox and 300 emails/day on the free plan).
/// Never throws — delivery failures are logged and surfaced via the bool result.
/// </summary>
public sealed class BrevoEmailService : IEmailService
{
    private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";
    private const string DefaultFromName = "Griot";
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(HttpClient http, IConfiguration config, ILogger<BrevoEmailService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var apiKey = _config["Brevo:ApiKey"] ?? _config["BREVO_API_KEY"] ?? _config["Brevo__ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Brevo API key is not configured (set Brevo:ApiKey / BREVO_API_KEY). Email to {To} skipped.", message.To);
            return false;
        }

        var (fromName, fromEmail) = ResolveSender(message.From);
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogWarning(
                "Brevo sender is not configured (set Brevo:FromEmail / BREVO_FROM_EMAIL to an email you verified in the Brevo dashboard). Email to {To} skipped.",
                message.To);
            return false;
        }

        var payload = new
        {
            sender = new { name = fromName, email = fromEmail },
            to = new Object[] { new { email = message.To } },
            subject = message.Subject,
            htmlContent = message.HtmlBody,
            textContent = PlainText(message.HtmlBody)
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
                _logger.LogWarning("Brevo rejected email to {To}: {Status} {Body}", message.To, (int)response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("Brevo accepted email to {To} subject={Subject}", message.To, message.Subject);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Brevo delivery to {To} failed", message.To);
            return false;
        }
    }

    /// <summary>
    /// Resolve (name, email) for the sender. `message.From` may override the
    /// configured default using the standard "Name &lt;email&gt;" shape.
    /// Keyword syntax: `private (string? Name, string? Email) ResolveSender(string? from)`.
    /// </summary>
    private (string? Name, string? Email) ResolveSender(string? from)
    {
        var configured = _config["Brevo:FromEmail"] ?? _config["BREVO_FROM_EMAIL"];
        if (string.IsNullOrWhiteSpace(configured))
            return (null, null);

        var fromName = _config["Brevo:FromName"] ?? _config["BREVO_FROM_NAME"] ?? DefaultFromName;
        var sender = from ?? configured;

        var m = Regex.Match(sender, @"^(.*?)\s*<([^>]+)>\s*$");
        if (m.Success)
        {
            var name = m.Groups[1].Value;
            return (string.IsNullOrWhiteSpace(name) ? fromName : name.Trim(), m.Groups[2].Value.Trim());
        }

        return (fromName, sender.Trim());
    }

    /// <summary>Rough HTML→plaintext fallback for the email text part (code stays readable).</summary>
    private static string PlainText(string html)
        => System.Net.WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
}