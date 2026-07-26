using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zuijin.Infrastructure.DependencyInjection;

namespace Zuijin.AspNetCore.DependencyInjection;

public class ZuijinBuilder
{
    public IServiceCollection Services { get; }

    internal ZuijinBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public ZuijinBuilder UseEntityFramework(Action<DbContextOptionsBuilder> configureDbContext)
    {
        Services.AddZuijinInfrastructure(configureDbContext);
        return this;
    }

    public ZuijinBuilder UseSqlServer(string connectionString)
    {
        return UseEntityFramework(options => options.UseSqlServer(connectionString));
    }
}
