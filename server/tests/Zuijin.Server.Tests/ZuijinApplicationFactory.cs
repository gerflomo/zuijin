using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zuijin.Infrastructure.Persistence;
using Zuijin.Infrastructure.Services;

namespace Zuijin.Server.Tests;

/// <summary>
/// Boots the real host against a dedicated test database. Configuration is supplied
/// in memory so the tests never depend on the developer's user secrets.
/// </summary>
public sealed class ZuijinApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://localhost:7295";

    private const string TestConnectionString =
        "Server=localhost;Database=Zuijin_Test;Trusted_Connection=True;TrustServerCertificate=True";

    // Fixed so signing keys persisted by a previous run stay decryptable.
    private const string TestMasterKey = "8Xk1vQ2mR7pT4wZ9aB6cD3eF0gH5jK8lM1nO4pQ7rS0=";

    public async ValueTask InitializeAsync()
    {
        // The host's key maintenance service writes to the database during startup,
        // so the schema must exist before the factory builds the host.
        var options = new DbContextOptionsBuilder<ZuijinDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        await using var context = new ZuijinDbContext(options, new SystemClock());
        await context.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting rather than ConfigureAppConfiguration: the host reads the
        // connection string while building, before app configuration sources are added.
        foreach (var (key, value) in TestConfiguration)
        {
            builder.UseSetting(key, value);
        }
    }

    private static Dictionary<string, string> TestConfiguration => new()
    {
        ["ConnectionStrings:Zuijin"] = TestConnectionString,
        ["Zuijin:Issuer"] = Issuer,
        ["Zuijin:SigningKeyMasterKey"] = TestMasterKey,
        ["Zuijin:RequirePkce"] = "true",
        ["Zuijin:RequireHttpsRedirectUris"] = "true",
        ["Zuijin:DefaultAccessTokenLifetime"] = "3600",
        ["Zuijin:DefaultRefreshTokenLifetime"] = "2592000",
        ["Zuijin:DefaultIdTokenLifetime"] = "3600",
        ["Zuijin:AuthorizationCodeLifetime"] = "300",
        ["Zuijin:DeviceCodeLifetime"] = "300",
        ["Zuijin:DeviceCodePollingInterval"] = "5",
        ["Zuijin:KeyRotationIntervalDays"] = "90",
        ["Zuijin:MaxFailedLoginAttempts"] = "5",
        ["Zuijin:LockoutDurationMinutes"] = "15"
    };
}
