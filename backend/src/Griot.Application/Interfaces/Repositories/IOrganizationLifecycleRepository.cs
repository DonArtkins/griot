using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Griot.Domain.Entities;
using Griot.Domain.Enums;

namespace Griot.Application.Interfaces.Repositories;

/// <summary>
/// Spec 32 — platform persistence for company onboarding/lifecycle. Unlike the
/// tenant-scoped repositories these paths run with <c>IgnoreQueryFilters</c> plus
/// explicit organization targeting (the SuperAdmin session has no tenant scope and
/// the strict spec-29 tenant filters fail closed without one). Every mutation
/// commits inside the same SQL transaction as its
/// <see cref="OrganizationLifecycleEvent"/> and AuditLogs rows.
/// </summary>
public interface IOrganizationLifecycleRepository
{
    /// <summary>Serialize company creation on the unique Slug key (UPDLOCK/HOLDLOCK probe) and commit the full seed atomically.</summary>
    Task ExecuteOnboardingTransactionAsync(string slug, Func<Task> action);

    /// <summary>Serialize lifecycle transitions on an existing organization row (RoleRepository lock pattern).</summary>
    Task ExecuteInTransactionAsync(Guid organizationId, Func<Task> action);

    /// <summary>Same lock pattern, propagating the core method's result (plan update returns the edited company).</summary>
    Task<T> ExecuteInTransactionAsync<T>(Guid organizationId, Func<Task<T>> action);

    /// <summary>Single SaveChanges commit point for queued inserts + audit rows inside the caller's transaction.</summary>
    Task SaveAsync();

    // ── Platform probes (IgnoreQueryFilters + explicit targeting) ──

    Task<bool> SlugExistsAsync(string slug);
    Task<User?> FindUserByEmailAsync(string email);
    Task<User?> FindUserByIdAsync(Guid userId);
    Task<Organization?> FindOrganizationByIdAsync(Guid organizationId);
    Task<OrganizationMember?> FindMemberAsync(Guid organizationId, Guid userId);

    /// <summary>Unaccepted, unexpired invite lookup by token (plaintext storage, same convention as workspace invites — spec 13).</summary>
    Task<OrganizationInvite?> FindActiveInviteByTokenAsync(string token);

    // ── Platform queries ──

    /// <summary>Paginated + name/slug-searchable company list (SuperAdmin surface).</summary>
    Task<(IReadOnlyList<Organization> Items, int Total)> ListOrganizationsAsync(int page, int pageSize, string? search);

    // ── Writes (queued; committed by SaveAsync inside the caller's transaction) ──

    Task AddOrganizationAsync(Organization organization);
    Task AddMemberAsync(OrganizationMember member);
    Task AddSystemRolesAsync(IReadOnlyList<Role> roles);
    Task AddWorkspaceAsync(Workspace workspace);
    Task AddInviteAsync(OrganizationInvite invite);
    Task AddLifecycleEventAsync(OrganizationLifecycleEvent lifecycleEvent);
    Task UpdateOrganizationAsync(Organization organization);
    Task UpdateMemberAsync(OrganizationMember member);
    Task UpdateInviteAsync(OrganizationInvite invite);
}
