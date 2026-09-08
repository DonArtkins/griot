using System.ComponentModel.DataAnnotations;

namespace Griot.Application.DTOs.Auth;

/// <summary>Body for <c>POST /api/auth/otp/request</c>.</summary>
public class OtpRequestRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    /// <summary>One of <c>email_verify</c>, <c>login_2fa</c>, <c>password_reset</c>.</summary>
    [Required]
    [MaxLength(50)]
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>Body for <c>POST /api/auth/otp/verify</c>.</summary>
public class OtpVerifyRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>Result of an OTP request. <see cref="Success"/>bool false means delivery failed (→ 502).</summary>
public class OtpRequestResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int ExpiresInMinutes { get; init; } = 10;
}

/// <summary>Result of an OTP verification attempt.</summary>
public class OtpVerifyResult
{
    /// <summary>True when the code matched and the challenge was consumed.</summary>
    public bool Verified { get; init; }

    public string? Message { get; init; }

    /// <summary>True when the purpose was <c>email_verify</c> and the user row was marked verified.</summary>
    public bool EmailVerified { get; init; }

    /// <summary>The challenge exceeded its attempt budget and was invalidated (caller maps to 429).</summary>
    public bool LockedOut { get; init; }
}