using System.ComponentModel.DataAnnotations;

namespace Griot.Application.DTOs.Auth;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
