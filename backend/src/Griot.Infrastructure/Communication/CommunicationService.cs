using Griot.Application.Interfaces.Services;
using Griot.Infrastructure.Redis;
using Microsoft.Extensions.Logging;

namespace Griot.Infrastructure.Communication;

/// <summary>
/// Rate-guarded facade over the Brevo channels (spec 12).
/// Every outbound message first pays a per-channel, per-recipient Redis sliding
/// window BEFORE hitting Brevo. When the local window is exhausted, the message
/// is dropped with a warning — Brevo's own daily quota/rate caps are therefore
/// never reachable from app code. The existing auth controller limits
/// (OTP 3/15min/email, login 10/15min/IP) sit one layer above this.
/// Never throws.
/// </summary>
public sealed class CommunicationService : ICommunicationService
{
    // Local guardrails (below Brevo's 300/day and per-second caps on every plan).
    private const int EmailLimit = 10;
    private const int EmailWindowSeconds = 900;        // 10 / 15 min per recipient
    private const int SmsLimit = 5;
    private const int SmsWindowSeconds = 3600;         // 5 / hour per phone
    private const int WhatsAppLimit = 5;
    private const int WhatsAppWindowSeconds = 3600;    // 5 / hour per phone

    private readonly IEmailService _email;
    private readonly ISmsService _sms;
    private readonly IWhatsAppService _whatsApp;
    private readonly IContactSynchronizer _contacts;
    private readonly IRedisRateLimiter _rateLimiter;
    private readonly ILogger<CommunicationService> _logger;

    public CommunicationService(
        IEmailService email,
        ISmsService sms,
        IWhatsAppService whatsApp,
        IContactSynchronizer contacts,
        IRedisRateLimiter rateLimiter,
        ILogger<CommunicationService> logger)
    {
        _email = email;
        _sms = sms;
        _whatsApp = whatsApp;
        _contacts = contacts;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!await AcquireAsync("email", message.To, EmailLimit, EmailWindowSeconds, ct).ConfigureAwait(false))
            return false;
        return await _email.SendAsync(message, ct).ConfigureAwait(false);
    }

    public async Task<bool> SendSmsAsync(SmsMessage message, CancellationToken ct = default)
    {
        if (!await AcquireAsync("sms", message.ToPhone, SmsLimit, SmsWindowSeconds, ct).ConfigureAwait(false))
            return false;
        return await _sms.SendAsync(message, ct).ConfigureAwait(false);
    }

    public async Task<bool> SendWhatsAppAsync(WhatsAppMessage message, CancellationToken ct = default)
    {
        if (!await AcquireAsync("whatsapp", message.ToPhone, WhatsAppLimit, WhatsAppWindowSeconds, ct).ConfigureAwait(false))
            return false;
        return await _whatsApp.SendAsync(message, ct).ConfigureAwait(false);
    }

    public async Task<bool> SyncContactAsync(string email, string displayName, bool emailVerified, CancellationToken ct = default)
        => await _contacts.UpsertContactAsync(email, displayName, emailVerified, ct).ConfigureAwait(false);

    private async Task<bool> AcquireAsync(string channel, string recipient, int limit, int windowSeconds, CancellationToken ct)
    {
        var key = $"ratelimit:comm:{channel}:{recipient.ToLowerInvariant()}";
        var result = await _rateLimiter.TryAcquireAsync(key, limit, windowSeconds).ConfigureAwait(false);
        if (result.Allowed) return true;

        _logger.LogWarning(
            "Communication rate limit hit: {Channel} -> {To}: retry in {RetryAfter}s",
            channel, recipient, (int)result.RetryAfter.TotalSeconds);
        return false;
    }
}