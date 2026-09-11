using System.Text.Json.Serialization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Griot.Api.Auth;
using Griot.Api.GraphQL;
using Griot.Api.GraphQL.DataLoaders;
using Griot.Api.Middleware;
using Griot.Api.Services;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Infrastructure.Integrations;
using Griot.Infrastructure.Persistence;
using Griot.Infrastructure.Redis;
using Griot.Infrastructure.Email;
using Griot.Infrastructure.Repositories;
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Local dev secrets (DB connection string, JWT key) live in appsettings.Local.json — git-ignored.
// The design-time factory reads the same file; runtime now loads it too so `dotnet run --project src/Griot.Api` works out of the box.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
// Explicit environment/CLI settings take precedence over the local development file.
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);

// Controllers (thin wrappers only; business logic lives in Griot.Application services).
// Enums serialize/deserialize as their string names (matches api-surface.md contract:
// "Backlog"/"Todo"/"InProgress"/"InReview"/"Done", "Low/Medium/High/Urgent", etc).
builder.Services.AddControllers(options => options.Filters.Add<AiAccessFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Application services (wired for DI; full implementations per specs 06/08).
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});

// Repositories (Infrastructure layer — Dapper for hot paths, EF Core via DbContext for regular ops).
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Generic repositories (specs 13-17): one per domain entity, injected into DomainService.
// Spec 29 tenancy: ITenantContext (AsyncLocal scope by default) + the generic
// repositories needed below, incl. the new organization stores.
builder.Services.AddScoped<Griot.Application.Tenancy.ITenantContext, Griot.Application.Tenancy.TenantContext>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.User>, GenericRepository<Griot.Domain.Entities.User>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Organization>, GenericRepository<Griot.Domain.Entities.Organization>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.OrganizationMember>, GenericRepository<Griot.Domain.Entities.OrganizationMember>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Role>, GenericRepository<Griot.Domain.Entities.Role>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.OrganizationInvite>, GenericRepository<Griot.Domain.Entities.OrganizationInvite>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.OrganizationLifecycleEvent>, GenericRepository<Griot.Domain.Entities.OrganizationLifecycleEvent>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Workspace>, GenericRepository<Griot.Domain.Entities.Workspace>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.WorkspaceMember>, GenericRepository<Griot.Domain.Entities.WorkspaceMember>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Invite>, GenericRepository<Griot.Domain.Entities.Invite>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Project>, GenericRepository<Griot.Domain.Entities.Project>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Board>, GenericRepository<Griot.Domain.Entities.Board>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Column>, GenericRepository<Griot.Domain.Entities.Column>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.TaskItem>, GenericRepository<Griot.Domain.Entities.TaskItem>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Comment>, GenericRepository<Griot.Domain.Entities.Comment>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Attachment>, GenericRepository<Griot.Domain.Entities.Attachment>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.Notification>, GenericRepository<Griot.Domain.Entities.Notification>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.ActivityLog>, GenericRepository<Griot.Domain.Entities.ActivityLog>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.ErrorLog>, GenericRepository<Griot.Domain.Entities.ErrorLog>>();
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.AuditLog>, GenericRepository<Griot.Domain.Entities.AuditLog>>();

// Spec 20 audit pipeline: request correlation + audit-trail writer. IAuditService queues
// AuditLogs/ActivityLogs rows into the same scoped change tracker as the domain mutation
// (atomic commit with the caller's SaveChanges); IRequestContext is the API-layer
// implementation of the application-layer correlation surface.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, RequestContext>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Spec 20 telemetry delivery ("Bumped §1"): bounded best-effort ApiLogs/ErrorLogs queue
// (capacity 1,000) with one background flusher + shutdown drain. Singleton = one shared
// queue; also hosted so StopAsync flushes before the host exits. The 500 handler records
// ErrorLogs through IErrorLogService (pipeline §2) — best-effort, never throws.
builder.Services.AddSingleton<ITelemetryWriter, TelemetryWriter>();
builder.Services.AddHostedService(sp => (TelemetryWriter)sp.GetRequiredService<ITelemetryWriter>());
builder.Services.AddSingleton<IErrorLogService, ErrorLogService>();

builder.Services.AddScoped<IDomainService, DomainService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext (SQL Server 2022) — connection string is config/env driven (ConnectionStrings__Default); never hardcoded.
var defaultConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured (set ConnectionStrings__Default).");

// Pooled DbContextFactory for both GraphQL DataLoaders and regular use
// This creates a context pool that can be used by both controllers and DataLoaders
builder.Services.AddPooledDbContextFactory<GriotDbContext>(options =>
{
    options.UseSqlServer(defaultConnection);
    options.UseLazyLoadingProxies();
});

// Also register as scoped for controllers that expect DbContext injection.
// Spec 29: every scoped context inherits the active tenant scope so the
// global query filters isolate reads to the request's organization
// (null scope outside requests / SuperAdmin bypass = see filter semantics).
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IDbContextFactory<GriotDbContext>>();
    return factory.CreateDbContext().WithTenant(
        sp.GetRequiredService<Griot.Application.Tenancy.ITenantContext>());
});

// Redis (spec 07): sliding-window rate limiting on /api/auth/login.
// Connection is config/env driven (Redis:Connection / Redis__Connection); localhost dev default below.
// IMPORTANT: StackExchange.Redis expects "host:port" format — NOT "redis://host:port".
var redisEndpoint = builder.Configuration["Redis:Connection"];
if (string.IsNullOrWhiteSpace(redisEndpoint))
    redisEndpoint = "localhost:6380"; // host port of the shared sababisha-redis container

var redisConnection = ConnectionMultiplexer.Connect(redisEndpoint);
// IConnectionMultiplexer is a long-lived shared object — must be singleton.
// Registering as scoped would allow the DI container to dispose it at request end,
// breaking all subsequent Redis calls.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => redisConnection);
builder.Services.AddScoped<IRedisRateLimiter, RedisRateLimiter>();

// Spec 09: Trigger.dev enqueue client (server-to-server; enqueue-after-persist pattern).
// Typed HttpClient — never exposed to web/mobile; failures never roll back domain writes.
builder.Services.AddHttpClient<TriggerDevClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpContextAccessor();

// GraphQL server (HotChocolate 14+) — code-first schema, DataLoaders, filtering, sorting, auth,
// query-cost guard (parser field/node caps + max execution depth + execution timeout).
builder.Services
    .AddGraphQLServer()
    .AddQueryType<GriotQuery>()
    .AddMutationType<GriotMutation>()
    .UseField<AiFieldMiddleware>()
    .AddAuthorization()
    .AddDataLoader<AssigneeDataLoader>()
    .AddDataLoader<CommentDataLoader>()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .ModifyRequestOptions(opt =>
    {
        // Execution timeout (abuse prevention)
        opt.ExecutionTimeout = TimeSpan.FromSeconds(30);
    })
    .ModifyParserOptions(opt =>
    {
        // Query-cost guard: cap document size/shape before parsing/validation runs
        // (parse-time DoS protection; complements the execution timeout).
        opt.MaxAllowedFields = 256;
        opt.MaxAllowedNodes = 512;
    })
    .AddMaxExecutionDepthRule(
        maxAllowedExecutionDepth: 10,
        skipIntrospectionFields: true,
        allowRequestOverrides: false);

// Health checks (/health).
builder.Services.AddHealthChecks();

// CORS (generic policy; origins from config, localhost Vite/React by default).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173", "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Auth: JWT bearer validation wiring (token issuance flows are spec 07; key from config, never hardcoded).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Resolve the finalized configuration used by AuthService, including host overrides.
        var jwtKey = builder.Configuration["JWT:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT:Key is not configured (set JWT__Key).");
        var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
        if (jwtKeyBytes.Length < 32)
            throw new InvalidOperationException($"JWT:Key must be at least 32 UTF-8 bytes (HS256 minimum); configured key is {jwtKeyBytes.Length} bytes. Set a longer JWT__Key.");
        var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "Griot";
        var jwtAudience = builder.Configuration["JWT:Audience"] ?? "GriotClients";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes)
        };
    })
    // Spec 09: AI service token scheme — GRIOT_SERVICE_TOKEN bearer → restricted ai-on-behalf-of
    // OBO principal (documented in docs/api/ai-service-token-contract.md).
    .AddScheme<AuthenticationSchemeOptions, ServiceTokenHandler>(
        ServiceTokenHandler.SchemeName, _ => { })
    // Spec 09 fix: With JwtBearer as the DEFAULT scheme, app.UseAuthentication() only runs
    // JwtBearerHandler — the ServiceToken scheme above was registered but never executed in
    // the live pipeline, so a valid GRIOT_SERVICE_TOKEN would still receive 401. (Unit tests
    // passed because they invoke the handler directly, bypassing the pipeline.)
    //
    // Fix = policy scheme forward selector (canonical "multi-auth" pattern): inspect the
    // Authorization header and forward to the right handler. A JWT-shaped bearer
    // (header.payload.signature = exactly two dots) goes to JwtBearer; any other bearer
    // goes to ServiceToken. Nothing else changes — JWT remains the default for challenges.
    .AddPolicyScheme("MultiAuth", "MultiAuth (JWT or Griot service token)", options =>
    {
        options.ForwardDefaultSelector = ctx =>
            ServiceTokenHandler.SelectScheme(
                ServiceTokenHandler.ResolveServiceToken(builder.Configuration),
                ctx.Request.Headers.Authorization.ToString());
    });

// The default scheme becomes the policy scheme, so UseAuthentication runs the selector
// and forwards each request to the correct concrete handler.
builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultScheme            = "MultiAuth";
    options.DefaultAuthenticateScheme = "MultiAuth";
    options.DefaultChallengeScheme    = "MultiAuth";
});

builder.Services.AddAuthorization();

// Rate limiting (per-client fixed window; refined in spec 06 auth and bulk paths).
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Spec 20 ("Bumped §5"): dev-only observability seed so Postman folder 14 and the
// dashboard have representative ApiLogs/AuditLogs data before real traffic. Production
// seeds nothing (environment-gated inside the seeder; skipped once any row exists).
await DevObservabilitySeeder.SeedAsync(app.Services);

// Spec 20 (pipeline §2): global exception handler — every unhandled 5xx is persisted to
// ErrorLogs (same RequestId as the X-Request-Id response header) and responded with an
// RFC 7807 `application/problem+json` body ({ type, title, status, detail?, traceId,
// requestId }). X-Request-Id is preserved on every response, including 500s. Dev keeps
// the exception detail body; production sends the generic title only. DomainError-based
// 4xx stays as-is (business errors are not exceptions) — only unhandled 5xx land in ErrorLogs.
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error
                    ?? context.Features.Get<IExceptionHandlerFeature>()?.Error;

        // Persist to ErrorLogs BEFORE the response is written; the service never throws
        // and the queue write completes synchronously (best-effort delivery contract).
        try
        {
            var errorLogs = context.RequestServices.GetRequiredService<IErrorLogService>();
            var errorUserId = Guid.TryParse(
                context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out var errorUid) ? errorUid : (Guid?)null;
            await errorLogs.RecordAsync(
                error ?? new InvalidOperationException("Unhandled exception with no handler feature."),
                Guid.TryParse(context.TraceIdentifier, out var rid) ? rid : null,
                errorUserId,
                "Griot.Api").ConfigureAwait(false);
        }
        catch
        {
            // Never let telemetry recording fail the 500 response itself.
        }

        context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new Dictionary<string, object?>
        {
            ["type"] = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            ["title"] = "Internal Server Error",
            ["status"] = StatusCodes.Status500InternalServerError,
            ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier,
            ["requestId"] = context.TraceIdentifier
        };
        if (context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment() && error is not null)
            problem["detail"] = error.Message; // Dev keeps the exception detail body.

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(problem)).ConfigureAwait(false);
    });
});

// Request-id middleware (spec 04 + spec 20 ERD amendment): X-Request-Id is normalized to
// a real GUID ("D" format) — assigned when absent, REPLACED when malformed — so
// TraceIdentifier, the X-Request-Id response header and every observability row
// (ApiLogs.RequestId, ErrorLogs.RequestId, AuditLogs.RequestId) carry the SAME value.
app.Use(async (context, next) =>
{
    var raw = context.Request.Headers["X-Request-Id"].FirstOrDefault();
    var requestId = Guid.TryParse(raw, out var parsed)
        ? parsed
        : Guid.NewGuid();

    context.Request.Headers["X-Request-Id"] = requestId.ToString("D");
    context.TraceIdentifier = requestId.ToString("D");
    context.Response.Headers["X-Request-Id"] = requestId.ToString("D");
    await next(context);
});

// Spec 20 (pipeline §1): ApiLogs capture starts HERE — registered after request-id and
// BEFORE every early exit (401 challenge, 404, 429 limiter, HMAC 401, 500) — and
// finalizes from response completion after authentication/exception handling, so rows
// record the status the client actually saw and the resolved user (or null when auth
// was never reached). Health checks are excluded. Fire-and-forget bounded write.
app.UseMiddleware<ApiLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseRateLimiter();

// Spec 09: Verify Trigger.dev HMAC signature before auth middleware runs on the webhook route.
// Must be before UseAuthentication so bad signatures are rejected without leaking auth details.
app.UseMiddleware<WebhookHmacMiddleware>();

// Spec 29: resolve the request tenant from the JWT `org` claim right after
// authentication so the whole downstream pipeline (controllers, services,
// scoped DbContext) runs inside the tenant scope.
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// GraphQL endpoint with Banana Cake Pop UI in development
// - /graphql → GraphQL endpoint (POST requests)
// - /graphql?sdl → Schema Definition Language (GET)
// - /graphql/ → Banana Cake Pop interactive UI (dev only)
app.MapGraphQL("/graphql")
    .WithOptions(options =>
    {
        options.Tool.Enable = app.Environment.IsDevelopment();
    });

app.Run();

public partial class Program { }
