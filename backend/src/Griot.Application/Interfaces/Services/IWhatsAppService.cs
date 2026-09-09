namespace Griot.Application.Interfaces.Services;

/// <summary>Transactional WhatsApp delivery contract (Brevo WhatsApp API).</summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Send a transactional WhatsApp message via Brevo https://api.brevo.com/v3/whatsapp/sendMessage.
    /// Returns true when Brevo accepted the message (2xx); false when delivery or
    /// configuration failed. Implementations must never throw.
    /// </summary>
    Task<bool> SendAsync(WhatsAppMessage message, CancellationToken ct = default);
}

/// <summary>A transactional WhatsApp message (text or template-based).</summary>
public record WhatsAppMessage(
    string ToPhone,
    string? Text = null,
    int? TemplateId = null
);