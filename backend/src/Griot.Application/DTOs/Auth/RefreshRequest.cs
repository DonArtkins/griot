using System.ComponentModel.DataAnnotations;

namespace Griot.Application.DTOs.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Spec 30 (stateless tenant session): optional pin of the active organization.
    /// Re-sent by the client on every refresh so the re-issued access token keeps the
    /// same `org`/`role`/`perms`. Absent: a single active membership is auto-picked,
    /// otherwise the platform view applies (client re-selects via select-organization).
    /// </summary>
    public Guid? OrganizationId { get; set; }
}
