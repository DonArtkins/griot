using Griot.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Griot.Domain.Entities;

namespace Griot.Tests.Auth;

public sealed class SqlAuthFactAttribute : FactAttribute
{
    public SqlAuthFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("GRIOT_RUN_SQL_TESTS") != "1")
            Skip = "Set GRIOT_RUN_SQL_TESTS=1 to test against a disposable SQL Server database.";
    }
}

public sealed class SqlAuthFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;
    public string ApiDirectory { get; private set; } = string.Empty;
    public Guid LegacyRootId { get; } = Guid.NewGuid();
    public Guid LegacyReplacementId { get; } = Guid.NewGuid();
    public Guid LegacyIndependentId { get; } = Guid.NewGuid();

    public GriotDbContext CreateContext() => new(
        new DbContextOptionsBuilder<GriotDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("GRIOT_RUN_SQL_TESTS") != "1")
            return;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Griot.sln")))
            directory = directory.Parent;
        ApiDirectory = Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Backend root not found."), "src", "Griot.Api");
        var config = new ConfigurationBuilder().SetBasePath(ApiDirectory)
            .AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables().Build();
        var source = config["GRIOT_TEST_SQL_CONNECTION"] ?? config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Configure GRIOT_TEST_SQL_CONNECTION for SQL integration tests.");
        var connection = new SqlConnectionStringBuilder(source)
        {
            InitialCatalog = $"GriotAuthTests_{Guid.NewGuid():N}",
            Pooling = false
        };
        ConnectionString = connection.ConnectionString;
        await using var context = CreateContext();
        await context.GetService<IMigrator>().MigrateAsync("20260907195152_AddOtpAndReports");
        var user = new User { Email = $"migration-{Guid.NewGuid():N}@example.com", DisplayName = "Migration test", PasswordHash = "test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        // Seed the pre-migration schema without referring to the new FamilyId column.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.RefreshTokens (Id, UserId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenId, CreatedAt)
            VALUES ({LegacyRootId}, {user.Id}, {LegacyRootId.ToString()}, DATEADD(day, 30, SYSUTCDATETIME()), SYSUTCDATETIME(), {LegacyReplacementId}, SYSUTCDATETIME()),
                   ({LegacyReplacementId}, {user.Id}, {LegacyReplacementId.ToString()}, DATEADD(day, 30, SYSUTCDATETIME()), NULL, NULL, SYSUTCDATETIME()),
                   ({LegacyIndependentId}, {user.Id}, {LegacyIndependentId.ToString()}, DATEADD(day, 30, SYSUTCDATETIME()), NULL, NULL, SYSUTCDATETIME());
            """);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString))
            return;
        // Only the unique database created by this fixture can be removed.
        var database = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        if (!database.StartsWith("GriotAuthTests_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to remove a non-test database.");
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
