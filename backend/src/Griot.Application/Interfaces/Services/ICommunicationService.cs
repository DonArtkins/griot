namespace Griot.Application.Interfaces.Services;

/// <summary>Delivery channel supported by the Griot communication layer.</summary>
public enum CommunicationChannel { Email, Sms, WhatsApp }

/// <summary>
/// The one entry point every Griot service uses to reach users.
/// Applies per-channel, per-recipient sliding-window rate limits (Redis) BEFORE
/// delegating to Brevo — so the system can never accidentally trip Brevo's own
/// rate/quota caps. Each SendAsync returns false when rate-limited, misconfigured,
/// or rejected; never throws.
/// </summary>
public interface ICommunicationService
{
    /// <summary>Send a transactional email (rate-limited per recipient).</summary>
    Task<bool> SendEmailAsync(EmailMessage message, CancellationToken ct = default);

    /// <summary>Send a transactional SMS (rate-limited per phone number).</summary>
    Task<bool> SendSmsAsync(SmsMessage message, CancellationToken ct = default);

    /// <summary>Send a transactional WhatsApp message (rate-limited per phone number).</summary>
    Task<bool> SendWhatsAppAsync(WhatsAppMessage message, CancellationToken ct = default);

    /// <summary>
    /// Sync a user's contact lifecycle to Brevo (feeds Automations).
    /// Not rate-limited in the same way (1–2 calls per user lifetime), but still
    /// fault-tolerant.
    /// </summary>
    Task<bool> SyncContactAsync(string email, string displayName, bool emailVerified, CancellationToken ct = default);
}