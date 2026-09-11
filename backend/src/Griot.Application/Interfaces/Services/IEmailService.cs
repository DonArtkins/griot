namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Transactional email delivery contract.
/// Infrastructure owns the implementation (BrevoEmailService); Application only sees this interface.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a transactional email.
    /// Returns true when Brevo accepted the message (2xx); false when delivery failed.
    /// Implementations must never throw — surface errors via the return value.
    /// </summary>
    Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default);
}

/// <summary>A transactional email message to send via Brevo.</summary>
public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? ReplyTo = null
);
