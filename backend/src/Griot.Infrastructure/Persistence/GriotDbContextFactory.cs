using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Griot.Infrastructure.Persistence;

public class GriotDbContextFactory : IDesignTimeDbContextFactory<GriotDbContext>
{
    public GriotDbContext CreateDbContext(string[] args)
    {
        var basePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "../Griot.Api");
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new System.InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<GriotDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new GriotDbContext(optionsBuilder.Options);
    }
}
