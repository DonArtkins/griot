using System.Text.Json.Serialization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Griot.Api.GraphQL;
using Griot.Api.GraphQL.DataLoaders;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Infrastructure.Persistence;
using Griot.Infrastructure.Redis;
using Griot.Infrastructure.Email;
using Griot.Infrastructure.Repositories;
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Options;
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
builder.Services.AddControllers()
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
builder.Services.AddScoped<IGenericRepository<Griot.Domain.Entities.User>, GenericRepository<Griot.Domain.Entities.User>>();
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

// Also register as scoped for controllers that expect DbContext injection
builder.Services.AddScoped(sp => 
{
    var factory = sp.GetRequiredService<IDbContextFactory<GriotDbContext>>();
    return factory.CreateDbContext();
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

// GraphQL server (HotChocolate 14+) — code-first schema, DataLoaders, filtering, sorting, auth,
// query-cost guard (parser field/node caps + max execution depth + execution timeout).
builder.Services
    .AddGraphQLServer()
    .AddQueryType<GriotQuery>()
    .AddMutationType<GriotMutation>()
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

// Spec 04: global exception handler — every response carries X-Request-Id,
// including 500s. Must wrap the request-id middleware so TraceIdentifier is
// available; the handler re-applies the header after the downstream pipeline
// (e.g. the dev exception page) clears response headers.
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/plain";

        if (context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error
                        ?? context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            await context.Response.WriteAsync(error?.ToString() ?? "Internal Server Error");
        }
        else
        {
            await context.Response.WriteAsync("Internal Server Error");
        }
    });
});

// Request-id middleware (spec 04: structured logging + stable request id on every response).
app.Use(async (context, next) =>
{
    var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString("N");

    context.Request.Headers["X-Request-Id"] = requestId;
    context.TraceIdentifier = requestId;
    context.Response.Headers["X-Request-Id"] = requestId;
    await next(context);
});

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
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
