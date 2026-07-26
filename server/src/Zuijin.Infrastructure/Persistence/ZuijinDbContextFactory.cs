using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Zuijin.Infrastructure.Services;

namespace Zuijin.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef` commands can instantiate the context
/// without the application's DI container. The connection string can be
/// overridden with the ZUIJIN_CONNECTIONSTRING environment variable; the
/// default targets the local development SQL Server instance.
/// </summary>
public class ZuijinDbContextFactory : IDesignTimeDbContextFactory<ZuijinDbContext>
{
    public ZuijinDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ZUIJIN_CONNECTIONSTRING")
            ?? "Server=localhost;Database=Zuijin;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ZuijinDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ZuijinDbContext(options, new SystemClock());
    }
}
