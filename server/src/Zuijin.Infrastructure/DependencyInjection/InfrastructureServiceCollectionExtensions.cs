using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zuijin.Application.Abstractions;
using Zuijin.Domain.Repositories;
using Zuijin.Infrastructure.Persistence;
using Zuijin.Infrastructure.Persistence.Repositories;
using Zuijin.Infrastructure.Security;
using Zuijin.Infrastructure.Services;

namespace Zuijin.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddZuijinInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<ZuijinDbContext>(configureDbContext);
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ZuijinDbContext>());

        // Repositories
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IScopeRepository, ScopeRepository>();
        services.AddScoped<IAuthorizationGrantRepository, AuthorizationGrantRepository>();
        services.AddScoped<IDeviceCodeRepository, DeviceCodeRepository>();
        services.AddScoped<IConsentRepository, ConsentRepository>();
        services.AddScoped<ISigningKeyRepository, SigningKeyRepository>();

        // Services
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
        services.AddSingleton<IKeyProtector, AesKeyProtector>();
        services.AddSingleton<ISigningKeyService, SigningKeyService>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddHostedService<SigningKeyMaintenanceService>();

        return services;
    }
}
