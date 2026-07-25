using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zuijin.Application.Configuration;
using Zuijin.Infrastructure.DependencyInjection;

namespace Zuijin.AspNetCore.DependencyInjection;

public class ZuijinBuilder
{
    public IServiceCollection Services { get; }
    public ZuijinOptions Options { get; }

    internal ZuijinBuilder(IServiceCollection services, ZuijinOptions options)
    {
        Services = services;
        Options = options;
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
