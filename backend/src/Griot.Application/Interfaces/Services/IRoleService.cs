using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Griot.Application.DTOs.Roles;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Spec 31 — RBAC v2 role engine: system-role seeding (idempotent, five roles
/// per company, <c>IsSystem = true</c>, undeletable/uneditable), custom-role
/// CRUD composed from the fixed permission catalogue, member role assignment,
/// and the force-revoke endpoint that turns role changes into immediate effect
/// by revoking the affected users' refresh families. All decisions are
/// re-checked server-side through <see cref="IPermissionService"/>.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Startup backfill: ensures every Active organization has its five system
    /// roles (idempotent; platform scope, tenant filters bypassed). Returns the
    /// number of system roles created. Spec 32's onboarding reuses
    /// <see cref="EnsureSystemRolesAsync"/> per new company.
    /// </summary>
    Task<int> BackfillSystemRolesAsync();

    /// <summary>Ensures the five system roles exist for one organization (idempotent). Returns created count.</summary>
    Task<int> EnsureSystemRolesAsync(Guid organizationId);

    /// <summary>System + custom roles of the organization (org.read).</summary>
    Task<IReadOnlyList<RoleDto>> ListAsync(Guid callerId, Guid organizationId);

    /// <summary>
    /// Creates a custom role (org.roles.manage). Every permission key must be in
    /// the fixed catalogue and within the creator's own effective set (no
    /// escalation); unknown keys → 400, duplicate name → 409.
    /// </summary>
    Task<RoleDto> CreateAsync(Guid callerId, Guid organizationId, CreateRoleRequest request);

    /// <summary>Updates name/permissions of a CUSTOM role; system roles → 403.</summary>
    Task<RoleDto> UpdateAsync(Guid callerId, Guid organizationId, Guid roleId, UpdateRoleRequest request);

    /// <summary>
    /// Deletes a CUSTOM role; system roles → 403. Members still holding the role
    /// cascade back to the system Member role, and their refresh families are
    /// revoked so the change is immediately effective.
    /// </summary>
    Task DeleteAsync(Guid callerId, Guid organizationId, Guid roleId);

    /// <summary>
    /// Force-revoke (spec 31): revokes ALL refresh families of every active
    /// member holding the role, so a role change takes effect on the next
    /// login/refresh instead of at natural token expiry.
    /// </summary>
    Task<ForceRevokeResult> ForceRevokeAsync(Guid callerId, Guid organizationId, Guid roleId);

    /// <summary>
    /// Sets one member's organization role (org.members.manage): a system role
    /// name or <c>custom:{roleId}</c>. Self-change → 403; demoting an Owner
    /// requires an Owner; the custom role must belong to the same company.
    /// </summary>
    Task<MemberRoleResult> SetMemberRoleAsync(Guid callerId, Guid organizationId, Guid memberId, SetMemberRoleRequest request);
}