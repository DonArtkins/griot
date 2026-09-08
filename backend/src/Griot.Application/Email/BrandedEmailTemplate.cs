using System.Net;

namespace Griot.Application.Email;

/// <summary>
/// Branded HTML email shell + per-purpose renderers (OTP codes, admin new-account notice).
/// Faithful C# port of the Sababisha website's Resend templates — same brand tokens (bg/card/ink/muted/
/// accent/border/soft), Inter + DM Serif Display, the accent-with-swervy heading stroke,and table-based
/// layout for Outlook/Gmail. Every email type customizes preheader/eyebrow/title/accent word/body/
/// footerNote —"customized for each and every email" (research/ai-features-research.md §1.6).
/// </summary>
public static class BrandedEmailTemplate
{
    private const string Bg = "#EFEFEF";
    private const string Card = "#FFFFFF";
    private const string Ink = "#111111";
    private const string Muted = "#666666";
    private const string Accent = "#FF5A36";
    private const string Border = "#EEEEEE";
    private const string Soft = "#FAFAFA";
    private const string FontSans = "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    private const string FontDisplay = "'DM Serif Display', Georgia, 'Times New Roman', serif";

    private static string Esc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>The brand swervy stroke (inline SVG — no external image dependency(。</summary>
    private static string Swervy(int width)
    {
        var q = Math.Max(8, width / 4);
        var q2 = width - q;
        return $@"<svg width=""{width}"" height=""10"" viewBox=""0 0 {width} 10"" xmlns=""http://www.w3.org/2000/svg"" style=""display:block;width:{width}px;max-width:100%;height:10px;border:0;margin:0;padding:0;""><path d=""M1 8 Q {q} 2 {width / 2} 7 T {q2} 6"" stroke=""{Accent}"" stroke-width=""3"" fill=""none"" stroke-linecap=""round""/></svg>";
    }

    private static string BuildTitle(string title, string? accentWord)
    {
        if (string.IsNullOrEmpty(accentWord))
            return $"<span style=\"color:{Ink};\">{Esc(title)}</span>";

        var idx = title.IndexOf(accentWord, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return $"<span style=\"color:{Ink};\">{Esc(title)}</span>";

        var before = title[..idx];
        var match = title.Substring(idx, accentWord.Length);
        var after = title[(idx + accentWord.Length)..];
        var underline = Math.Clamp(match.Length * 16, 80, 240);
        return $"<span style=\"color:{Ink};\">{Esc(before)}</span><span style=\"font-family:{FontDisplay};font-size:32px;font-style:italic;font-weight:400;color:{Accent};\">{Esc(match)}</span><span style=\"color:{Ink};\">{Esc(after)}</span><div style=\"line-height:0;margin:2px 0 0;padding:0;\">{Swervy(underline)}</div>";
    }

    private static string CodeBox(string code) => $@"
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:0 0 16px;"">
        <tr><td align=""center"" style=""background:{Soft};border:1px solid {Border};border-radius:18px;padding:22px 20px;"">
            <span style=""font-family:{FontSans};font-size:34px;font-weight:700;letter-spacing:0.35em;color:{Ink};"">{Esc(code)}</span>
        </td></tr></table>";

    private static string SectionCard(string title, string bodyHtml) => $@"
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:0 0 16px;"">
        <tr><td style=""background:{Soft};border:1px solid {Border};border-radius:18px;padding:18px 20px;"">
            <p style=""margin:0 0 8px;font-family:{FontSans};font-size:11px;letter-spacing:0.12em;text-transform:uppercase;color:{Accent};font-weight:700;"">{Esc(title)}</p>
            <div style=""font-family:{FontSans};font-size:15px;line-height:1.65;color:{Ink};"">{bodyHtml}</div>
        </td></tr></table>";

    private static string DetailRow(string label, string valueHtml, bool isLast = false) => $@"
        <tr><td style=""padding:12px 0;{(isLast ? "" : $"border-bottom:1px solid {Border};")}width:34%;vertical-align:top;font-family:{FontSans};font-size:13px;color:{Muted};"">{Esc(label)}</td>
        <td style=""padding:12px 0;{(isLast ? "" : $"border-bottom:1px solid {Border};")}vertical-align:top;font-family:{FontSans};font-size:15px;color:{Ink};font-weight:700;"">{valueHtml}</td></tr>";

    private static string BrandedHeader() => $@"
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:0 0 28px;"">
        <tr><td align=""left"">
        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""border-collapse:separate;"">
        <tr><td align=""center"" valign=""middle"" width=""56"" height=""56"" style=""width:56px;height:56px;background:#FFFFFF;border-radius:999px;box-shadow:0 4px 6px -1px rgba(17,17,17,0.1),0 2px 4px -2px rgba(17,17,17,0.1);border:2px solid rgba(17,17,17,0.1);"">
            <span style=""display:block;width:44px;height:44px;line-height:44px;text-align:center;font-family:{FontDisplay};font-size:26px;font-weight:700;color:{Accent};"">G</span>
        </td></tr>
        <tr><td align=""left"" style=""padding:10px 0 4px;""><span style=""font-family:{FontDisplay};font-size:24px;line-height:1;letter-spacing:-0.025em;color:{Ink};font-weight:700;"">Griot</span></td></tr>
        <tr><td align=""left"">
            <span style=""font-family:{FontSans};font-size:10px;letter-spacing:0.1em;text-transform:uppercase;color:{Accent};font-weight:600;line-height:1;display:inline-block;"">We make it happen</span>
            {Swervy(120)}
        </td></tr></table>
        </td></tr></table>";

    private static string BrandedFooter(string note, string siteUrl) => $@"
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin-top:32px;"">
        <tr><td style=""padding-top:24px;border-top:1px solid {Border};"">
            <p style=""margin:0 0 8px;font-family:{FontSans};font-size:12px;line-height:1.5;color:{Muted};"">{Esc(note)}</p>
            <p style=""margin:0;font-family:{FontSans};font-size:12px;line-height:1.5;color:{Muted};""><a href=""{Esc(siteUrl)}"" style=""color:{Accent};text-decoration:none;font-weight:700;"">{Esc(siteUrl.Replace("https://", "").Replace("http://", "").TrimEnd('/'))}</a> &nbsp;·&nbsp; Sababisha Solutions Limited</p>
        </td></tr></table>";

    private static string Shell(string preheader, string eyebrow, string title, string? accentWord, string bodyHtml, string footerNote, string siteUrl)
    {
        var titleHtml = BuildTitle(title, accentWord);
        return $@"<!DOCTYPE html>
        <html lang=""en""><head><meta charset=""utf-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/><meta name=""color-scheme"" content=""light""/><meta name=""supported-color-schemes"" content=""light""/><title>{Esc(title)}</title>
        <!--[if !mso]><!--><link rel=""preconnect"" href=""https://fonts.googleapis.com""/><link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin/><link href=""https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=Inter:wght@400;600;700&display=swap"" rel=""stylesheet""/><!--<![endif]-->
        </head>
        <body style=""margin:0;padding:0;background:{Bg};"">
        <div style=""display:none;max-height:0;overflow:hidden;opacity:0;mso-hide:all;"">{Esc(preheader)}</div>
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:{Bg};margin:0;padding:0;"">
        <tr><td align=""center"" style=""padding:32px 16px;"">
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:600px;width:100%;background:{Card};border-radius:28px;border:1px solid {Border};box-shadow:0 18px 40px rgba(17,17,17,0.06);overflow:hidden;"">
        <tr><td style=""padding:36px 32px 40px;font-family:{FontSans};color:{Ink};"">
            {BrandedHeader()}
            <p style=""margin:0 0 14px;font-family:{FontSans};font-size:11px;letter-spacing:0.16em;text-transform:uppercase;color:{Accent};font-weight:700;"">{Esc(eyebrow)}</p>
            <h1 style=""margin:0 0 28px;font-family:{FontDisplay};font-size:32px;line-height:1.15;font-weight:400;letter-spacing:-0.02em;"">{titleHtml}</h1>
            {bodyHtml}
            {BrandedFooter(footerNote, siteUrl)}
        </td></tr></table>
        <p style=""margin:20px 0 0;font-family:{FontSans};font-size:11px;color:{Muted};"">Fintech software · Enterprise systems · Digital transformation</p>
        </td></tr></table>
        </body></html>";
    }

    public static string OtpSubject(string purpose) => purpose switch
    {
        "login_2fa" => "Your Griot login code",
        "password_reset" => "Reset your Griot password",
        _ => "Verify your Griot email",
    };

    /// <summary>Purpose-customized OTP email — one branded shell, per-purpose copy.</summary>
    public static string RenderOtpEmail(string purpose, string code, string displayName, string siteUrl)
    {
        var intro = $"<p style=\"margin:0 0 22px;font-family:{FontSans};font-size:16px;line-height:1.6;color:{Muted};\">Hi {Esc(displayName)},</p>"; 
        switch (purpose)
        {
            case "login_2fa":
                return Shell(
                    $"Login code for {displayName}",
                    "Two-factor authentication",
                    "Your login code",
                    "login",
                    $"{intro}<p style=\"margin:0 0 22px;font-family:{FontSans};font-size:16px;line-height:1.6;color:{Muted};\">Enter the 6-digit code below to finish signing in. It expires in 10 minutes.</p>{CodeBox(code)}<p style=\"margin:20px 0 0;font-family:{FontSans};font-size:14px;color:{Muted};\">If you didn&rsquo;t try to sign in, ignore this email.</p>",
                    "This code authenticates a Griot account. Never share it.",
                    siteUrl);
            case "password_reset":
                return Shell(
                    $"Password reset for {displayName}",
                    "Password reset",
                    "Reset your password",
                    "reset",
                    $"{intro}<p style=\"margin:0 0 22px;font-family:{FontSans};font-size:16px;line-height:1.6;color:{Muted};\">Use the code below to reset your Griot password. It expires in 10 minutes.</p>{CodeBox(code)}<p style=\"margin:20px 0 0;font-family:{FontSans};font-size:14px;color:{Muted};\">If you didn&rsquo;t request this, ignore this email and your password will stay unchanged.</p>",
                    "This code can only reset a password. Never share it.",
                    siteUrl);
            default:
                return Shell(
                    $"Verify your email for {displayName}",
                    "Email verification",
                    "Verify your email",
                    "email",
                    $"{intro}<p style=\"margin:0 0 22px;font-family:{FontSans};font-size:16px;line-height:1.6;color:{Muted};\">Welcome to Griot! Enter the 6-digit code below to verify your email address and activate your account. It expires in 10 minutes.</p>{CodeBox(code)}<p style=\"margin:20px 0 0;font-family:{FontSans};font-size:14px;color:{Muted};\">If you didn&rsquo;t create a Griot account, ignore this email.</p>",
                    "This code verifies a Griot account email address. Never share it.",
                    siteUrl);
        }
    }

    /// <summary>Admin notification sent to <c>Resend:ContactToEmail</c> (CONTACT_TO_EMAIL( when a new account registers.</summary>
    public static string RenderNewAccountAdminEmail(string displayName, string email, string siteUrl)
    {
        var details = $@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:0 0 24px;""><tbody>
            {DetailRow("Name", Esc(displayName))}
            {DetailRow("Email", $@"<a href=""mailto:{Esc(email)}"" style=""color:{Accent};text-decoration:none;"">{Esc(email)}</a>", true)}
        </tbody></table>";
        return Shell(
            $"New account — {displayName}",
            "Account activity",
            "New user registered",
            "registered",
            $"{details}{SectionCard("Account", $@"<p style=""margin:0;font-size:18px;font-weight:700;"">{Esc(displayName)}</p><p style=""margin:10px 0 0;color:{Muted};font-size:14px;"">{Esc(email)}</p>")}",
            "This notification was sent because a new user registered on Griot.",
            siteUrl);
    }
}