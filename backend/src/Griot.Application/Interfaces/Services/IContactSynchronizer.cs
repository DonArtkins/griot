namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Brevo contact sync — the programmatic hook that feeds Brevo Automations.
/// On register we add/update the contact (attributes: FIRSTNAME/LASTNAME,
/// ACCOUNT_STATUS=UNVERIFIED, EMAIL_VERIFIED=false, SIGNUP_DATE). On email
/// verification we flip ACCOUNT_STATUS=VERIFIED + EMAIL_VERIFIED=true, which
/// triggers any \"contact updated\" / list-based automation (welcome, onboarding,
/// re-engagement). Never throws — failures are logged and surfaced via bool.
/// </summary>
public interface IContactSynchronizer
{
    /// <summary>Upsert a contact (creates or updates) with lifecycle attributes.</summary>
    Task<bool> UpsertContactAsync(string email, string displayName, bool emailVerified, CancellationToken ct = default);

    /// <summary>Flip verification attributes on an existing contact.</summary>
    Task<bool> MarkVerifiedAsync(string email, CancellationToken ct = default);
}