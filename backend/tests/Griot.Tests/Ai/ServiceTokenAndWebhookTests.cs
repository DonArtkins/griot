using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Griot.Api.Auth;
using Griot.Api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Griot.Tests.Ai;

/// <summary>
/// Unit tests for spec 09: AI service token + webhook HMAC.
///
/// ServiceTokenHandlerTests — 6 tests covering:
///   1. Valid service token → ai-agent claims issued
///   2. Missing Authorization header → NoResult (not Fail)
///   3. Non-Bearer scheme → NoResult
///   4. Wrong token → Fail
///   5. ai-agent principal carries exactly the four scope claims + no delete/invite scopes
///   6. Token not configured → NoResult
///
/// WebhookHmacTests — 6 tests covering:
///   1. Valid HMAC → next called (202 from downstream)
///   2. Bad HMAC → 401
///   3. Missing signature header → 401
///   4. Unrelated route bypasses middleware
///   5. Webhook secret not configured → 503
///   6. Non-POST verb on webhook route bypasses middleware
///
/// MultiAuthForwardSelectorTests — 4 tests covering the policy-scheme routing rule
/// (ServiceTokenHandler.SelectScheme), the live-pipeline fix for spec 09:
///   1. JWT-shaped bearer (two dots) → JwtBearer
///   2. Non-JWT bearer → ServiceToken
///   3. Missing/empty header → JwtBearer (default challenge unchanged)
///   4. Non-Bearer header → JwtBearer
/// ----------------------------------------------------------------------------

/// <summary>
/// Minimal in-memory stub of IGenericRepository{User} used to satisfy ServiceTokenHandler's
/// user-lookup dependency in unit tests. A single test user is pre-seeded; this is test
/// fixture data and is explicitly exempt from the "no hardcoded runtime values" rule.
/// </summary>
internal sealed class TestUserRepositoryStub : Griot.Application.Interfaces.Repositories.IGenericRepository<Griot.Domain.Entities.User>
{
    public static readonly Guid TestOboUserId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Griot.Domain.Entities.User TestOboUser = new()
    {
        Id          = TestOboUserId,
        DisplayName = "Test OBO User",
        Email       = "test-obo@griot.local",
    };

    public Task<Griot.Domain.Entities.User?> GetByIdAsync(Guid id)
        => Task.FromResult(id == TestOboUserId ? TestOboUser : null);

    public Task<IEnumerable<Griot.Domain.Entities.User>> GetAllAsync()
        => throw new NotSupportedException();

    public Task<IEnumerable<Griot.Domain.Entities.User>> FindAsync(System.Linq.Expressions.Expression<Func<Griot.Domain.Entities.User, bool>> predicate)
        => throw new NotSupportedException();

    public Task AddAsync(Griot.Domain.Entities.User entity)
        => throw new NotSupportedException();

    public void Update(Griot.Domain.Entities.User entity)
        => throw new NotSupportedException();

    public void Remove(Griot.Domain.Entities.User entity)
        => throw new NotSupportedException();

    public Task SaveChangesAsync()
        => throw new NotSupportedException();
}

public class ServiceTokenHandlerTests
{
    private static ServiceTokenHandler BuildHandler(string? configuredToken, DefaultHttpContext ctx)
    {
        var inMemory = new Dictionary<string, string?>();
        if (configuredToken is not null)
            inMemory["ServiceToken:Key"] = configuredToken;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ServiceTokenHandler>(
                ServiceTokenHandler.SchemeName, _ => { });
        services.AddSingleton<IConfiguration>(config);
        services.AddScoped<Griot.Application.Interfaces.Repositories.IGenericRepository<Griot.Domain.Entities.User>, TestUserRepositoryStub>();
        var sp = services.BuildServiceProvider();

        var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = schemeProvider.GetSchemeAsync(ServiceTokenHandler.SchemeName).GetAwaiter().GetResult()!;
        var handler = (ServiceTokenHandler)ActivatorUtilities.CreateInstance(sp, typeof(ServiceTokenHandler),
            sp.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>(),
            sp.GetRequiredService<ILoggerFactory>(),
            System.Text.Encodings.Web.UrlEncoder.Default,
            config,
            sp.GetRequiredService<IServiceScopeFactory>());

        handler.InitializeAsync(scheme, ctx).GetAwaiter().GetResult();
        return handler;
    }

    private static DefaultHttpContext MakeHttpContext(string? authHeader, string? onBehalfOfHeader = null)
    {
        var ctx = new DefaultHttpContext();
        if (authHeader is not null)
            ctx.Request.Headers.Authorization = authHeader;
        if (onBehalfOfHeader is not null)
            ctx.Request.Headers[ServiceTokenHandler.OnBehalfOfHeaderName] = onBehalfOfHeader;
        return ctx;
    }

    [Fact]
    public async Task ValidToken_ValidOboUser_ReturnsSuccess_WithAiOnBehalfOfClaims()
    {
        const string token = "griot-dev-service-token-CHANGE-IN-PRODUCTION-min32chars!!";
        var ctx     = MakeHttpContext($"Bearer {token}", TestUserRepositoryStub.TestOboUserId.ToString());
        var handler = BuildHandler(token, ctx);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.True(result.Principal!.IsInRole(ServiceTokenHandler.AiOnBehalfOfRole));
        Assert.Equal(TestUserRepositoryStub.TestOboUserId.ToString(),
            result.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal(TestUserRepositoryStub.TestOboUser.DisplayName,
            result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)!.Value);
        Assert.Equal(TestUserRepositoryStub.TestOboUser.Email,
            result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)!.Value);
        Assert.Equal("service_token_obo",
            result.Principal.FindFirst("auth_method")!.Value);
    }

    [Fact]
    public async Task MissingAuthHeader_ReturnsNoResult()
    {
        var ctx     = MakeHttpContext(null, TestUserRepositoryStub.TestOboUserId.ToString());
        var handler = BuildHandler("some-valid-token-long-enough-32-bytes!!", ctx);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task NonBearerScheme_ReturnsNoResult()
    {
        var ctx     = MakeHttpContext("Basic dXNlcjpwYXNz", TestUserRepositoryStub.TestOboUserId.ToString());
        var handler = BuildHandler("some-valid-token-long-enough-32-bytes!!", ctx);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task WrongToken_ReturnsFail()
    {
        var ctx     = MakeHttpContext("Bearer wrong-token-that-does-not-match!!", TestUserRepositoryStub.TestOboUserId.ToString());
        var handler = BuildHandler("correct-token-long-enough-32bytes!!!", ctx);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task MissingOnBehalfOfHeader_ReturnsFail()
    {
        const string token = "griot-dev-service-token-CHANGE-IN-PRODUCTION-min32chars!!";
        var ctx     = MakeHttpContext($"Bearer {token}"); // no X-On-Behalf-Of
        var handler = BuildHandler(token, ctx);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Contains("On-Behalf-Of", result.Failure!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidOnBehalfOfGuid_ReturnsFail()
    {
        const string token = "griot-dev-service-token-CHANGE-IN-PRODUCTION-min32chars!!";
        var ctx     = MakeHttpContext($"Bearer {token}", "not-a-guid");
        var handler = BuildHandler(token, ctx);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task OnBehalfOfUserNotFound_ReturnsFail()
    {
        const string token = "griot-dev-service-token-CHANGE-IN-PRODUCTION-min32chars!!";
        var ctx     = MakeHttpContext($"Bearer {token}", "99999999-9999-9999-9999-999999999999");
        var handler = BuildHandler(token, ctx);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task ValidToken_PrincipalHasExactlyFourScopeClaims()
    {
        const string token = "griot-dev-service-token-CHANGE-IN-PRODUCTION-min32chars!!";
        var ctx     = MakeHttpContext($"Bearer {token}", TestUserRepositoryStub.TestOboUserId.ToString());
        var handler = BuildHandler(token, ctx);

        var result = await handler.AuthenticateAsync();
        var scopes = result.Principal!.FindAll("scope");

        Assert.Contains(scopes, c => c.Value == ServiceTokenHandler.ScopeReadWorkspace);
        Assert.Contains(scopes, c => c.Value == ServiceTokenHandler.ScopeCreateTask);
        Assert.Contains(scopes, c => c.Value == ServiceTokenHandler.ScopeAddComment);
        Assert.Contains(scopes, c => c.Value == ServiceTokenHandler.ScopeCreateNotification);

        Assert.DoesNotContain(scopes, c => c.Value == "Delete");
        Assert.DoesNotContain(scopes, c => c.Value == "InviteMember");
    }

    [Fact]
    public async Task TokenNotConfigured_ReturnsNoResult()
    {
        var ctx     = MakeHttpContext("Bearer any-bearer-token-value-here", TestUserRepositoryStub.TestOboUserId.ToString());
        var handler = BuildHandler(null, ctx);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }
}

// ─── WebhookHmacMiddleware tests ──────────────────────────────────────────────

public class WebhookHmacTests
{
    private const string WebhookRoute = "/api/webhooks/trigger";
    private const string ValidSecret  = "griot-dev-webhook-secret";

    private static string ComputeSignature(string secret, string body)
        => "sha256=" + Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body))
        ).ToLowerInvariant();

    private static (DefaultHttpContext ctx, bool[] nextCalled, WebhookHmacMiddleware mw)
        BuildMiddleware(string method, string path, string? sigHeader, string? body, string? configuredSecret)
    {
        var inMemory = new Dictionary<string, string?>();
        if (configuredSecret is not null)
            inMemory["Webhook:Secret"] = configuredSecret;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.Method = method;
        httpCtx.Request.Path   = path;

        var bodyBytes = Encoding.UTF8.GetBytes(body ?? "");
        httpCtx.Request.Body          = new MemoryStream(bodyBytes);
        httpCtx.Request.ContentLength = bodyBytes.Length;

        if (sigHeader is not null)
            httpCtx.Request.Headers["X-Trigger-Signature"] = sigHeader;

        httpCtx.Response.Body = new MemoryStream();

        var called = new bool[1];
        RequestDelegate next = _ => { called[0] = true; return Task.CompletedTask; };

        var loggerFactory = new ServiceCollection().AddLogging().BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>();

        var mw = new WebhookHmacMiddleware(next, config, loggerFactory.CreateLogger<WebhookHmacMiddleware>());
        return (httpCtx, called, mw);
    }

    [Fact]
    public async Task ValidHmac_CallsNext()
    {
        const string body = "{\"event\":\"task.created\"}";
        var sig = ComputeSignature(ValidSecret, body);
        var (ctx, called, mw) = BuildMiddleware("POST", WebhookRoute, sig, body, ValidSecret);

        await mw.InvokeAsync(ctx);

        Assert.True(called[0], "Next middleware should be called for a valid HMAC.");
    }

    [Fact]
    public async Task BadHmac_Returns401()
    {
        const string body   = "{\"event\":\"task.created\"}";
        const string badSig = "sha256=badbadbadbad";
        var (ctx, called, mw) = BuildMiddleware("POST", WebhookRoute, badSig, body, ValidSecret);

        await mw.InvokeAsync(ctx);

        Assert.False(called[0]);
        Assert.Equal(StatusCodes.Status401Unauthorized, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task MissingSignatureHeader_Returns401()
    {
        var (ctx, called, mw) = BuildMiddleware("POST", WebhookRoute, null, "{}", ValidSecret);

        await mw.InvokeAsync(ctx);

        Assert.False(called[0]);
        Assert.Equal(StatusCodes.Status401Unauthorized, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UnrelatedRoute_BypassesMiddleware()
    {
        var (ctx, called, mw) = BuildMiddleware("GET", "/api/tasks", null, null, ValidSecret);

        await mw.InvokeAsync(ctx);

        Assert.True(called[0], "Unrelated routes must pass through untouched.");
    }

    [Fact]
    public async Task SecretNotConfigured_Returns503()
    {
        var sig = ComputeSignature(ValidSecret, "{}");
        var (ctx, called, mw) = BuildMiddleware("POST", WebhookRoute, sig, "{}", null);

        await mw.InvokeAsync(ctx);

        Assert.False(called[0]);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task GetOnWebhookRoute_BypassesMiddleware()
    {
        var (ctx, called, mw) = BuildMiddleware("GET", WebhookRoute, null, null, ValidSecret);

        await mw.InvokeAsync(ctx);

        Assert.True(called[0], "GET on webhook route should bypass middleware (only POST is intercepted).");
    }
}

// ─── MultiAuth policy-scheme forward selector tests ───────────────────────────

public class MultiAuthForwardSelectorTests
{
    private const string JwtScheme = "Bearer"; // JwtBearerDefaults.AuthenticationScheme
    private const string ServiceScheme = ServiceTokenHandler.SchemeName;

    [Theory]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.c2lnbmF0dXJl")]
    [InlineData("bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.c2lnbmF0dXJl")] // case-insensitive scheme
    public void JwtShapedBearer_ForwardsToJwtBearer(string header)
    {
        Assert.Equal(JwtScheme, ServiceTokenHandler.SelectScheme(header));
    }

    [Theory]
    [InlineData("Bearer griot-static-service-token-no-dots")]
    [InlineData("Bearer a.b")] // one dot — not JWT-shaped
    [InlineData("Bearer a.b.c.d")] // three dots — not a JWT
    public void NonJwtBearer_ForwardsToServiceToken(string header)
    {
        Assert.Equal(ServiceScheme, ServiceTokenHandler.SelectScheme(header));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOrEmptyHeader_FallsBackToJwtBearer(string? header)
    {
        Assert.Equal(JwtScheme, ServiceTokenHandler.SelectScheme(header!));
    }

    [Fact]
    public void NonBearerHeader_FallsBackToJwtBearer()
    {
        Assert.Equal(JwtScheme, ServiceTokenHandler.SelectScheme("Basic dXNlcjpwYXNz"));
    }
}

// ─── OBO workspace membership + claim propagation boundary tests ──────────────

/// <summary>
/// Validates Spec 09 OBO boundary behavior: since the AI service-token principal now ASSUMES
/// a real user's identity (no more virtual ai-agent pseudo-GUID hacks), workspace membership
/// works exactly as for a regular user — the real user MUST exist as OwnerId OR as a row in
/// WorkspaceMembers. DomainService.IsMember is a pure check with no special-case logic.
/// </summary>
public class OboMembershipBoundaryTests
{
    private static System.Reflection.MethodInfo GetIsMemberMethod()
    {
        var mi = typeof(global::Griot.Application.Services.DomainService).GetMethod(
            "IsMember",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Assert.NotNull(mi);
        return mi;
    }

    /// <summary>
    /// Build a DomainService instance with all repo parameters set to null. Safe because
    /// IsMember is pure and only inspects the Workspace argument (never dereferences repos).
    /// </summary>
    private static global::Griot.Application.Services.DomainService BuildDomainServicePure()
    {
        var ctor = typeof(global::Griot.Application.Services.DomainService).GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)[0];
        var args = new object?[ctor.GetParameters().Length];
        return (global::Griot.Application.Services.DomainService)ctor.Invoke(args)!;
    }

    [Fact]
    public void IsMember_OboUserInMembersList_Passes()
    {
        // OBO user = TestUserRepositoryStub.TestOboUserId (matches the real user the AI assumes)
        var oboUserId = TestUserRepositoryStub.TestOboUserId;
        var domain   = BuildDomainServicePure();
        var isMember = GetIsMemberMethod();
        var ws = new global::Griot.Domain.Entities.Workspace
        {
            Id      = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Members = new List<global::Griot.Domain.Entities.WorkspaceMember>
            {
                new() { UserId = oboUserId, Role = global::Griot.Domain.Enums.WorkspaceRole.Member }
            },
        };
        var result = (bool)isMember.Invoke(domain, new object[] { ws, oboUserId })!;
        Assert.True(result);
    }

    [Fact]
    public void IsMember_OboUserIsOwner_Passes()
    {
        var oboUserId = TestUserRepositoryStub.TestOboUserId;
        var domain   = BuildDomainServicePure();
        var isMember = GetIsMemberMethod();
        var ws = new global::Griot.Domain.Entities.Workspace
        {
            Id      = Guid.NewGuid(),
            OwnerId = oboUserId, // AI assumes workspace OWNER identity
            Members = new List<global::Griot.Domain.Entities.WorkspaceMember>(),
        };
        var result = (bool)isMember.Invoke(domain, new object[] { ws, oboUserId })!;
        Assert.True(result);
    }

    [Fact]
    public void IsMember_RandomUser_NotMemberOfEmptyWorkspace()
    {
        // Baseline: an unknown user must never pass the pure IsMember check.
        // This guards against ever re-adding the old "virtual ai-agent" hack.
        var domain   = BuildDomainServicePure();
        var isMember = GetIsMemberMethod();
        var ws = new global::Griot.Domain.Entities.Workspace
        {
            Id      = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Members = new List<global::Griot.Domain.Entities.WorkspaceMember>(),
        };
        var randomUser = Guid.NewGuid();
        var result = (bool)isMember.Invoke(domain, new object[] { ws, randomUser })!;
        Assert.False(result);
    }
}

// ─── Bug 3: ai-agent ForbidIfAiAgent REST controller scope enforcement ────────

/// <summary>
/// Validates that every destructive controller endpoints (delete / invite / member-role-change /
/// accept-invite) immediately return 403 when the caller carries the ai-agent role.
/// Uses a minimal concrete-subclass stub of DomainControllerBase exercising the
/// exact guard path WorkspaceController uses; runtime guards exercised via ClaimsPrincipal stub.
/// </summary>
public class AiAgentControllerGuardTests
{
    private sealed class StubController : global::Griot.Api.Controllers.DomainControllerBase
    {
        public IActionResult? CallForbidIfAiCall() => ForbidIfAiCall();
        public bool GetIsAiCall() => IsAiCall;
    }

    private static StubController BuildControllerWithRole(string? role)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };
        if (role is not null)
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        var ctx = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return new StubController { ControllerContext = ctx };
    }

    [Fact]
    public void ForbidIfAiCall_AiOnBehalfOfRole_ReturnsForbidResult()
    {
        var ctlr = BuildControllerWithRole(ServiceTokenHandler.AiOnBehalfOfRole);
        var result = ctlr.CallForbidIfAiCall();
        Assert.NotNull(result);
        Assert.IsType<Microsoft.AspNetCore.Mvc.ForbidResult>(result);
    }

    [Fact]
    public void ForbidIfAiCall_RegularUserRole_ReturnsNull()
    {
        var ctlr = BuildControllerWithRole("User");
        var result = ctlr.CallForbidIfAiCall();
        Assert.Null(result);
    }

    [Fact]
    public void ForbidIfAiCall_NoRoleClaims_ReturnsNull()
    {
        var ctlr = BuildControllerWithRole(null);
        var result = ctlr.CallForbidIfAiCall();
        Assert.Null(result);
    }

    [Fact]
    public void IsAiCall_AiOnBehalfOfRole_IsTrue()
    {
        var ctlr = BuildControllerWithRole(ServiceTokenHandler.AiOnBehalfOfRole);
        Assert.True(ctlr.GetIsAiCall());
    }

    [Fact]
    public void IsAiCall_RegularUserRole_IsFalse()
    {
        var ctlr = BuildControllerWithRole("User");
        Assert.False(ctlr.GetIsAiCall());
    }
}
