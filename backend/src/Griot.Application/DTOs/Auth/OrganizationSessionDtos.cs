using System;
using System.ComponentModel.DataAnnotations;

namespace Griot.Application.DTOs.Auth;

/// <summary>
/// One caller membership row (GET /api/auth/organizations — spec 30). Memberships
/// with a non-active status (Invited/Suspended) are never listed.
/// </summary>
public class OrganizationMembershipDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>Effective tenant role string (owner/admin/project_manager/member/client/custom:{roleId}).</summary>
    public string Role { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }

    /// <summary>True when this organization is the active `org` of the presented session.</summary>
    public bool IsActive { get; set; }
}

/// <summary>POST /api/auth/select-organization body (spec 30).</summary>
public class SelectOrganizationRequest
{
    /// <summary>The organization to make active (re-mints the token pair with the new `org` claim).</summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Optional refresh token of the session being switched. When presented, its rotation
    /// family is revoked AFTER the new pair is minted (single active session per family —
    /// the stale-org token can never be refreshed again). Omit it to keep both sessions.
    /// </summary>
    public string? RefreshToken { get; set; }
}

/// <summary>SuperAdmin bootstrap outcome (startup, spec 30). Idempotent by design.</summary>
public sealed record SuperAdminBootstrapResult(bool Created, bool Upgraded);