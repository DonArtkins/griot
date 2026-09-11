using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Griot.Application.Services;

/// <summary>
/// Pure query-string redaction + IP masking helpers (spec 20, middleware-order bump §2):
/// sensitive query values are redacted by case-insensitive token/code/password/secret/
/// reset/authorization keys BEFORE 500-character truncation; URL-encoded and repeated
/// keys are handled. IPs are stored masked (/24 for IPv4, /64 for IPv6).
/// </summary>
public static class ApiLogSanitizer
{
    /// <summary>Query keys whose values are never stored.</summary>
    public static readonly string[] SensitiveKeys =
    {
        "token", "code", "password", "secret", "reset", "authorization"
    };

    /// <summary>Maximum stored QueryString length (mirrors the ApiLog.QueryString column cap).</summary>
    public const int MaxQueryStringLength = 500;

    private const string RedactedValue = "[redacted]";

    /// <summary>
    /// Redact sensitive values from a raw query string (leading '?' optional). Handles
    /// case-insensitive keys, URL-encoded keys, and repeated keys. Non-sensitive params
    /// are preserved verbatim.
    /// </summary>
    public static string RedactQueryString(string? queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return string.Empty;

        var raw = queryString.TrimStart('?');
        if (raw.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        var first = true;
        foreach (var pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!first) sb.Append('&');
            first = false;

            var separator = pair.IndexOf('=');
            if (separator < 0)
            {
                sb.Append(pair);
                continue;
            }

            // The STORED key keeps its original (possibly URL-encoded) text verbatim;
            // decoding is only for the sensitivity CHECK, so `toke%6E=abc` is detected
            // as a token key and stored as `toke%6E=[redacted]` (spec 20 bump §2).
            var rawKey = pair[..separator];
            if (IsSensitiveKey(Uri.UnescapeDataString(rawKey)))
                sb.Append(rawKey).Append('=').Append(RedactedValue);
            else
                sb.Append(pair);
        }

        var result = sb.ToString();
        return result.Length > MaxQueryStringLength
            ? result[..MaxQueryStringLength]
            : result;
    }

    /// <summary>True when the (URL-decoded) query key is in the sensitive vocabulary.</summary>
    public static bool IsSensitiveKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var normalized = key.Trim().ToLowerInvariant();
        return SensitiveKeys.Any(k => normalized == k);
    }

    /// <summary>
    /// Mask an IP for storage: IPv4 keeps the first three octets (a.b.c.0); IPv6 keeps
    /// the first four hextets (a:b:c:d::); anything unparseable is dropped (stored null).
    /// </summary>
    public static string? MaskIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;

        // Strip an IPv6-mapped IPv4 suffix and brackets.
        var value = ip.Trim().TrimStart('[').TrimEnd(']');
        if (value.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            value = value["::ffff:".Length..];

        var parts = value.Split('.');
        if (parts.Length == 4 && parts.All(p => byte.TryParse(p, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
            return $"{parts[0]}.{parts[1]}.{parts[2]}.0"; // /24

        var hextets = value.Split(':');
        if (hextets.Length >= 4 && hextets.Take(4).All(Hextet))
            return $"{hextets[0]}:{hextets[1]}:{hextets[2]}:{hextets[3]}::"; // /64

        return null;
    }

    private static bool Hextet(string p)
        => p.Length > 0 && p.Length <= 4 && p.All(c => Uri.IsHexDigit(c));
}
