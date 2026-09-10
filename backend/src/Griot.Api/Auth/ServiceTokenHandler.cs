using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Griot.Application.Interfaces.Repositories;
using Griot.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Griot.Api.Auth;

/// <summary>
/// Custom authentication handler for the GRIOT_SERVICE_TOKEN static bearer token
/// using the On-Behalf-Of (OBO) pattern. Converts a matching service token into a
/// ClaimsPrincipal that assumes the identity of a REAL USER resolved from the database,
/// restricted to the following scoped capabilities: ReadWorkspace, CreateTask,
/// AddComment, CreateNotification. Deletes, invites, and member management are NOT
/// in scope — enforced at the controller level by checking the "ai-on-behalf-of"
/// role marker or "auth_method=service_token_obo" claim. This handler is attached
/// to the "ServiceToken" auth scheme.
///
/// Required headers:
///   - Authorization: Bearer {GRIOT_SERVICE_TOKEN}
///   - X-On-Behalf-Of: {Guid} — real User.Id from the Users table
///
/// Token lookup order:
///   1. Configuration["ServiceToken:Key"]
///   2. env: GRIOT_SERVICE_TOKEN
///
/// The comparison is constant-time to prevent timing-oracle attacks.
/// User lookup is performed against the Users table via IGenericRepository{User}
/// using a per-call IServiceScope (authentication handlers are singleton-scoped).
/// </summary>
public sealed class ServiceTokenHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ServiceToken";
    public const string AiOnBehalfOfRole = "ai-on-behalf-of";

    public const string ScopeReadWorkspace        = "ReadWorkspace";
    public const string ScopeCreateTask           = "CreateTask";
    public const string ScopeAddComment           = "AddComment";
    public const string ScopeCreateNotification   = "CreateNotification";

    public const string OnBehalfOfHeaderName = "X-On-Behalf-Of";

    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public ServiceTokenHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        IConfiguration config,
        IServiceScopeFactory scopeFactory)
        : base(options, logger, encoder)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredToken = _config["ServiceToken:Key"]
                           ?? _config["GRIOT_SERVICE_TOKEN"];

        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            return AuthenticateResult.NoResult();
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString().Trim();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var providedToken = headerValue["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(providedToken))
            return AuthenticateResult.NoResult();

        var configuredBytes = Encoding.UTF8.GetBytes(configuredToken);
        var providedBytes   = Encoding.UTF8.GetBytes(providedToken);

        if (!CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes))
            return AuthenticateResult.Fail("Invalid service token.");

        if (!Request.Headers.TryGetValue(OnBehalfOfHeaderName, out var oboHeaderValues))
            return AuthenticateResult.Fail($"Missing required header: {OnBehalfOfHeaderName}");

        var oboValue = oboHeaderValues.ToString().Trim();
        if (!Guid.TryParse(oboValue, out var oboUserId))
            return AuthenticateResult.Fail($"{OnBehalfOfHeaderName} must be a valid GUID.");

        User? user;
        using (var scope = _scopeFactory.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IGenericRepository<User>>();
            user = await users.GetByIdAsync(oboUserId);
        }

        if (user is null)
            return AuthenticateResult.Fail($"On-Behalf-Of user not found: {oboUserId}");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, AiOnBehalfOfRole),
            new Claim("obo", user.Id.ToString()),
            new Claim("auth_method", "service_token_obo"),
            new Claim("scope", ScopeReadWorkspace),
            new Claim("scope", ScopeCreateTask),
            new Claim("scope", ScopeAddComment),
            new Claim("scope", ScopeCreateNotification),
        };

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        Logger.LogInformation(
            "AI service token authenticated — OBO principal issued for userId={UserId}",
            user.Id);
        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    /// Forward selector for the "MultiAuth" policy scheme (spec 09): routes each bearer token
    /// to the correct concrete handler. A JWT-shaped token (exactly two dots —
    /// header.payload.signature) goes to JwtBearer; any other bearer goes to the service-token
    /// scheme; a missing/empty header falls back to JwtBearer so 401 challenges keep their
    /// original shape. Kept as a pure static method so the routing rule is unit-testable.
    /// </summary>
    public static string SelectScheme(string authorizationHeader)
    {
        if (!string.IsNullOrWhiteSpace(authorizationHeader)
            && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorizationHeader["Bearer ".Length..].Trim();
            return token.Count(c => c == '.') == 2
                ? Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme
                : SchemeName;
        }

        return Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    }
}
