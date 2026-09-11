namespace Griot.Domain.Enums;

/// <summary>
/// Display/billing-metadata plan of a tenant company (spec 29).
/// No payments in this wave — plan is metadata only.
/// </summary>
public enum OrganizationPlan
{
    Free,
    Pro,
    Enterprise
}
