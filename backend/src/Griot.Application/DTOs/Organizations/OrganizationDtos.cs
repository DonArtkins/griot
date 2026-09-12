using System;
using Griot.Domain.Enums;

namespace Griot.Application.DTOs.Organizations;

/// <summary>
/// Spec 32 — request body for <c>POST /api/organizations</c> (SuperAdmin onboarding).
/// The owner must already be a registered Griot account (<c>Organization.OwnerId</c>
/// is a non-nullable FK); the company-owner membership activates on invite accept.
/// </summary>
public sealed record CreateOrganizationRequest(
    string Name,
    string Slug,
    string OwnerEmail,
    string? OwnerDisplayName = null,
    OrganizationPlan Plan = OrganizationPlan.Free);

/// <summary>Spec 32 — company representation returned by the platform surface.</summary>
public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    Guid OwnerId,
    string Status,
    string Plan,
    DateTime CreatedAt,
    DateTime? OffboardedAt);

/// <summary>Spec 32 — the onboarding owner invite returned to the SuperAdmin with the 201.</summary>
public sealed record OwnerInviteDto(
    Guid InviteId,
    string Email,
    OrganizationRole Role,
    string Token,
    DateTime ExpiresAt);

/// <summary>Spec 32 — 201 body of <c>POST /api/organizations</c>: seeded company + owner invite.</summary>
public sealed record CreateOrganizationResponse(OrganizationDto Organization, OwnerInviteDto? OwnerInvite);

/// <summary>
/// Spec 32 — optional body for <c>POST /api/organizations/{id}/suspend</c> and
/// <c>/reactivate</c>. The reason (when present) is stored on the lifecycle event
/// payload and the AuditLogs row.
/// </summary>
public sealed record SuspendRequest(string? Reason = null);

/// <summary>
/// Spec 32 — body for <c>POST /api/organizations/{id}/transfer-ownership</c>.
/// The new owner must be a different, Active member of the company; the previous
/// owner's membership demotes to Admin. Claims re-resolve on the next refresh
/// (spec 30 session resolution) — no forced revocation in this spec.
/// </summary>
public sealed record TransferOwnershipRequest(Guid NewOwnerUserId);

/// <summary>
/// Spec 32 — body for <c>PUT /api/organizations/{id}/plan</c>. Plan is
/// display/billing metadata only (spec 29 — no payments in this wave).
/// </summary>
public sealed record UpdatePlanRequest(OrganizationPlan Plan);
