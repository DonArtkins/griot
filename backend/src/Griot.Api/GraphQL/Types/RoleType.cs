using System;
using Griot.Domain.Enums;

namespace Griot.Api.GraphQL.Types;

/// <summary>
/// Spec 31: GraphQL projection of one role row (system or custom) of a company.
/// REST remains the mutation surface; GraphQL exposes the read model for
/// workspace/org dashboards.
/// </summary>
public class RoleGraphQLType
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }

    /// <summary>Permission keys of the role (system roles carry their catalogue grants; Member = empty).</summary>
    public string[] Permissions { get; set; } = Array.Empty<string>();

    public DateTime CreatedAt { get; set; }
}

/// <summary>GraphQL view of one member's effective role (spec 31).</summary>
public class MemberRoleGraphQLType
{
    public Guid MemberId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>"owner"/"admin"/"project_manager"/"member"/"client" or "custom:{roleId}".</summary>
    public string Role { get; set; } = string.Empty;
    public Guid? CustomRoleId { get; set; }
}