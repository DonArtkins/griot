using System;
using System.Linq;
using System.Threading.Tasks;
using Griot.Domain.Entities;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Griot.Api.Services;

/// <summary>
/// Spec 20 "Bumped §5" DEV-ONLY seed: writes a handful of representative ApiLogs and
/// AuditLogs rows so Postman folder 14 ("Audit — trail captured") and the dashboard
/// have visible data before real traffic exists. Production seeds NOTHING (gated by
/// environment + `Observability:DevSeed`; skipped the moment any row exists). No
/// hardcoded user/workspace GUIDs — the actor is resolved from the existing Users
/// table, and rows only appear when at least one real user exists. ApiLogs.RequestId
/// is unique per the ERD, so every seeded row gets its own generated GUID.
/// </summary>
public static class DevObservabilitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
            return;

        var config = services.GetRequiredService<IConfiguration>();
        if (string.Equals(config["Observability:DevSeed"], "false", StringComparison.OrdinalIgnoreCase))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GriotDbContext>();

        if (await db.ApiLogs.AnyAsync().ConfigureAwait(false) || await db.AuditLogs.AnyAsync().ConfigureAwait(false))
            return;

        var user = await db.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync().ConfigureAwait(false);
        if (user is null)
            return; // Nothing to attach an audit actor to; real traffic will populate the tables.

        var now = DateTime.UtcNow;
        db.ApiLogs.AddRange(
            new ApiLog
            {
                RequestId = Guid.NewGuid(), UserId = user.Id, Method = "GET", Path = "/api/workspaces",
                StatusCode = 200, DurationMs = 14, UserAgent = "dev-seed", IpAddress = "127.0.0.0",
                CreatedAt = now.AddMinutes(-9)
            },
            new ApiLog
            {
                RequestId = Guid.NewGuid(), UserId = user.Id, Method = "GET", Path = "/api/dashboard/summary",
                QueryString = "workspaceId=[redacted]", StatusCode = 200, DurationMs = 21,
                UserAgent = "dev-seed", IpAddress = "127.0.0.0", CreatedAt = now.AddMinutes(-6)
            },
            new ApiLog
            {
                RequestId = Guid.NewGuid(), Method = "GET", Path = "/api/workspaces",
                StatusCode = 401, DurationMs = 2, UserAgent = "dev-seed", IpAddress = "127.0.0.0",
                CreatedAt = now.AddMinutes(-3)
            });

        var seededRequestId = Guid.NewGuid();
        db.AuditLogs.AddRange(
            new AuditLog
            {
                ActorId = user.Id, Action = "Dev.Seed", EntityType = "Workspace", EntityId = Guid.NewGuid(),
                Before = null, After = "{\"note\":\"dev observability seed row\"}", RequestId = seededRequestId,
                CreatedAt = now.AddMinutes(-9)
            },
            new AuditLog
            {
                ActorId = user.Id, Action = "Dev.Seed", EntityType = "Task", EntityId = Guid.NewGuid(),
                Before = null, After = "{\"note\":\"dev observability seed row\"}", RequestId = seededRequestId,
                CreatedAt = now.AddMinutes(-9)
            });

        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
