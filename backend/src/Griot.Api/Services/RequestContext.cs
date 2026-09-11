using System;
using Griot.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace Griot.Api.Services;

/// <summary>
/// Spec 20 request correlation (pipeline §4): resolves the normalized request id
/// assigned by the request-id middleware in <c>Program.cs</c> (client
/// <c>X-Request-Id</c> when parseable, otherwise a generated GUID — stored in
/// <see cref="Microsoft.AspNetCore.Http.HttpContext.TraceIdentifier"/>) for
/// AuditLogs rows. Returns null outside an HTTP request or when the client
/// supplied a non-GUID request id (audit rows persist with a null RequestId).
/// </summary>
public sealed class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public RequestContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? RequestId =>
        Guid.TryParse(_accessor.HttpContext?.TraceIdentifier, out var id) ? id : null;
}
