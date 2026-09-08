using System.Text;
using System.Threading.RateLimiting;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Controllers (thin wrappers only; business logic lives in Griot.Application services).
builder.Services.AddControllers();

// Application services (wired for DI; full implementations per specs 06/08).
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext (SQL Server 2022) — connection string is config/env driven (ConnectionStrings__Default); never hardcoded.
var defaultConnection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured (set ConnectionStrings__Default).");
builder.Services.AddDbContext<GriotDbContext>(options =>
    options.UseSqlServer(defaultConnection));

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

// Auth: JWT bearer validation wiring (token issuance flows are spec 08; key from config, never hardcoded).
var jwtKey = builder.Configuration["JWT:Key"]
    ?? throw new InvalidOperationException("JWT:Key is not configured (set JWT__Key).");
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "Griot";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "GriotClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

// Request-id middleware (spec 04: structured logging + stable request id on every response).
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("X-Request-Id", out var requestId))
    {
        requestId = Guid.NewGuid().ToString("N");
        context.Request.Headers["X-Request-Id"] = requestId;
    }

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

app.Run();
