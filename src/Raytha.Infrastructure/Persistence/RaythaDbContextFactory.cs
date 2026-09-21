using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Raytha.Infrastructure.Persistence;

public class RaythaDbContextFactory : IDesignTimeDbContextFactory<RaythaDbContext>
{
    public RaythaDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5433;Username=postgres;Password=changeme;Database=raytha";

        var optionsBuilder = new DbContextOptionsBuilder<RaythaDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new RaythaDbContext(optionsBuilder.Options);
    }
}
