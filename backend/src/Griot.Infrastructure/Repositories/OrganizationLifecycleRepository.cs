using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Griot.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrganizationLifecycleRepository"/> (spec 32).
/// Platform paths run with IgnoreQueryFilters + explicit organization targeting (the
/// SuperAdmin session has no tenant scope — spec-29 tenant filters fail closed without
/// one). Transactions mirror <see cref="RoleRepository"/>: lock the anchor row first
/// (unique Slug for onboarding, the organization row for transitions), run the mutation
/// + lifecycle + audit writes, then commit — a failure rolls back everything and clears
/// the change tracker (zero partial rows).
/// </summary>
public sealed class OrganizationLifecycleRepository : IOrganizationLifecycleRepository
{
    private readonly GriotDbContext _context;

    public OrganizationLifecycleRepository(GriotDbContext context) => _context = context;

    public async Task ExecuteOnboardingTransactionAsync(string slug, Func<Task> action)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            // Lock the unique Slug key range before insert: concurrent onboarding of
            // the same slug serializes here instead of racing into the unique index.
            var slugTaken = await _context.Organizations
                .FromSqlInterpolated($"SELECT * FROM dbo.Organizations WITH (UPDLOCK, HOLDLOCK) WHERE Slug = {slug}")
                .IgnoreQueryFilters().AsNoTracking().AnyAsync().ConfigureAwait(false);
            if (slugTaken)
                throw new DomainError(DomainErrorKind.Conflict, $"A company with slug '{slug}' already exists.");
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

    public async Task ExecuteInTransactionAsync(Guid organizationId, Func<Task> action) =>
        await ExecuteInTransactionAsync<object?>(organizationId, async () => { await action().ConfigureAwait(false); return null; }).ConfigureAwait(false);

    public async Task<T> ExecuteInTransactionAsync<T>(Guid organizationId, Func<Task<T>> action)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            // Lock the existing tenant row before checking status or membership holders.
            var exists = await _context.Organizations
                .FromSqlInterpolated($"SELECT * FROM dbo.Organizations WITH (UPDLOCK, HOLDLOCK) WHERE Id = {organizationId}")
                .IgnoreQueryFilters().AsNoTracking().AnyAsync().ConfigureAwait(false);
            if (!exists)
                throw new DomainError(DomainErrorKind.NotFound, "Organization not found.");
            var result = await action().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    public Task SaveAsync() => _context.SaveChangesAsync();

    // ── Platform probes (IgnoreQueryFilters + explicit targeting) ──

    public Task<bool> SlugExistsAsync(string slug) =>
        _context.Organizations.AsNoTracking().IgnoreQueryFilters().AnyAsync(o => o.Slug == slug);

    public Task<User?> FindUserByEmailAsync(string email) =>
        _context.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> FindUserByIdAsync(Guid userId) =>
        _context.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);

    public Task<Organization?> FindOrganizationByIdAsync(Guid organizationId) =>
        _context.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == organizationId);

    public Task<OrganizationMember?> FindMemberAsync(Guid organizationId, Guid userId) =>
        _context.OrganizationMembers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);

    public Task<OrganizationInvite?> FindActiveInviteByTokenAsync(string token) =>
        _context.OrganizationInvites.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Token == token
                && i.Status == OrganizationMemberStatus.Invited
                && i.AcceptedAt == null
                && i.ExpiresAt > DateTime.UtcNow);

    public async Task<(IReadOnlyList<Organization> Items, int Total)> ListOrganizationsAsync(
        int page, int pageSize, string? search)
    {
        var query = _context.Organizations.AsNoTracking().IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Name.Contains(search) || o.Slug.Contains(search));
        var total = await query.CountAsync().ConfigureAwait(false);
        var items = await query
            .OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync().ConfigureAwait(false);
        return (items, total);
    }

    // ── Writes (committed by SaveAsync inside the caller's transaction) ──

    public async Task AddOrganizationAsync(Organization organization)
    {
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddMemberAsync(OrganizationMember member)
    {
        _context.OrganizationMembers.Add(member);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddSystemRolesAsync(IReadOnlyList<Role> roles)
    {
        _context.Roles.AddRange(roles);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddWorkspaceAsync(Workspace workspace)
    {
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddInviteAsync(OrganizationInvite invite)
    {
        _context.OrganizationInvites.Add(invite);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddLifecycleEventAsync(OrganizationLifecycleEvent lifecycleEvent)
    {
        _context.OrganizationLifecycleEvents.Add(lifecycleEvent);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateOrganizationAsync(Organization organization)
    {
        _context.Organizations.Update(organization);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateMemberAsync(OrganizationMember member)
    {
        _context.OrganizationMembers.Update(member);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateInviteAsync(OrganizationInvite invite)
    {
        _context.OrganizationInvites.Update(invite);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
