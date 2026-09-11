using System;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Request correlation surface (spec 20). The API layer implements this with
/// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>; the application
/// layer reads it without any HTTP dependency.
/// </summary>
public interface IRequestContext
{
    /// <summary>Normalized request id GUID of the current HTTP request (or null outside one).</summary>
    Guid? RequestId { get; }
}
