// ═══════════════════════════════════════════════════════════════════════════
// Observability — usp_PruneObservabilityLogs integration test (spec 20, §5)
//
// SQL-gated (GRIOT_RUN_SQL_TESTS=1, same pattern as tests/Griot.Tests/Auth/).
// Seeds boundary rows against a disposable SQL Server database, applies the
// stored procedure, and verifies the retention contract:
//   ApiLogs      > 90d deleted, fresh kept
//   ErrorLogs    > 90d deleted ONLY when resolved (FixedAt set); unfixed survive
//   AuditLogs    > 365d deleted, fresh kept
//   ActivityLogs > 180d deleted, fresh kept
// ...and that a second execution is a no-op (idempotency).
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.IO;
using System.Threading.Tasks;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Griot.Infrastructure.Persistence;
using Griot.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Griot.Tests.Domain;

public class ObservabilityPruneSqlTests : IClassFixture<SqlAuthFixture>
{
    private readonly SqlAuthFixture _fixture;

    public ObservabilityPruneSqlTests(SqlAuthFixture fixture) => _fixture = fixture;

    private const string Marker = "prune-sql-test";

    private async Task ApplyProcedureAsync()
    {
        var procPath = Path.GetFullPath(Path.Combine(
            _fixture.ApiDirectory, "..", "Griot.Infrastructure", "Sql", "usp_PruneObservabilityLogs.sql"));
        var procText = await File.ReadAllTextAsync(procPath);
        // The batch ends with a GO separator that ExecuteSqlRawAsync must not see.
        var batch = System.Text.RegularExpressions.Regex.Replace(
            procText.ReplaceLineEndings("\n"), @"\nGO\s*$", "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        await using var ctx = _fixture.CreateContext();
        // 1) CREATE OR ALTER the procedure (single batch, no GO), then 2) execute it.
        await ctx.Database.ExecuteSqlRawAsync(batch);
        await ctx.Database.ExecuteSqlRawAsync("EXEC dbo.usp_PruneObservabilityLogs");
    }

    [SqlAuthFact]
    public async Task PruneObservabilityLogs_RetentionWindows_PrunesOldRowsAndIsIdempotent()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = $"{Marker}-{Guid.NewGuid():N}@griot.test",
            DisplayName = "Prune SQL User",
            PasswordHash = "sql-test-not-real"
        };
        var pruneOrg = Guid.NewGuid();
        var workspace = new Workspace
        {
            Name = "Prune SQL Workspace",
            Slug = $"{Marker}-{Guid.NewGuid():N}",
            OwnerId = user.Id,
            // Spec 29: required tenant column on the post-tenancy schema.
            OrganizationId = pruneOrg
        };

        var oldApiPath = $"/prune-sql/old/{Guid.NewGuid():N}";
        var freshApiPath = $"/prune-sql/fresh/{Guid.NewGuid():N}";
        var resolvedOldError = $"prune-sql resolved-old {Guid.NewGuid():N}";
        var unresolvedOldError = $"prune-sql unresolved-old {Guid.NewGuid():N}";
        var resolvedFreshError = $"prune-sql resolved-fresh {Guid.NewGuid():N}";
        var auditOldRequestId = Guid.NewGuid();
        var auditFreshRequestId = Guid.NewGuid();

        // Seed (FK-complete: organization + user + workspace first, then the log rows).
        await using (var ctx = _fixture.CreateContext())
        {
            ctx.Users.Add(user);
            // Spec 29: Workspaces.OrganizationId is a real FK to Organizations — the
            // tenant row must exist before the workspace can be persisted.
            ctx.Organizations.Add(new Organization
            {
                Id = pruneOrg,
                Name = "Prune SQL Org",
                Slug = $"{Marker}-{Guid.NewGuid():N}",
                OwnerId = user.Id
            });
            ctx.Workspaces.Add(workspace);
            await ctx.SaveChangesAsync();

            ctx.ApiLogs.AddRange(
                new ApiLog { RequestId = Guid.NewGuid(), Method = "GET", Path = oldApiPath, StatusCode = 500, DurationMs = 1, UserAgent = Marker, IpAddress = "127.0.0.1", CreatedAt = now.AddDays(-91) },
                new ApiLog { RequestId = Guid.NewGuid(), Method = "GET", Path = freshApiPath, StatusCode = 200, DurationMs = 1, UserAgent = Marker, IpAddress = "127.0.0.1", CreatedAt = now.AddDays(-2) });

            ctx.ErrorLogs.AddRange(
                new ErrorLog { ExceptionType = "System.InvalidOperationException", Message = resolvedOldError, UserId = user.Id, FixStatus = ErrorFixStatus.Fixed, FixedAt = now.AddDays(-90), SolvedByUserId = user.Id, CreatedAt = now.AddDays(-91) },
                new ErrorLog { ExceptionType = "System.InvalidOperationException", Message = unresolvedOldError, UserId = user.Id, FixStatus = ErrorFixStatus.Open, FixedAt = null, CreatedAt = now.AddDays(-91) },
                new ErrorLog { ExceptionType = "System.InvalidOperationException", Message = resolvedFreshError, UserId = user.Id, FixStatus = ErrorFixStatus.Fixed, FixedAt = now.AddDays(-2), SolvedByUserId = user.Id, CreatedAt = now.AddDays(-2) });

            ctx.AuditLogs.AddRange(
                new AuditLog { ActorId = user.Id, Action = "Prune.Sql.Old", EntityType = "Task", EntityId = Guid.NewGuid(), RequestId = auditOldRequestId, CreatedAt = now.AddDays(-366) },
                new AuditLog { ActorId = user.Id, Action = "Prune.Sql.Fresh", EntityType = "Task", EntityId = Guid.NewGuid(), RequestId = auditFreshRequestId, CreatedAt = now.AddDays(-2) });

            ctx.ActivityLogs.AddRange(
                new ActivityLog { WorkspaceId = workspace.Id, OrganizationId = pruneOrg, ActorId = user.Id, EntityType = "Task", EntityId = Guid.NewGuid(), Action = "Prune.Sql.Old", Payload = Marker, CreatedAt = now.AddDays(-181) },
                new ActivityLog { WorkspaceId = workspace.Id, OrganizationId = pruneOrg, ActorId = user.Id, EntityType = "Task", EntityId = Guid.NewGuid(), Action = "Prune.Sql.Fresh", Payload = Marker, CreatedAt = now.AddDays(-2) });

            await ctx.SaveChangesAsync();
        }

        // GriotDbContext.UpdateTimestamps() overwrites CreatedAt to UtcNow on every
        // Added entity, so the backdated seeds must be re-aged with raw SQL AFTER
        // the commit (this is the only way to make "old" rows genuinely old).
        await using (var ctx = _fixture.CreateContext())
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.ApiLogs SET CreatedAt = {0} WHERE Path = {1}", now.AddDays(-91), oldApiPath);
            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.ApiLogs SET CreatedAt = {0} WHERE Path = {1}", now.AddDays(-2), freshApiPath);

            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.ErrorLogs SET CreatedAt = {0} WHERE Message = {1}", now.AddDays(-91), resolvedOldError);
            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.ErrorLogs SET CreatedAt = {0} WHERE Message = {1}", now.AddDays(-91), unresolvedOldError);
            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.ErrorLogs SET CreatedAt = {0} WHERE Message = {1}", now.AddDays(-2), resolvedFreshError);

            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.AuditLogs SET CreatedAt = {0} WHERE RequestId = {1}", now.AddDays(-366), auditOldRequestId);
            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.AuditLogs SET CreatedAt = {0} WHERE RequestId = {1}", now.AddDays(-2), auditFreshRequestId);

            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.ActivityLogs SET CreatedAt = {0} WHERE Action = {1} AND Payload = {2}", now.AddDays(-181), "Prune.Sql.Old", Marker);
            await ctx.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.ActivityLogs SET CreatedAt = {0} WHERE Action = {1} AND Payload = {2}", now.AddDays(-2), "Prune.Sql.Fresh", Marker);
        }

        // Act — first execution.
        await ApplyProcedureAsync();

        // Assert — retention contract.
        await using (var ctx = _fixture.CreateContext())
        {
            Assert.False(await ctx.ApiLogs.AnyAsync(a => a.Path == oldApiPath), "ApiLogs older than 90 days must be pruned.");
            Assert.True(await ctx.ApiLogs.AnyAsync(a => a.Path == freshApiPath), "Fresh ApiLogs must survive.");

            Assert.False(await ctx.ErrorLogs.AnyAsync(e => e.Message == resolvedOldError), "Resolved ErrorLogs older than 90 days must be pruned.");
            Assert.True(await ctx.ErrorLogs.AnyAsync(e => e.Message == unresolvedOldError), "Unfixed ErrorLogs must survive the retention tail until triaged.");
            Assert.True(await ctx.ErrorLogs.AnyAsync(e => e.Message == resolvedFreshError), "Fresh ErrorLogs must survive.");

            Assert.False(await ctx.AuditLogs.AnyAsync(a => a.RequestId == auditOldRequestId), "AuditLogs older than 365 days must be pruned.");
            Assert.True(await ctx.AuditLogs.AnyAsync(a => a.RequestId == auditFreshRequestId), "Fresh AuditLogs must survive.");

            Assert.False(await ctx.ActivityLogs.AnyAsync(a => a.Action == "Prune.Sql.Old"), "ActivityLogs older than 180 days must be pruned.");
            Assert.True(await ctx.ActivityLogs.AnyAsync(a => a.Action == "Prune.Sql.Fresh"), "Fresh ActivityLogs must survive.");
        }

        // Act — second execution (idempotency: deletes 0, must not throw).
        await ApplyProcedureAsync();

        // Assert — state unchanged after the re-run.
        await using (var ctx = _fixture.CreateContext())
        {
            Assert.True(await ctx.ApiLogs.AnyAsync(a => a.Path == freshApiPath));
            Assert.True(await ctx.ErrorLogs.AnyAsync(e => e.Message == unresolvedOldError));
            Assert.True(await ctx.ErrorLogs.AnyAsync(e => e.Message == resolvedFreshError));
            Assert.True(await ctx.AuditLogs.AnyAsync(a => a.RequestId == auditFreshRequestId));
            Assert.True(await ctx.ActivityLogs.AnyAsync(a => a.Action == "Prune.Sql.Fresh"));
            Assert.False(await ctx.ApiLogs.AnyAsync(a => a.Path == oldApiPath));
            Assert.False(await ctx.AuditLogs.AnyAsync(a => a.RequestId == auditOldRequestId));
        }
    }
}