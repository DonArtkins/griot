namespace Griot.Domain.Enums;

/// <summary>
/// A user's role inside one company (spec 29 / 31).
/// Owner = the company Admin who can view ALL projects under that company only.
/// Use <see cref="Custom"/> with a <see cref="Entities.Role"/> row for
/// company-defined roles (created by Admin/PM from the fixed permission catalogue).
/// </summary>
public enum OrganizationRole
{
    Owner,
    Admin,
    ProjectManager,
    Member,
    Client,
    Custom
}
