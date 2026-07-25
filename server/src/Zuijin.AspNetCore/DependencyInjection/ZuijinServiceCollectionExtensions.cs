using Microsoft.Extensions.DependencyInjection;
using Zuijin.Application.Configuration;

namespace Zuijin.AspNetCore.DependencyInjection;

public static class ZuijinServiceCollectionExtensions
{
    public static ZuijinBuilder AddZuijin(this IServiceCollection services, Action<ZuijinOptions>? configureOptions = null)
    {
        var options = new ZuijinOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);

        return new ZuijinBuilder(services, options);
    }
}
