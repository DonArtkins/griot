using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Griot.Domain.Entities;
using Griot.Domain.Enums;

namespace Griot.Application.Interfaces.Repositories;

/// <summary>
/// Spec 31 role persistence surface (EF Core via the scoped, tenant-filtered
/// <c>GriotDbContext</c>). Platform paths (startup backfill) use the
/// <c>*IgnoreFilters</c> members because tenant query filters fail closed when
/// no request scope exists — the startup scope has none.
/// </summary>
public interface IRoleRepository
{
    // ── Tenant-scoped reads/writes (normal request flow) ──

    /// <summary>All role rows of one organization (system + custom), name-ordered.</summary>
    Task<IReadOnlyList<Role>> ListForOrganizationAsync(Guid organizationId);

    /// <summary>One role by id, or null when unknown/outside the active tenant.</summary>
    Task<Role?> FindByIdAsync(Guid roleId);

    /// <summary>First role with the given name in the organization, or null (duplicate-name check).</summary>
    Task<Role?> FindByNameAsync(Guid organizationId, string name);

    /// <summary>Persist a new role row.</summary>
    Task<Role> AddAsync(Role role);

    /// <summary>Persist edits to a role row.</summary>
    Task UpdateAsync(Role role);

    /// <summary>Remove a (custom) role row.</summary>
    Task DeleteAsync(Role role);

    /// <summary>
    /// Active members holding the given CUSTOM role (for force-revoke + delete cascade).
    /// Returns the member entities (user ids included) with the CustomRole loaded.
    /// </summary>
    Task<IReadOnlyList<OrganizationMember>> ListActiveMembersForCustomRoleAsync(Guid roleId);

    /// <summary>Active member ids holding the given SYSTEM role in the organization (force-revoke).</summary>
    Task<IReadOnlyList<Guid>> ListActiveMemberUserIdsForSystemRoleAsync(Guid organizationId, OrganizationRole role);

    /// <summary>
    /// One membership by id inside the organization (any status; service filters), or null.
    /// The CustomRole nav is loaded when present.
    /// </summary>
    Task<OrganizationMember?> FindMemberByIdAsync(Guid organizationId, Guid memberId);

    /// <summary>Move every member holding the custom role back to the system Member role (delete cascade).</summary>
    /// <returns>The user ids of the demoted members (for refresh-family revocation).</returns>
    Task<IReadOnlyList<Guid>> DemoteCustomRoleMembersAsync(Guid organizationId, Guid roleId);

    /// <summary>Set one member's role (system enum or Custom + CustomRoleId).</summary>
    Task UpdateMemberRoleAsync(OrganizationMember member);

    // ── Platform paths (startup backfill; tenant filters bypassed) ──

    /// <summary>Ids of Active organizations that do NOT yet have their five seeded system roles.</summary>
    Task<IReadOnlyList<Guid>> ListActiveOrganizationIdsMissingSystemRolesAsync();

    /// <summary>How many system-role rows exist for the organization (idempotency probe).</summary>
    Task<int> CountSystemRolesAsync(Guid organizationId);

    /// <summary>Persist several role rows in one save (startup backfill / onboarding).</summary>
    Task AddRangeAsync(IReadOnlyList<Role> roles);
}