using System;
using System.Threading.Tasks;
using Griot.Application.DTOs;
using Griot.Application.DTOs.Organizations;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Spec 32 — company onboarding &amp; platform management. SuperAdmin-only surface
/// (fail closed on <see cref="Griot.Application.Tenancy.ITenantContext.IsSuperAdmin"/>):
/// onboard a company with a transactional seed (Organization + Owner membership +
/// five system roles + default Workspace + Onboarded lifecycle event + AuditLogs),
/// list/search companies, suspend/reactivate, transfer ownership, plan metadata,
/// and the company-owner invite-accept flow (Brevo-branded email, 7-day token).
/// Suspended companies keep reads + auth (spec 29/32); tenant writes fail 403
/// <c>org_suspended</c> via <c>TenantGuard</c> on the existing services.
/// </summary>
public interface IOrganizationLifecycleService
{
    /// <summary>Onboard a company (transactional seed + owner invite). 201 semantics.</summary>
    Task<CreateOrganizationResponse> CreateAsync(Guid callerId, CreateOrganizationRequest request);

    /// <summary>Paginated, searchable company list (SuperAdmin only).</summary>
    Task<PaginatedResult<OrganizationDto>> ListAsync(Guid callerId, int page, int pageSize, string? search);

    /// <summary>One company: SuperAdmin, or an Active Owner/Admin member of that company only.</summary>
    Task<OrganizationDto> GetByIdAsync(Guid callerId, Guid organizationId);

    /// <summary>Active → Suspended (writes in the tenant then fail 403 org_suspended).</summary>
    Task SuspendAsync(Guid callerId, Guid organizationId, SuspendRequest? request);

    /// <summary>Suspended → Active.</summary>
    Task ReactivateAsync(Guid callerId, Guid organizationId, SuspendRequest? request);

    /// <summary>Move company ownership to a different Active member (previous owner demotes to Admin).</summary>
    Task TransferOwnershipAsync(Guid callerId, Guid organizationId, TransferOwnershipRequest request);

    /// <summary>Update the plan metadata field.</summary>
    Task<OrganizationDto> UpdatePlanAsync(Guid callerId, Guid organizationId, UpdatePlanRequest request);

    /// <summary>
    /// Accept an organization invite by token (the invited, authenticated user;
    /// invite email must match the caller account). Activates the Owner/Admin membership.
    /// </summary>
    Task AcceptInviteAsync(string token, Guid callerId);
}
