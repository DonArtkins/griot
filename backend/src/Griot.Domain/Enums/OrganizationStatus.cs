namespace Griot.Domain.Enums;

/// <summary>
/// Lifecycle state of a tenant company (spec 29 / 32 / 33).
/// Suspended orgs keep reads + auth but every write returns 403 org_suspended.
/// </summary>
public enum OrganizationStatus
{
    Active,
    Suspended,
    Offboarding,
    Archived
}
