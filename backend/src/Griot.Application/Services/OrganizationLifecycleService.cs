using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Griot.Application.Authorization;
using Griot.Application.DTOs;
using Griot.Application.DTOs.Organizations;
using Griot.Application.Email;
using Griot.Application.Helpers;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Tenancy;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Griot.Application.Services;

/// <summary>
/// Spec 32 — company onboarding &amp; platform management (SuperAdmin surface).
/// Onboarding seeds ONE SQL transaction: Organizations row + owner OrganizationMembers
/// row (Status Invited) + the five spec-31 system roles (shared statics with
/// <see cref="RoleService"/> so seeded permissions never diverge) + the default
/// Workspaces row + an Onboarded <see cref="OrganizationLifecycleEvent"/> + AuditLogs
/// rows — a failure leaves zero partial rows. The Brevo owner invite is best-effort
/// AFTER the durable commit (spec 12: delivery never blocks state). Suspend/reactivate/
/// transfer/plan transitions write their lifecycle event + audit in the same
/// transaction (spec 20 hard rule). Every platform mutation fails closed unless the
/// request principal is the SuperAdmin (<see cref="ITenantContext.IsSuperAdmin"/>).
/// </summary>
public sealed class OrganizationLifecycleService : IOrganizationLifecycleService
{
    private static readonly Regex SlugRegex = new("^[a-z0-9][a-z0-9-]{1,62}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);

    private readonly ITenantContext _tenant;
    private readonly IOrganizationLifecycleRepository _organizations;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrganizationLifecycleService> _logger;

    public OrganizationLifecycleService(
        IOrganizationLifecycleRepository organizations,
        IAuditService audit,
        IEmailService email,
        IConfiguration configuration,
        ILogger<OrganizationLifecycleService> logger,
        ITenantContext tenant)
    {
        _tenant = tenant;
        _organizations = organizations;
        _audit = audit;
        _email = email;
        _configuration = configuration;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Onboarding (spec 32 §1)
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<CreateOrganizationResponse> CreateAsync(Guid callerId, CreateOrganizationRequest request)
    {
        RequireSuperAdmin();

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > 100)
            throw new DomainError(DomainErrorKind.Validation, "Company name must be 1–100 characters.");

        var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (!SlugRegex.IsMatch(slug))
            throw new DomainError(DomainErrorKind.Validation,
                "Slug must be 2–63 characters of lowercase letters, digits and hyphens, starting with a letter or digit.");

        var ownerEmail = (request.OwnerEmail ?? string.Empty).Trim();
        if (!EmailRegex.IsMatch(ownerEmail))
            throw new DomainError(DomainErrorKind.Validation, "A valid ownerEmail is required.");

        // JsonStringEnumConverter accepts integers by default, so an undefined numeric
        // value (e.g. 999) would otherwise bind and persist — reject before any write.
        if (!Enum.IsDefined(request.Plan))
            throw new DomainError(DomainErrorKind.Validation, "A valid organization plan is required.");

        // Organization.OwnerId is a non-nullable FK (spec 29) — the owner must be a
        // registered Griot account before the company can be onboarded.
        var owner = await _organizations.FindUserByEmailAsync(ownerEmail).ConfigureAwait(false)
            ?? throw new DomainError(DomainErrorKind.Validation,
                "No registered Griot account exists for ownerEmail. The owner must register first, then the company can be onboarded.");

        var organization = new Organization
        {
            Name = name,
            Slug = slug,
            OwnerId = owner.Id,
            Status = OrganizationStatus.Active,
            PlanName = request.Plan
        };
        var invite = new OrganizationInvite
        {
            OrganizationId = organization.Id,
            Email = ownerEmail,
            Role = OrganizationRole.Owner,
            Token = NewInviteToken(),
            Status = OrganizationMemberStatus.Invited,
            InvitedById = callerId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        var member = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = owner.Id,
            Role = OrganizationRole.Owner,
            Status = OrganizationMemberStatus.Invited,
            JoinedAt = DateTime.UtcNow
        };
        var systemRoles = RoleService.SystemRoleNames.Select(systemRoleName => new Role
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = systemRoleName,
            IsSystem = true,
            Permissions = RoleService.SystemRolePermissionString(systemRoleName)
        }).ToList();
        var workspace = new Workspace
        {
            Name = "Default Workspace",
            Slug = $"{slug}-default", // Workspace.Slug is globally unique (spec 29 index)
            OwnerId = owner.Id,
            OrganizationId = organization.Id
        };
        var lifecycle = new OrganizationLifecycleEvent
        {
            OrganizationId = organization.Id,
            Kind = "Onboarded",
            ActorUserId = callerId,
            PayloadJson = JsonSerializer.Serialize(new { slug, ownerEmail, plan = request.Plan.ToString() })
        };

        await _organizations.ExecuteOnboardingTransactionAsync(slug, async () =>
        {
            await _organizations.AddOrganizationAsync(organization).ConfigureAwait(false);
            await _organizations.AddMemberAsync(member).ConfigureAwait(false);
            await _organizations.AddSystemRolesAsync(systemRoles).ConfigureAwait(false);
            await _organizations.AddWorkspaceAsync(workspace).ConfigureAwait(false);
            await _organizations.AddInviteAsync(invite).ConfigureAwait(false);
            await _organizations.AddLifecycleEventAsync(lifecycle).ConfigureAwait(false);
            _audit.QueueAudit(new AuditEntry(
                callerId, "Organization.Onboarded", "Organization", organization.Id,
                null, ToDto(organization).ToString(), OrganizationId: organization.Id));
            await _organizations.SaveAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        // Best-effort Brevo invite AFTER the durable seed commits (spec 12: a failed
        // delivery must never roll back state; the token also travels in the 201 body).
        await SendOwnerInviteEmailAsync(organization, owner, invite).ConfigureAwait(false);

        return new CreateOrganizationResponse(
            ToDto(organization),
            new OwnerInviteDto(invite.Id, invite.Email, invite.Role, invite.Token, invite.ExpiresAt));
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<OrganizationDto>> ListAsync(Guid callerId, int page, int pageSize, string? search)
    {
        RequireSuperAdmin();
        page = PaginationHelper.ValidatePageNumber(page);
        pageSize = PaginationHelper.ValidatePageSize(pageSize);
        var (items, total) = await _organizations.ListOrganizationsAsync(page, pageSize, search).ConfigureAwait(false);
        return new PaginatedResult<OrganizationDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<OrganizationDto> GetByIdAsync(Guid callerId, Guid organizationId)
    {
        var organization = await RequireOrganizationAsync(organizationId).ConfigureAwait(false);

        if (!_tenant.IsSuperAdmin)
        {
            // Company Admins (Active Owner/Admin membership) may view their own company only.
            var member = await _organizations.FindMemberAsync(organizationId, callerId).ConfigureAwait(false);
            var isCompanyAdmin = member is { Status: OrganizationMemberStatus.Active }
                && (member.Role is OrganizationRole.Owner or OrganizationRole.Admin);
            if (!isCompanyAdmin)
                throw new DomainError(DomainErrorKind.Forbidden,
                    "Platform management is SuperAdmin-only; company Admins may view their own company.");
        }

        return ToDto(organization);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle transitions (each = status flip + lifecycle event + audit, one transaction)
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task SuspendAsync(Guid callerId, Guid organizationId, SuspendRequest? request) =>
        _organizations.ExecuteInTransactionAsync(organizationId, () => SuspendCoreAsync(callerId, organizationId, request));

    private async Task SuspendCoreAsync(Guid callerId, Guid organizationId, SuspendRequest? request)
    {
        RequireSuperAdmin();
        var organization = await RequireOrganizationAsync(organizationId).ConfigureAwait(false);
        if (organization.Status != OrganizationStatus.Active)
            throw new DomainError(DomainErrorKind.Conflict,
                $"Only Active companies can be suspended (current status: {organization.Status}).");

        var before = organization.Status.ToString();
        organization.Status = OrganizationStatus.Suspended;
        organization.UpdatedAt = DateTime.UtcNow;
        await _organizations.UpdateOrganizationAsync(organization).ConfigureAwait(false);
        await RecordTransitionAsync(callerId, organization, "Suspended", before, request?.Reason).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task ReactivateAsync(Guid callerId, Guid organizationId, SuspendRequest? request) =>
        _organizations.ExecuteInTransactionAsync(organizationId, () => ReactivateCoreAsync(callerId, organizationId, request));

    private async Task ReactivateCoreAsync(Guid callerId, Guid organizationId, SuspendRequest? request)
    {
        RequireSuperAdmin();
        var organization = await RequireOrganizationAsync(organizationId).ConfigureAwait(false);
        if (organization.Status != OrganizationStatus.Suspended)
            throw new DomainError(DomainErrorKind.Conflict,
                $"Only Suspended companies can be reactivated (current status: {organization.Status}).");

        var before = organization.Status.ToString();
        organization.Status = OrganizationStatus.Active;
        organization.UpdatedAt = DateTime.UtcNow;
        await _organizations.UpdateOrganizationAsync(organization).ConfigureAwait(false);
        await RecordTransitionAsync(callerId, organization, "Reactivated", before, request?.Reason).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task TransferOwnershipAsync(Guid callerId, Guid organizationId, TransferOwnershipRequest request) =>
        _organizations.ExecuteInTransactionAsync(organizationId, () => TransferCoreAsync(callerId, organizationId, request));

    private async Task TransferCoreAsync(Guid callerId, Guid organizationId, TransferOwnershipRequest request)
    {
        RequireSuperAdmin();
        var organization = await RequireOrganizationAsync(organizationId).ConfigureAwait(false);
        if (request.NewOwnerUserId == Guid.Empty || request.NewOwnerUserId == organization.OwnerId)
            throw new DomainError(DomainErrorKind.Conflict, "The new owner must be a different member of this company.");

        var newOwnerMember = await _organizations.FindMemberAsync(organizationId, request.NewOwnerUserId).ConfigureAwait(false)
            ?? throw new DomainError(DomainErrorKind.NotFound, "The new owner is not a member of this company.");
        if (newOwnerMember.Status != OrganizationMemberStatus.Active)
            throw new DomainError(DomainErrorKind.Validation,
                "The new owner must hold an Active membership in this company.");

        var before = JsonSerializer.Serialize(new { organization.OwnerId });
        var previousOwnerId = organization.OwnerId;
        var previousOwnerMember = await _organizations.FindMemberAsync(organizationId, previousOwnerId).ConfigureAwait(false);

        organization.OwnerId = request.NewOwnerUserId;
        organization.UpdatedAt = DateTime.UtcNow;
        newOwnerMember.Role = OrganizationRole.Owner;
        newOwnerMember.CustomRoleId = null;
        await _organizations.UpdateOrganizationAsync(organization).ConfigureAwait(false);
        await _organizations.UpdateMemberAsync(newOwnerMember).ConfigureAwait(false);

        if (previousOwnerMember is not null && previousOwnerMember.Role == OrganizationRole.Owner)
        {
            previousOwnerMember.Role = OrganizationRole.Admin;
            await _organizations.UpdateMemberAsync(previousOwnerMember).ConfigureAwait(false);
        }

        await _organizations.AddLifecycleEventAsync(new OrganizationLifecycleEvent
        {
            OrganizationId = organizationId,
            Kind = "OwnershipTransferred",
            ActorUserId = callerId,
            PayloadJson = JsonSerializer.Serialize(new { from = previousOwnerId, to = request.NewOwnerUserId })
        }).ConfigureAwait(false);
        _audit.QueueAudit(new AuditEntry(
            callerId, "Organization.OwnershipTransferred", "Organization", organization.Id,
            before, JsonSerializer.Serialize(new { organization.OwnerId }), OrganizationId: organization.Id));
        await _organizations.SaveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<OrganizationDto> UpdatePlanAsync(Guid callerId, Guid organizationId, UpdatePlanRequest request) =>
        _organizations.ExecuteInTransactionAsync(organizationId, () => UpdatePlanCoreAsync(callerId, organizationId, request));

    private async Task<OrganizationDto> UpdatePlanCoreAsync(Guid callerId, Guid organizationId, UpdatePlanRequest request)
    {
        RequireSuperAdmin();
        // Same undefined-enum guard as onboarding: JsonStringEnumConverter accepts
        // integers, so reject undefined numeric values before persistence.
        if (!Enum.IsDefined(request.Plan))
            throw new DomainError(DomainErrorKind.Validation, "A valid organization plan is required.");
        var organization = await RequireOrganizationAsync(organizationId).ConfigureAwait(false);
        var before = organization.PlanName.ToString();

        organization.PlanName = request.Plan;
        organization.UpdatedAt = DateTime.UtcNow;
        await _organizations.UpdateOrganizationAsync(organization).ConfigureAwait(false);

        await _organizations.AddLifecycleEventAsync(new OrganizationLifecycleEvent
        {
            OrganizationId = organizationId,
            Kind = "PlanChanged",
            ActorUserId = callerId,
            PayloadJson = JsonSerializer.Serialize(new { from = before, to = request.Plan.ToString() })
        }).ConfigureAwait(false);
        _audit.QueueAudit(new AuditEntry(
            callerId, "Organization.PlanChanged", "Organization", organization.Id,
            before, request.Plan.ToString(), OrganizationId: organization.Id));
        await _organizations.SaveAsync().ConfigureAwait(false);

        return ToDto(organization);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Invite accept (spec 32 §2 — resolves the onboarding invite into the Owner membership)
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task AcceptInviteAsync(string token, Guid callerId)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainError(DomainErrorKind.Validation, "An invite token is required.");
        if (callerId == Guid.Empty)
            throw new DomainError(DomainErrorKind.Forbidden, "Authentication is required to accept an organization invite.");

        var invite = await _organizations.FindActiveInviteByTokenAsync(token.Trim()).ConfigureAwait(false)
            ?? throw new DomainError(DomainErrorKind.NotFound, "Invite not valid or expired.");

        var user = await _organizations.FindUserByIdAsync(callerId).ConfigureAwait(false)
            ?? throw new DomainError(DomainErrorKind.Forbidden, "Caller identity could not be resolved.");
        if (!string.Equals(user.Email, invite.Email, StringComparison.OrdinalIgnoreCase))
            throw new DomainError(DomainErrorKind.Forbidden,
                "This invite was issued for a different email address. Sign in with the invited account.");

        await _organizations.ExecuteInTransactionAsync(invite.OrganizationId, async () =>
        {
            // Re-check under the organization row lock: a concurrent accept may have
            // consumed the token between the probe and the transaction.
            var live = await _organizations.FindActiveInviteByTokenAsync(invite.Token).ConfigureAwait(false)
                ?? throw new DomainError(DomainErrorKind.Conflict, "Invite not valid or already used.");

            var member = await _organizations.FindMemberAsync(invite.OrganizationId, callerId).ConfigureAwait(false);
            if (member is null)
            {
                member = new OrganizationMember
                {
                    OrganizationId = invite.OrganizationId,
                    UserId = callerId,
                    Role = live.Role,
                    CustomRoleId = live.CustomRoleId,
                    Status = OrganizationMemberStatus.Active,
                    JoinedAt = DateTime.UtcNow
                };
                await _organizations.AddMemberAsync(member).ConfigureAwait(false);
            }
            else
            {
                member.Role = live.Role;
                member.CustomRoleId = live.CustomRoleId;
                member.Status = OrganizationMemberStatus.Active;
                await _organizations.UpdateMemberAsync(member).ConfigureAwait(false);
            }

            live.Status = OrganizationMemberStatus.Active;
            live.AcceptedAt = DateTime.UtcNow;
            await _organizations.UpdateInviteAsync(live).ConfigureAwait(false);

            _audit.QueueAudit(new AuditEntry(
                callerId, "Organization.InviteAccepted", "OrganizationInvite", live.Id,
                null, JsonSerializer.Serialize(new { live.OrganizationId, role = live.Role.ToString() }),
                OrganizationId: live.OrganizationId));
            await _organizations.SaveAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<Organization> RequireOrganizationAsync(Guid organizationId) =>
        await _organizations.FindOrganizationByIdAsync(organizationId).ConfigureAwait(false)
        ?? throw new DomainError(DomainErrorKind.NotFound, "Organization not found.");

    /// <summary>One lifecycle row + one audit row for a status/ownership transition, then the commit.</summary>
    private async Task RecordTransitionAsync(
        Guid callerId, Organization organization, string kind, string? before, string? reason)
    {
        await _organizations.AddLifecycleEventAsync(new OrganizationLifecycleEvent
        {
            OrganizationId = organization.Id,
            Kind = kind,
            ActorUserId = callerId,
            PayloadJson = JsonSerializer.Serialize(new { from = before, to = organization.Status.ToString(), reason })
        }).ConfigureAwait(false);
        // Audit After carries the resulting status AND the suspension/reactivation
        // reason (SuspendRequest contract: the reason lands on the lifecycle event
        // payload and the AuditLogs row); the lifecycle event keeps its own payload.
        _audit.QueueAudit(new AuditEntry(
            callerId, $"Organization.{kind}", "Organization", organization.Id,
            before,
            JsonSerializer.Serialize(new { status = organization.Status.ToString(), reason }),
            OrganizationId: organization.Id));
        await _organizations.SaveAsync().ConfigureAwait(false);
    }

    private async Task SendOwnerInviteEmailAsync(Organization organization, User owner, OrganizationInvite invite)
    {
        var displayName = string.IsNullOrWhiteSpace(owner.DisplayName) ? invite.Email : owner.DisplayName;
        var html = BrandedEmailTemplate.RenderOrganizationInviteEmail(
            organization.Name, displayName, RoleSelection.RoleName(invite.Role), invite.Token, GetSiteUrl());
        var delivered = await _email.SendAsync(
            new EmailMessage(invite.Email, BrandedEmailTemplate.OrganizationInviteSubject(organization.Name), html))
            .ConfigureAwait(false);
        if (!delivered)
            _logger.LogWarning("Organization invite email delivery failed for {Email} (organization {OrganizationId})",
                invite.Email, organization.Id);
    }

    /// <summary>Same storage convention as workspace invites (spec 13): two-Guid hex token, plaintext.</summary>
    private static string NewInviteToken() =>
        Guid.NewGuid().ToString().Replace("-", string.Empty) + Guid.NewGuid().ToString("N");

    private static OrganizationDto ToDto(Organization organization) => new(
        organization.Id,
        organization.Name,
        organization.Slug,
        organization.OwnerId,
        organization.Status.ToString(),
        organization.PlanName.ToString(),
        organization.CreatedAt,
        organization.OffboardedAt);

    private string GetSiteUrl() => (_configuration["SITE_URL"] ?? "https://griot.app").TrimEnd('/');

    /// <summary>Fail closed: platform management requires the SuperAdmin principal (spec 29/32).</summary>
    private void RequireSuperAdmin()
    {
        if (!_tenant.IsSuperAdmin)
            throw new DomainError(DomainErrorKind.Forbidden, "SuperAdmin authority is required for platform management.");
    }
}
