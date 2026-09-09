using Griot.Application.Interfaces.Services;
using Griot.Infrastructure.Communication;
using Griot.Infrastructure.Redis;
using Microsoft.Extensions.Logging;
using Moq;

namespace Griot.Tests.Auth;

/// <summary>
/// Unit tests for the rate-guarded <see cref="CommunicationService"/> facade.
/// Verifies that local Redis windows gate every channel BEFORE Brevo is called,
/// so app code can never trip Brevo's own rate/quota caps.
/// </summary>
public class CommunicationServiceTests
{
    private CommunicationService BuildService(
        Mock<IRedisRateLimiter> limiter,
        Mock<IEmailService>? email = null,
        Mock<ISmsService>? sms = null,
        Mock<IWhatsAppService>? whatsApp = null,
        Mock<IContactSynchronizer>? contacts = null)
    {
        return new CommunicationService(
            (email ?? new Mock<IEmailService>()).Object,
            (sms ?? new Mock<ISmsService>()).Object,
            (whatsApp ?? new Mock<IWhatsAppService>()).Object,
            (contacts ?? new Mock<IContactSynchronizer>()).Object,
            limiter.Object,
            Mock.Of<ILogger<CommunicationService>>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Email — under the local window → delegates to Brevo
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendEmail_UnderLimit_DelegatesToBrevo()
    {
        var limiter = new Mock<IRedisRateLimiter>();
        limiter.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
              .ReturnsAsync(new RateLimitResult(true, 9, TimeSpan.Zero));
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = BuildService(limiter, email);
        var ok = await sut.SendEmailAsync(new EmailMessage("a@b.co", "Hi", "<p>Hi</p>"));

        Assert.True(ok);
        email.Verify(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SMS — local window exhausted → blocked BEFORE Brevo
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendSms_RateLimited_DoesNotDelegate()
    {
        var limiter = new Mock<IRedisRateLimiter>();
        limiter.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
              .ReturnsAsync(new RateLimitResult(false, 0, TimeSpan.FromSeconds(60)));
        var sms = new Mock<ISmsService>();
        sms.Setup(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = BuildService(limiter, sms: sms);
        var ok = await sut.SendSmsAsync(new SmsMessage("+254700000000", "Griot code: 123456"));

        Assert.False(ok);
        sms.Verify(s => s.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // WhatsApp — local window exhausted → blocked BEFORE Brevo
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendWhatsApp_RateLimited_DoesNotDelegate()
    {
        var limiter = new Mock<IRedisRateLimiter>();
        limiter.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
              .ReturnsAsync(new RateLimitResult(false, 0, TimeSpan.FromSeconds(60)));
        var whatsApp = new Mock<IWhatsAppService>();
        whatsApp.Setup(w => w.SendAsync(It.IsAny<WhatsAppMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = BuildService(limiter, whatsApp: whatsApp);
        var ok = await sut.SendWhatsAppAsync(new WhatsAppMessage("+254700000000", "Griot code: 123456"));

        Assert.False(ok);
        whatsApp.Verify(w => w.SendAsync(It.IsAny<WhatsAppMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Contact sync — always delegated (best-effort automation hook)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncContact_DelegatesToSynchronizer()
    {
        var limiter = new Mock<IRedisRateLimiter>();
        var contacts = new Mock<IContactSynchronizer>();
        contacts.Setup(c => c.UpsertContactAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var sut = BuildService(limiter, contacts: contacts);
        var ok = await sut.SyncContactAsync("a@b.co", "Amara Ke", false);

        Assert.True(ok);
        contacts.Verify(c => c.UpsertContactAsync("a@b.co", "Amara Ke", false, It.IsAny<CancellationToken>()), Times.Once);
    }
}