using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Griot.Infrastructure.Persistence;

public class GriotDbContextFactory : IDesignTimeDbContextFactory<GriotDbContext>
{
    public GriotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GriotDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,14333;Database=Griot;User Id=sa;Password=SababishaDev2026!;TrustServerCertificate=True");

        return new GriotDbContext(optionsBuilder.Options);
    }
}
