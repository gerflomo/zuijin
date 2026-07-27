using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Infrastructure.Persistence;
using Zuijin.Infrastructure.Persistence.Seeding;
using Zuijin.Infrastructure.Security;
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

    public const string ConfidentialClientId = "test-client";
    public const string ConfidentialClientSecret = "test-client-secret";
    public const string PublicClientId = "test-public-client";
    public const string DisabledClientId = "test-disabled-client";
    public const string WrongGrantClientId = "test-wrong-grant-client";

    public async ValueTask InitializeAsync()
    {
        // The host's key maintenance service writes to the database during startup,
        // so the schema must exist before the factory builds the host.
        var options = new DbContextOptionsBuilder<ZuijinDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        await using var context = new ZuijinDbContext(options, new SystemClock());
        await context.Database.MigrateAsync();
        await SeedTestClients(context);
        await SeedTestUser(context);
    }

    /// <summary>
    /// A user with one role and one permission, so token issuance exercises RBAC resolution.
    /// </summary>
    private static async Task SeedTestUser(ZuijinDbContext context)
    {
        await context.Users.IgnoreQueryFilters().Where(user => user.Id == UserId).ExecuteDeleteAsync();
        await context.Roles.IgnoreQueryFilters().Where(role => role.Id == RoleId).ExecuteDeleteAsync();
        await context.Permissions.IgnoreQueryFilters()
            .Where(permission => permission.Id == PermissionId).ExecuteDeleteAsync();

        context.Permissions.Add(new Permission
        {
            Id = PermissionId,
            Name = PermissionName,
            DisplayName = "Read reports"
        });

        context.Roles.Add(new Role
        {
            Id = RoleId,
            Name = RoleName,
            RolePermissions = [new RolePermission { PermissionId = PermissionId }]
        });

        context.Users.Add(new User
        {
            Id = UserId,
            SubjectId = SubjectId,
            Username = Username,
            Email = "test-user@example.test",
            EmailConfirmed = true,
            PasswordHash = new Argon2PasswordHasher().HashPassword(Password),
            IsActive = true,
            UserRoles = [new UserRole { RoleId = RoleId }]
        });

        await context.SaveChangesAsync();
    }

    /// <summary>Name of the fixture API resource, i.e. the audience the tests expect.</summary>
    public const string ApiAudience = "https://api.zuijin.test";

    /// <summary>A scope owned by <see cref="ApiAudience"/>, as opposed to the identity scopes.</summary>
    public const string ApiScopeName = "api.read";

    private static readonly Guid ApiResourceId = new("019fa100-0000-7000-8000-000000000001");
    private static readonly Guid ApiScopeId = new("019fa100-0000-7000-8000-000000000002");

    /// <summary>Client configured for the authorization code flow.</summary>
    public const string CodeClientId = "test-code-client";

    public const string CodeClientRedirectUri = "https://client.example.test/callback";
    public const string ConsentClientId = "test-consent-client";
    public const string Username = "test-user";
    public const string Password = "test-user-password";
    public const string SubjectId = "test-subject-id";
    public const string RoleName = "tester";
    public const string PermissionName = "reports.read";

    private static readonly Guid UserId = new("019fa100-0000-7000-8000-000000000003");
    private static readonly Guid RoleId = new("019fa100-0000-7000-8000-000000000004");
    private static readonly Guid PermissionId = new("019fa100-0000-7000-8000-000000000005");

    /// <summary>
    /// Recreates the fixture data so each run starts from a known state.
    /// </summary>
    private static async Task SeedTestClients(ZuijinDbContext context)
    {
        string[] fixtureClientIds =
        [
            ConfidentialClientId, PublicClientId, DisabledClientId, WrongGrantClientId,
            CodeClientId, ConsentClientId
        ];

        // Clients first: they reference the scope that is deleted below.
        await context.Clients
            .IgnoreQueryFilters()
            .Where(client => fixtureClientIds.Contains(client.ClientId))
            .ExecuteDeleteAsync();

        await context.ApiResources
            .IgnoreQueryFilters()
            .Where(resource => resource.Id == ApiResourceId)
            .ExecuteDeleteAsync();

        await context.Scopes
            .IgnoreQueryFilters()
            .Where(scope => scope.Id == ApiScopeId)
            .ExecuteDeleteAsync();

        context.Scopes.Add(new Scope
        {
            Id = ApiScopeId,
            Name = ApiScopeName,
            DisplayName = "Read access",
            IsStandard = false,
            IsActive = true
        });

        context.ApiResources.Add(new ApiResource
        {
            Id = ApiResourceId,
            Name = ApiAudience,
            DisplayName = "Zuijin test API",
            IsActive = true,
            Scopes = [new ApiResourceScope { ScopeId = ApiScopeId }]
        });

        var secretHash = new Sha256SecretHasher().Hash(ConfidentialClientSecret);

        context.Clients.AddRange(
            CreateClient(ConfidentialClientId, secretHash, ClientType.Confidential, isActive: true,
                GrantTypes.ClientCredentials),
            CreateClient(PublicClientId, secretHash: null, ClientType.Public, isActive: true,
                GrantTypes.ClientCredentials),
            CreateClient(DisabledClientId, secretHash, ClientType.Confidential, isActive: false,
                GrantTypes.ClientCredentials),
            CreateClient(WrongGrantClientId, secretHash, ClientType.Confidential, isActive: true,
                GrantTypes.AuthorizationCode),
            CreateCodeFlowClient(CodeClientId, secretHash, requireConsent: false),
            CreateCodeFlowClient(ConsentClientId, secretHash, requireConsent: true));

        await context.SaveChangesAsync();
    }

    private static Client CreateCodeFlowClient(string clientId, string secretHash, bool requireConsent)
    {
        return new Client
        {
            Id = Guid.CreateVersion7(),
            ClientId = clientId,
            ClientName = clientId,
            SecretHash = secretHash,
            Type = ClientType.Confidential,
            IsActive = true,
            RequirePkce = true,
            RequireConsent = requireConsent,
            AllowOfflineAccess = true,
            RedirectUris = [new ClientRedirectUri { Uri = CodeClientRedirectUri, Type = RedirectUriType.Redirect }],
            GrantTypes =
            [
                new ClientGrantType { GrantType = GrantTypes.AuthorizationCode },
                new ClientGrantType { GrantType = GrantTypes.RefreshToken }
            ],
            Scopes =
            [
                new ClientScope { ScopeId = StandardScopeSeed.OpenIdId },
                new ClientScope { ScopeId = StandardScopeSeed.ProfileId },
                new ClientScope { ScopeId = StandardScopeSeed.OfflineAccessId },
                new ClientScope { ScopeId = ApiScopeId }
            ]
        };
    }

    private static Client CreateClient(
        string clientId,
        string? secretHash,
        ClientType type,
        bool isActive,
        string grantType)
    {
        return new Client
        {
            Id = Guid.CreateVersion7(),
            ClientId = clientId,
            ClientName = clientId,
            SecretHash = secretHash,
            Type = type,
            IsActive = isActive,
            RequirePkce = true,
            RequireConsent = false,
            GrantTypes = [new ClientGrantType { GrantType = grantType }],
            Scopes =
            [
                // openid is present on purpose: the client credentials grant must not grant it.
                new ClientScope { ScopeId = StandardScopeSeed.OpenIdId },
                new ClientScope { ScopeId = StandardScopeSeed.ProfileId },
                new ClientScope { ScopeId = ApiScopeId }
            ]
        };
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
