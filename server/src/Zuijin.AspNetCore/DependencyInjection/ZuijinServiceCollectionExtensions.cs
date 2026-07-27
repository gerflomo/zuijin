using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zuijin.Application.Configuration;
using Zuijin.Application.Features.Tokens;

namespace Zuijin.AspNetCore.DependencyInjection;

public static class ZuijinServiceCollectionExtensions
{
    public static ZuijinBuilder AddZuijin(this IServiceCollection services, Action<ZuijinOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<ZuijinOptions>()
            .Configure(configureOptions)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<ZuijinOptions>, ValidateZuijinOptions>();

        // Concrete singleton so consumers can depend on ZuijinOptions directly;
        // resolving it triggers the same validation as IOptions<ZuijinOptions>.
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ZuijinOptions>>().Value);

        // Application feature handlers.
        services.AddScoped<ClientCredentialsTokenHandler>();

        return new ZuijinBuilder(services);
    }

    private sealed class ValidateZuijinOptions : IValidateOptions<ZuijinOptions>
    {
        public ValidateOptionsResult Validate(string? name, ZuijinOptions options)
        {
            var errors = ZuijinOptionsValidator.Validate(options);

            return errors.Count > 0
                ? ValidateOptionsResult.Fail(errors)
                : ValidateOptionsResult.Success;
        }
    }
}
