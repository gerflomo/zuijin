using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Repositories;

namespace Zuijin.AspNetCore.Endpoints.Discovery;

/// <summary>
/// OpenID Provider metadata and JWK Set endpoints. Both are anonymous by design:
/// relying parties read them before any credential exists.
/// </summary>
public static class DiscoveryEndpoints
{
    private const string SubjectTypePublic = "public";
    private const string SigningAlgorithm = "RS256";

    private static readonly string[] TokenEndpointAuthMethods =
        ["client_secret_basic", "client_secret_post"];

    private static readonly string[] SupportedCodeChallengeMethods = [CodeChallengeMethods.S256];

    private static readonly string[] GrantTypesSupported =
    [
        GrantTypes.AuthorizationCode,
        GrantTypes.ClientCredentials,
        GrantTypes.RefreshToken,
        GrantTypes.DeviceCode
    ];

    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ZuijinEndpointPaths.Discovery, GetDiscoveryDocument)
            .WithName("OpenIdConfiguration");

        endpoints.MapGet(ZuijinEndpointPaths.Jwks, GetJsonWebKeySet)
            .WithName("JsonWebKeySet");

        return endpoints;
    }

    private static async Task<IResult> GetDiscoveryDocument(
        ZuijinOptions options,
        IScopeRepository scopeRepository,
        CancellationToken cancellationToken)
    {
        var issuer = options.Issuer!.TrimEnd('/');

        var document = new DiscoveryDocument
        {
            Issuer = issuer,
            AuthorizationEndpoint = issuer + ZuijinEndpointPaths.Authorize,
            TokenEndpoint = issuer + ZuijinEndpointPaths.Token,
            UserInfoEndpoint = issuer + ZuijinEndpointPaths.UserInfo,
            JwksUri = issuer + ZuijinEndpointPaths.Jwks,
            RevocationEndpoint = issuer + ZuijinEndpointPaths.Revocation,
            IntrospectionEndpoint = issuer + ZuijinEndpointPaths.Introspection,
            DeviceAuthorizationEndpoint = issuer + ZuijinEndpointPaths.DeviceAuthorization,
            ScopesSupported = await GetActiveScopeNames(scopeRepository, cancellationToken),
            ResponseTypesSupported = [ResponseTypes.Code],
            GrantTypesSupported = GrantTypesSupported,
            SubjectTypesSupported = [SubjectTypePublic],
            IdTokenSigningAlgValuesSupported = [SigningAlgorithm],
            TokenEndpointAuthMethodsSupported = TokenEndpointAuthMethods,
            CodeChallengeMethodsSupported = SupportedCodeChallengeMethods
        };

        return Results.Json(document);
    }

    private static async Task<IResult> GetJsonWebKeySet(
        ISigningKeyService signingKeyService,
        CancellationToken cancellationToken)
    {
        var publicKeys = await signingKeyService.GetPublicKeys(cancellationToken);

        var document = new JsonWebKeySetDocument
        {
            Keys = publicKeys.Select(key => new JsonWebKeyDocument
            {
                KeyType = key.KeyType,
                Use = key.Use,
                KeyId = key.KeyId,
                Algorithm = key.Algorithm,
                Modulus = key.Modulus,
                Exponent = key.Exponent
            }).ToList()
        };

        return Results.Json(document);
    }

    private static async Task<IReadOnlyList<string>> GetActiveScopeNames(
        IScopeRepository scopeRepository,
        CancellationToken cancellationToken)
    {
        // Scopes are configuration-scale (dozens of rows), so a single page is enough.
        var total = await scopeRepository.GetCount(cancellationToken);
        if (total == 0)
        {
            return [];
        }

        var scopes = await scopeRepository.GetAll(1, total, cancellationToken);

        return scopes
            .Where(scope => scope.IsActive)
            .Select(scope => scope.Name)
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
