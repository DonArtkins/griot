using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.Interfaces.Repositories;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Griot.Application.Services;

namespace Griot.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRoleRepository"/> (spec 31). Request-flow
/// reads/writes ride the scoped, tenant-filtered <c>GriotDbContext</c> (strict
/// <c>Role</c>/<c>OrganizationMember</c> query filters from spec 29). The startup
/// backfill uses <c>IgnoreQueryFilters</c> because there is no request tenant scope
/// on the platform path — org targeting is explicit by parameter.
/// </summary>
public sealed class RoleRepository : IRoleRepository
{
    private readonly GriotDbContext _context;

    public RoleRepository(GriotDbContext context) => _context = context;

    public async Task ExecuteInTransactionAsync(Guid organizationId, Func<Task> action)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            // Lock the existing tenant row before checking names or role holders.
            // Every role mutation and seed takes the same lock, avoiding duplicate
            // names and partial seeds without changing the approved schema.
            var exists = await _context.Organizations
                .FromSqlInterpolated($"SELECT * FROM dbo.Organizations WITH (UPDLOCK, HOLDLOCK) WHERE Id = {organizationId}")
                .IgnoreQueryFilters().AsNoTracking().AnyAsync().ConfigureAwait(false);
            if (!exists)
                throw new DomainError(DomainErrorKind.NotFound, "Organization not found.");
            await action().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    public Task<Role?> FindSeedRoleByNameAsync(Guid organizationId, string name) =>
        _context.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name == name);

    public Task<int> CountUnrevokedFamiliesAsync(IReadOnlyList<Guid> userIds) =>
        _context.RefreshTokens.Where(t => userIds.Contains(t.UserId) && t.RevokedAt == null)
            .Select(t => new { t.UserId, t.FamilyId }).Distinct().CountAsync();

    // ── Tenant-scoped reads/writes ──

    public async Task<IReadOnlyList<Role>> ListForOrganizationAsync(Guid organizationId)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId)
            .OrderBy(r => r.Name)
            .ToListAsync()
            .ConfigureAwait(false);
        return roles;
    }

    public Task<Role?> FindByIdAsync(Guid roleId) =>
        _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

    public Task<Role?> FindByNameAsync(Guid organizationId, string name) =>
        _context.Roles.FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Name == name);

    public async Task<Role> AddAsync(Role role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return role;
    }

    public async Task UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(Role role)
    {
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrganizationMember>> ListActiveMembersForCustomRoleAsync(Guid roleId)
    {
        var members = await _context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.CustomRole)
            .Where(m => m.Role == OrganizationRole.Custom
                        && m.CustomRoleId == roleId
                        && m.Status == OrganizationMemberStatus.Active)
            .ToListAsync()
            .ConfigureAwait(false);
        return members;
    }

    public async Task<IReadOnlyList<Guid>> ListActiveMemberUserIdsForSystemRoleAsync(
        Guid organizationId, OrganizationRole role)
    {
        var ids = await _context.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId
                        && m.Role == role
                        && m.Status == OrganizationMemberStatus.Active)
            .Select(m => m.UserId)
            .ToListAsync()
            .ConfigureAwait(false);
        return ids;
    }

    public Task<OrganizationMember?> FindMemberByIdAsync(Guid organizationId, Guid memberId) =>
        _context.OrganizationMembers
            .Include(m => m.CustomRole)
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.Id == memberId);

    public async Task<IReadOnlyList<Guid>> DemoteCustomRoleMembersAsync(Guid organizationId, Guid roleId)
    {
        // Read the affected user ids FIRST (ExecuteUpdate cannot return rows), then
        // demote every Active member of this custom role back to the system Member role.
        var userIds = await _context.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId
                        && m.CustomRoleId == roleId
                        && m.Role == OrganizationRole.Custom)
            .Select(m => m.UserId)
            .ToListAsync()
            .ConfigureAwait(false);

        await _context.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId
                        && m.CustomRoleId == roleId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.Role, OrganizationRole.Member)
                .SetProperty(m => m.CustomRoleId, (Guid?)null))
            .ConfigureAwait(false);

        // Pending and expired invitations also reference Roles through NO ACTION
        // foreign keys. Preserve the invitation with the same Member fallback.
        await _context.OrganizationInvites
            .Where(i => i.OrganizationId == organizationId && i.CustomRoleId == roleId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Role, OrganizationRole.Member)
                .SetProperty(i => i.CustomRoleId, (Guid?)null)).ConfigureAwait(false);

        return userIds;
    }

    public async Task UpdateMemberRoleAsync(OrganizationMember member)
    {
        _context.OrganizationMembers.Update(member);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    // ── Platform paths (startup backfill; tenant filters bypassed) ──

    public async Task<IReadOnlyList<Guid>> ListActiveOrganizationIdsMissingSystemRolesAsync()
    {
        // Platform scope: no request tenant exists at startup, so the strict tenant
        // filters (which fail closed without a scope) must be bypassed explicitly.
        var orgIds = await _context.Organizations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Status == OrganizationStatus.Active
                        && _context.Roles
                            .IgnoreQueryFilters()
                            .Count(r => r.OrganizationId == o.Id && r.IsSystem) < 5)
            .Select(o => o.Id)
            .ToListAsync()
            .ConfigureAwait(false);
        return orgIds;
    }

    public Task<int> CountSystemRolesAsync(Guid organizationId) =>
        _context.Roles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(r => r.OrganizationId == organizationId && r.IsSystem);

    public async Task AddRangeAsync(IReadOnlyList<Role> roles)
    {
        _context.Roles.AddRange(roles);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
