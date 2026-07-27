using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zuijin.Application.Configuration;
using Zuijin.Application.Features.Authorization;
using Zuijin.Application.Features.Tokens;
using Zuijin.Application.Features.Users;
using Zuijin.AspNetCore.Authentication;
using Zuijin.AspNetCore.Endpoints;

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

        AddSessionAuthentication(services);
        AddFeatureHandlers(services);

        return new ZuijinBuilder(services);
    }

    /// <summary>
    /// Registers the end-user session under Zuijin's own scheme without touching the host's
    /// default scheme, so an application embedding Zuijin keeps its own authentication intact.
    /// </summary>
    private static void AddSessionAuthentication(IServiceCollection services)
    {
        services.AddAuthentication()
            .AddCookie(ZuijinAuthenticationDefaults.SessionScheme, options =>
            {
                options.Cookie.Name = ZuijinAuthenticationDefaults.SessionCookieName;
                options.Cookie.HttpOnly = true;
                // Lax rather than Strict: the client redirects the browser here by top-level
                // navigation, and Strict would drop the cookie on that hop.
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.LoginPath = ZuijinEndpointPaths.Login;
                options.SlidingExpiration = true;
            });
    }

    private static void AddFeatureHandlers(IServiceCollection services)
    {
        services.AddScoped<ClientAuthenticator>();
        services.AddScoped<ClientCredentialsTokenHandler>();
        services.AddScoped<AuthorizationCodeTokenHandler>();
        services.AddScoped<RefreshTokenGrantHandler>();
        services.AddScoped<UserTokenIssuer>();
        services.AddScoped<UserClaimsResolver>();
        services.AddScoped<UserAuthenticator>();
        services.AddScoped<AuthorizeRequestValidator>();
        services.AddScoped<AuthorizationCodeIssuer>();
        services.AddScoped<ConsentService>();
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
