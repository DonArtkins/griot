using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.Authorization;
using Griot.Application.DTOs.Roles;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Tenancy;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Griot.Application.Services;

/// <summary>
/// Spec 31 — RBAC v2 role engine. All authorization decisions re-resolve the
/// caller's active membership through <see cref="IPermissionService"/> (DB =
/// source of truth; the `perms` claim is only an optimization). Tenant scope
/// enforcement: reads/writes run inside the request's tenant-filtered context
/// AND the service asserts org/role/membership alignment (fail closed) before
/// every mutation, so even a mis-scoped context cannot cross companies.
/// </summary>
public sealed class RoleService : IRoleService
{
    /// <summary>
    /// The five canonical system roles seeded per company (spec 31 §3.1) — they mirror
    /// <see cref="OrganizationRole"/> (minus <c>Custom</c>). Their permission strings
    /// MUST agree with <see cref="PermissionCatalogue.PermsForSystemRole"/> so the seeded
    /// <c>Roles.Permissions</c> column and the spec-30 `perms` claim never diverge.
    /// </summary>
    internal static readonly IReadOnlyList<string> SystemRoleNames =
        new[] { "Owner", "Admin", "ProjectManager", "Member", "Client" };

    /// <summary>Comma-separated storage string for a system role's permission keys.</summary>
    internal static string SystemRolePermissionString(string name) => name switch
    {
        "Owner" or "Admin" =>
            PermissionCatalogue.Join(PermissionCatalogue.All).Replace(' ', ','),
        "ProjectManager" => PermissionCatalogue.Join(new[]
        {
            PermissionCatalogue.OrgRead, PermissionCatalogue.ProjectManage, PermissionCatalogue.TaskManage,
            PermissionCatalogue.CommentWrite, PermissionCatalogue.ClientManage,
            PermissionCatalogue.ClientFeedbackRead, PermissionCatalogue.ClientFeedbackRespond,
            PermissionCatalogue.ReportGenerate
        }).Replace(' ', ','),
        "Member" => string.Empty,
        "Client" => PermissionCatalogue.Join(new[]
        {
            PermissionCatalogue.OrgRead, PermissionCatalogue.ClientFeedbackRead
        }).Replace(' ', ','),
        _ => string.Empty
    };

    private readonly ITenantContext _tenant;
    private readonly IRoleRepository _roles;
    private readonly IAuthRepository _auth;
    private readonly IPermissionService _permissions;
    private readonly IAuditService _audit;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository roles,
        IAuthRepository auth,
        IPermissionService permissions,
        IAuditService audit,
        ILogger<RoleService> logger,
        ITenantContext tenant)
    {
        _tenant = tenant;
        _roles = roles;
        _auth = auth;
        _permissions = permissions;
        _audit = audit;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // System-role seeding (platform scope)
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<int> BackfillSystemRolesAsync()
    {
        var orgIds = await _roles.ListActiveOrganizationIdsMissingSystemRolesAsync().ConfigureAwait(false);
        var created = 0;
        foreach (var orgId in orgIds)
            created += await EnsureSystemRolesAsync(orgId).ConfigureAwait(false);
        return created;
    }

    /// <inheritdoc />
    public Task<int> EnsureSystemRolesAsync(Guid organizationId) =>
        InTransactionAsync(organizationId, () => EnsureSystemRolesCoreAsync(organizationId));

    private async Task<int> EnsureSystemRolesCoreAsync(Guid organizationId)
    {
        var existing = await _roles.CountSystemRolesAsync(organizationId).ConfigureAwait(false);
        if (existing >= SystemRoleNames.Count)
            return 0;

        // Re-probe per name so partial seeds converge instead of violating the
        // (OrganizationId, Name) unique index.
        var created = 0;
        foreach (var name in SystemRoleNames)
        {
            if (await _roles.FindSeedRoleByNameAsync(organizationId, name).ConfigureAwait(false) is Role existingRole)
            {
                if (!existingRole.IsSystem)
                    throw new DomainError(DomainErrorKind.Conflict, "A custom role uses a reserved system-role name.");
                continue;
            }

            await _roles.AddAsync(new Role
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = name,
                IsSystem = true,
                Permissions = SystemRolePermissionString(name)
            }).ConfigureAwait(false);
            created++;
        }

        if (created > 0)
            _logger.LogInformation(
                "Spec 31: seeded {Count} system roles for organization {OrganizationId}.",
                created, organizationId);
        return created;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reads
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleDto>> ListAsync(Guid callerId, Guid organizationId)
    {
        await RequireOrgAsync(callerId, organizationId, PermissionCatalogue.OrgRead, write: false).ConfigureAwait(false);

        var roles = await _roles.ListForOrganizationAsync(organizationId).ConfigureAwait(false);
        return roles.Select(ToDto).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Custom-role CRUD
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<RoleDto> CreateAsync(Guid callerId, Guid organizationId, CreateRoleRequest request) =>
        InTransactionAsync(organizationId, () => CreateCoreAsync(callerId, organizationId, request));

    private async Task<RoleDto> CreateCoreAsync(Guid callerId, Guid organizationId, CreateRoleRequest request)
    {
        var callerPerms = await RequireOrgAsync(callerId, organizationId, PermissionCatalogue.OrgRolesManage).ConfigureAwait(false);

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > 100)
            throw new DomainError(DomainErrorKind.Validation, "Role name must be 1–100 characters.");

        // Fixed catalogue + no-escalation: every requested key must be in the
        // catalogue AND within the creator's own effective permission set.
        RejectSystemName(name);
        var requested = ValidatePermissions(request.Permissions);
        var escalated = requested.Where(p => !callerPerms.Contains(p)).ToArray();
        if (escalated.Length > 0)
            throw new DomainError(DomainErrorKind.Forbidden,
                $"Cannot grant permissions the caller does not hold: {string.Join(", ", escalated)}.");

        if (await _roles.FindByNameAsync(organizationId, name).ConfigureAwait(false) is not null)
            throw new DomainError(DomainErrorKind.Conflict, $"A role named '{name}' already exists in this organization.");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            IsSystem = false,
            Permissions = WritePerms(requested)
        };
        await _roles.AddAsync(role).ConfigureAwait(false);

        await RecordAsync(callerId, "Role.Created", role.Id,
            new { role.Name, role.Permissions }, organizationId).ConfigureAwait(false);

        return ToDto(role);
    }

    /// <inheritdoc />
    public Task<RoleDto> UpdateAsync(Guid callerId, Guid organizationId, Guid roleId, UpdateRoleRequest request) =>
        InTransactionAsync(organizationId, () => UpdateCoreAsync(callerId, organizationId, roleId, request));

    private async Task<RoleDto> UpdateCoreAsync(Guid callerId, Guid organizationId, Guid roleId, UpdateRoleRequest request)
    {
        var callerPerms = await RequireOrgAsync(callerId, organizationId, PermissionCatalogue.OrgRolesManage).ConfigureAwait(false);

        var role = await RequireRoleAsync(organizationId, roleId).ConfigureAwait(false);
        if (role.IsSystem)
            throw new DomainError(DomainErrorKind.Forbidden, "System roles cannot be edited.");
        var before = new { role.Name, role.Permissions };

        if (request.Name is string newName)
        {
            newName = newName.Trim();
            if (newName.Length is 0 or > 100)
                throw new DomainError(DomainErrorKind.Validation, "Role name must be 1–100 characters.");
            if (!string.Equals(newName, role.Name, StringComparison.Ordinal)
                && await _roles.FindByNameAsync(organizationId, newName).ConfigureAwait(false) is not null)
                throw new DomainError(DomainErrorKind.Conflict, $"A role named '{newName}' already exists in this organization.");
            RejectSystemName(newName);
            role.Name = newName;
        }

        if (request.Permissions is IReadOnlyList<string> requestedList)
        {
            var requested = ValidatePermissions(requestedList);
            var escalated = requested.Where(p => !callerPerms.Contains(p)).ToArray();
            if (escalated.Length > 0)
                throw new DomainError(DomainErrorKind.Forbidden,
                    $"Cannot grant permissions the caller does not hold: {string.Join(", ", escalated)}.");
            role.Permissions = WritePerms(requested);
        }

        await _roles.UpdateAsync(role).ConfigureAwait(false);
        await RecordAsync(callerId, "Role.Updated", role.Id,
            new { role.Name, role.Permissions }, organizationId, before: before).ConfigureAwait(false);

        return ToDto(role);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid callerId, Guid organizationId, Guid roleId) =>
        _roles.ExecuteInTransactionAsync(organizationId, () => DeleteCoreAsync(callerId, organizationId, roleId));

    private async Task DeleteCoreAsync(Guid callerId, Guid organizationId, Guid roleId)
    {
        await RequireOrgAsync(callerId, organizationId, PermissionCatalogue.OrgRolesManage).ConfigureAwait(false);

        var role = await RequireRoleAsync(organizationId, roleId).ConfigureAwait(false);
        if (role.IsSystem)
            throw new DomainError(DomainErrorKind.Forbidden, "System roles cannot be deleted.");

        // User-approved cascade: affected members fall back to the SYSTEM Member
        // role, and their refresh families are revoked so the loss of the role's
        // permissions is immediately effective (no stale `perms` claim reuse).
        var demotedUsers = await _roles.DemoteCustomRoleMembersAsync(organizationId, roleId).ConfigureAwait(false);
        var revokedFamilies = await RevokeFamiliesAsync(demotedUsers).ConfigureAwait(false);

        await _roles.DeleteAsync(role).ConfigureAwait(false);

        await RecordAsync(callerId, "Role.Deleted", roleId,
            new { role.Name, role.IsSystem, AffectedUsers = demotedUsers.Distinct().Count(), FamiliesRevoked = revokedFamilies },
            organizationId).ConfigureAwait(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Force-revoke + member role assignment
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<ForceRevokeResult> ForceRevokeAsync(Guid callerId, Guid organizationId, Guid roleId) =>
        InTransactionAsync(organizationId, () => ForceRevokeCoreAsync(callerId, organizationId, roleId));

    private async Task<ForceRevokeResult> ForceRevokeCoreAsync(Guid callerId, Guid organizationId, Guid roleId)
    {
        await RequireOrgAsync(callerId, organizationId, PermissionCatalogue.OrgRolesManage).ConfigureAwait(false);

        var role = await RequireRoleAsync(organizationId, roleId).ConfigureAwait(false);

        List<Guid> userIds;
        if (role.IsSystem)
        {
            var orgRole = role.Name switch
            {
                "Owner" => OrganizationRole.Owner,
                "Admin" => OrganizationRole.Admin,
                "ProjectManager" => OrganizationRole.ProjectManager,
                "Client" => OrganizationRole.Client,
                _ => OrganizationRole.Member
            };
            var ids = await _roles
                .ListActiveMemberUserIdsForSystemRoleAsync(organizationId, orgRole)
                .ConfigureAwait(false);
            userIds = ids.ToList();
        }
        else
        {
            var members = await _roles.ListActiveMembersForCustomRoleAsync(roleId).ConfigureAwait(false);
            userIds = members.Select(m => m.UserId).ToList();
        }

        var families = await RevokeFamiliesAsync(userIds).ConfigureAwait(false);

        await RecordAsync(callerId, "Role.ForceRevoked", roleId,
            new { role.Name, role.IsSystem, AffectedUsers = userIds.Count, FamiliesRevoked = families },
            organizationId).ConfigureAwait(false);

        return new ForceRevokeResult(roleId, organizationId, userIds.Count, families);
    }

    /// <inheritdoc />
    public Task<MemberRoleResult> SetMemberRoleAsync(Guid callerId, Guid organizationId, Guid memberId, SetMemberRoleRequest request) =>
        InTransactionAsync(organizationId, () => SetMemberRoleCoreAsync(callerId, organizationId, memberId, request));

    private async Task<MemberRoleResult> SetMemberRoleCoreAsync(Guid callerId, Guid organizationId, Guid memberId, SetMemberRoleRequest request)
    {
        var caller = await RequireOrgAsync(callerId, organizationId, PermissionCatalogue.OrgMembersManage).ConfigureAwait(false);

        var member = await _roles.FindMemberByIdAsync(organizationId, memberId).ConfigureAwait(false)
                     ?? throw new DomainError(DomainErrorKind.NotFound, "Member not found in this organization.");

        if (member.UserId == callerId)
            throw new DomainError(DomainErrorKind.Forbidden, "You cannot change your own organization role.");

        if (member.OrganizationId != organizationId)
            throw new DomainError(DomainErrorKind.Forbidden, "cross-tenant");
        var before = new { member.UserId, member.Role, member.CustomRoleId };
        var callerRole = await _permissions.GetEffectiveRoleAsync(callerId, organizationId).ConfigureAwait(false);
        if (member.Role == OrganizationRole.Owner && !IsOwnerOrAbove(callerRole))
            throw new DomainError(DomainErrorKind.Forbidden, "Only an Owner can change an Owner's role.");

        var raw = (request.Role ?? string.Empty).Trim();

        // custom:{roleId} — the role must exist in THIS company (tenant guard).
        if (raw.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(raw["custom:".Length..], out var customRoleId))
                throw new DomainError(DomainErrorKind.Validation, "Malformed custom role reference (expected custom:{roleId}).");

            var customRole = await _roles.FindByIdAsync(customRoleId).ConfigureAwait(false)
                             ?? throw new DomainError(DomainErrorKind.NotFound, "Custom role not found.");
            if (customRole.OrganizationId != organizationId)
                throw new DomainError(DomainErrorKind.Forbidden, "cross-tenant");

            if (customRole.IsSystem)
                throw new DomainError(DomainErrorKind.Validation, "Use the system role name, not a custom reference.");
            if (ReadPerms(customRole.Permissions).Any(p => !caller.Contains(p)))
                throw new DomainError(DomainErrorKind.Forbidden, "Cannot assign permissions the caller does not hold.");

            member.Role = OrganizationRole.Custom;
            member.CustomRoleId = customRole.Id;
            member.CustomRole = customRole;
        }
        else
        {
            // "Custom" is never assignable by name — it is reached only through custom:{roleId}.
            if (!Enum.TryParse(raw, ignoreCase: true, out OrganizationRole role)
                || !Enum.IsDefined(role)
                || role == OrganizationRole.Custom)
                throw new DomainError(DomainErrorKind.Validation, $"Unknown organization role '{raw}'.");

            if (role == OrganizationRole.Owner && !IsOwnerOrAbove(callerRole))
                throw new DomainError(DomainErrorKind.Forbidden, "Only an Owner can assign the Owner role.");
            if (PermissionCatalogue.PermsForSystemRole(role)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(p => !caller.Contains(p)))
                throw new DomainError(DomainErrorKind.Forbidden, "Cannot assign permissions the caller does not hold.");

            member.Role = role;
            member.CustomRoleId = null;
            member.CustomRole = null;
        }

        await _roles.UpdateMemberRoleAsync(member).ConfigureAwait(false);

        await RecordAsync(callerId, "Member.RoleChanged", member.Id,
            new { member.UserId, member.Role, member.CustomRoleId }, organizationId,
            before: before, entityType: "OrganizationMember").ConfigureAwait(false);

        return new MemberRoleResult(member.Id, organizationId,
            member.CustomRoleId is Guid cid
                ? $"{RoleSelection.RoleCustomPrefix}{cid}"
                : RoleSelection.RoleName(member.Role),
            member.CustomRoleId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fail-closed org gate: the caller must be an Active member of the Active
    /// organization with the given permission; SuperAdmin passes with the full
    /// catalogue. Returns the caller's effective permission set (used for the
    /// no-escalation check on role CRUD).
    /// </summary>
    private async Task<IReadOnlySet<string>> RequireOrgAsync(Guid callerId, Guid organizationId, string permission, bool write = true)
    {
        if (_tenant.OrganizationId != organizationId)
            throw new DomainError(DomainErrorKind.Forbidden, "cross-tenant");
        if (write)
            TenantGuard.AssertTenant(_tenant, organizationId);
        if (organizationId == Guid.Empty)
            throw new DomainError(DomainErrorKind.Forbidden, "Organization scope is required.");
        if (!await _permissions.HasPermissionAsync(callerId, organizationId, permission).ConfigureAwait(false))
            throw new DomainError(DomainErrorKind.Forbidden, $"Missing required permission: {permission}.");
        return await _permissions.EffectivePermissionsAsync(callerId, organizationId).ConfigureAwait(false);
    }

    /// <summary>Loads the role inside the tenant (query filter) and asserts org alignment.</summary>
    private async Task<Role> RequireRoleAsync(Guid organizationId, Guid roleId)
    {
        var role = await _roles.FindByIdAsync(roleId).ConfigureAwait(false)
                   ?? throw new DomainError(DomainErrorKind.NotFound, "Role not found.");
        if (role.OrganizationId != organizationId)
            throw new DomainError(DomainErrorKind.Forbidden, "cross-tenant");
        return role;
    }

    /// <summary>Permission keys: must all be in the fixed catalogue (unknown → 400).</summary>
    private static List<string> ValidatePermissions(IReadOnlyList<string>? permissions)
    {
        if (permissions?.Any(p => p is null) == true)
            throw new DomainError(DomainErrorKind.Validation, "Permission keys cannot be null.");
        var requested = (permissions ?? Array.Empty<string>())
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var unknown = requested.Where(p => !PermissionCatalogue.All.Contains(p, StringComparer.Ordinal)).ToArray();
        if (unknown.Length > 0)
            throw new DomainError(DomainErrorKind.Validation,
                $"Unknown permission key(s): {string.Join(", ", unknown)}.");
        return requested;
    }

    private static bool IsOwnerOrAbove(string? role) =>
        role is RoleSelection.RoleSuperAdmin or RoleSelection.RoleOwner;

    /// <summary>Comma-separated storage → ordered distinct key list.</summary>
    private static IReadOnlyList<string> ReadPerms(string stored) =>
        stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Distinct(StringComparer.Ordinal)
              .OrderBy(k => k, StringComparer.Ordinal)
              .ToList();

    /// <summary>Ordered distinct key list → comma-separated storage (deduplicated).</summary>
    private static string WritePerms(IEnumerable<string> keys) =>
        string.Join(", ", keys.Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal));

    private static RoleDto ToDto(Role role) => new(
        role.Id, role.OrganizationId, role.Name, role.IsSystem,
        ReadPerms(role.Permissions).ToArray(), role.CreatedAt);

    private static void RejectSystemName(string name)
    {
        if (SystemRoleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new DomainError(DomainErrorKind.Conflict, "System role names are reserved.");
    }

    private async Task<T> InTransactionAsync<T>(Guid organizationId, Func<Task<T>> action)
    {
        T result = default!;
        await _roles.ExecuteInTransactionAsync(organizationId, async () =>
            result = await action().ConfigureAwait(false)).ConfigureAwait(false);
        return result;
    }

    private async Task<int> RevokeFamiliesAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToArray();
        var families = await _roles.CountUnrevokedFamiliesAsync(ids).ConfigureAwait(false);
        foreach (var userId in ids)
            await _auth.RevokeAllFamiliesAsync(userId, DateTime.UtcNow).ConfigureAwait(false);
        return families;
    }

    // RecordAsync shares the scoped DbContext and the surrounding role transaction.
    // A failed audit or revoke rolls the entire operation back.
    private Task RecordAsync(Guid actorId, string action, Guid entityId, object after,
        Guid organizationId, object? before = null, string entityType = "Role") =>
        _audit.RecordAsync(new AuditEntry(
            actorId, action, entityType, entityId,
            Before: AuditService.Snapshot(before),
            After: AuditService.Snapshot(after),
            OrganizationId: organizationId));
}
