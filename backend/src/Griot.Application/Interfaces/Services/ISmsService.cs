namespace Griot.Application.Interfaces.Services;

/// <summary>Transactional SMS delivery contract (Brevo Transactional SMS).</summary>
public interface ISmsService
{
    /// <summary>
    /// Send a transactional SMS via Brevo https://api.brevo.com/v3/transactionalSMS/send.
    /// Returns true when Brevo accepted the message (2xx); false when delivery or
    /// configuration failed. Implementations must never throw.
    /// </summary>
    Task<bool> SendAsync(SmsMessage message, CancellationToken ct = default);
}

/// <summary>A transactional SMS message (recipient with country code, no spaces).</summary>
public record SmsMessage(
    string ToPhone,
    string Content,
    string? Sender = null
);