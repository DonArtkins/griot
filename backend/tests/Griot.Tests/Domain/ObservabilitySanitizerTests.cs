using System;
using System.Linq;
using Griot.Application.Services;
using Xunit;

namespace Griot.Tests.Domain;

/// <summary>
/// Spec 20 acceptance: `ApiLogs.QueryString` redacts token/code/password keys and
/// preserves non-sensitive params; IP is masked in the stored row (/24 IPv4, /64 IPv6).
/// </summary>
public class ObservabilitySanitizerTests
{
    [Theory]
    [InlineData("token=abc", "token=[redacted]")]
    [InlineData("Token=abc", "Token=[redacted]")]          // case-insensitive
    [InlineData("PASSWORD=hunter2", "PASSWORD=[redacted]")]
    [InlineData("code=123456", "code=[redacted]")]          // OTP codes
    [InlineData("secret=xyz", "secret=[redacted]")]
    [InlineData("authorization=Bearer+jwt", "authorization=[redacted]")]
    [InlineData("reset=1", "reset=[redacted]")]             // spec-23 reset-token key
    public void RedactQueryString_RedactsSensitiveValues(string input, string expected)
        => Assert.Equal(expected, ApiLogSanitizer.RedactQueryString(input));

    [Fact]
    public void RedactQueryString_PreservesNonSensitiveParams()
        => Assert.Equal("page=2&workspaceId=abc-123", ApiLogSanitizer.RedactQueryString("page=2&workspaceId=abc-123"));

    [Fact]
    public void RedactQueryString_MixedParams_OnlySensitiveRedacted()
        => Assert.Equal("page=2&token=[redacted]&q=search", ApiLogSanitizer.RedactQueryString("page=2&token=abc&q=search"));

    [Fact]
    public void RedactQueryString_HandlesRepeatedKeys()
        => Assert.Equal("token=[redacted]&token=[redacted]&page=1", ApiLogSanitizer.RedactQueryString("token=a&token=b&page=1"));

    [Fact]
    public void RedactQueryString_HandlesUrlEncodedKeys()
        => Assert.Equal("toke%6E=[redacted]", ApiLogSanitizer.RedactQueryString("toke%6E=abc"));

    [Fact]
    public void RedactQueryString_TruncatesAtColumnCap()
    {
        var longQuery = "page=" + new string('a', 600);
        var result = ApiLogSanitizer.RedactQueryString(longQuery);
        Assert.True(result.Length <= ApiLogSanitizer.MaxQueryStringLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("?")]
    public void RedactQueryString_EmptyInput_ReturnsEmpty(string? input)
        => Assert.Equal(string.Empty, ApiLogSanitizer.RedactQueryString(input));

    [Theory]
    [InlineData("192.168.1.42", "192.168.1.0")]                     // /24
    [InlineData("::ffff:192.168.1.42", "192.168.1.0")]              // mapped IPv4
    [InlineData("[2001:db8:1111:2222:3333:4444:5555:6666]", "2001:db8:1111:2222::")] // /64, brackets
    public void MaskIp_MasksKnownFamilies(string input, string expected)
        => Assert.Equal(expected, ApiLogSanitizer.MaskIp(input));

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("1.2.3.999")]
    public void MaskIp_InvalidInput_StoresNull(string input)
        => Assert.Null(ApiLogSanitizer.MaskIp(input));
}
