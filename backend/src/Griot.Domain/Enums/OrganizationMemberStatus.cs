using System;

namespace Griot.Domain.Enums;

/// <summary>
/// Lifecycle state of a user's membership in one company (spec 29).
/// Distinct from the workspace-invite <see cref="InviteStatus"/> — org invites
/// resolve into this status on accept. Planned states are Invited/Active/Suspended.
/// </summary>
public enum OrganizationMemberStatus
{
    Invited,
    Active,
    Suspended
}