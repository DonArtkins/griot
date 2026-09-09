namespace Griot.Application.Services;

/// <summary>
/// Error signalling from application services to controllers. Message + kind map
/// cleanly to HTTP (400 validation / 403 forbidden / 404 not-found / 409 conflict).
/// </summary>
public sealed class DomainError : Exception
{
    public DomainErrorKind Kind { get; }

    public DomainError(DomainErrorKind kind, string message) : base(message)
    {
        Kind = kind;
    }

    public bool IsForbidden => Kind == DomainErrorKind.Forbidden;
    public bool IsNotFound => Kind == DomainErrorKind.NotFound;
    public bool IsConflict => Kind == DomainErrorKind.Conflict;
}

public enum DomainErrorKind
{
    Validation,
    Forbidden,
    NotFound,
    Conflict
}