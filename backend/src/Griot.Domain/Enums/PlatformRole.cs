namespace Griot.Domain.Enums;

/// <summary>
/// Platform-level role on <see cref="Entities.User"/> (spec 29).
/// SuperAdmin = the operator/owner of Griot itself; only SuperAdmins can
/// onboard, suspend or offboard companies. Resolved before any org role.
/// </summary>
public enum PlatformRole
{
    User,
    SuperAdmin
}
