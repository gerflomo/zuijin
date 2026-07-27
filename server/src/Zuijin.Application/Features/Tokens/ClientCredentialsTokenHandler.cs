using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;
using Zuijin.Domain.Services;

namespace Zuijin.Application.Features.Tokens;

/// <summary>
/// Authenticates a confidential client and issues an access token on its own behalf.
/// No user is involved, so no ID token and no refresh token are issued. Nothing is
/// persisted either: the access token is a self-contained JWT and only opaque tokens,
/// which must be looked up to be validated, earn a row in the database.
/// </summary>
public sealed class ClientCredentialsTokenHandler
{
    private const string BearerTokenType = "Bearer";
    private const string ClientIdClaim = "client_id";

    private readonly ClientAuthenticator _clientAuthenticator;
    private readonly IApiResourceRepository _apiResourceRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ZuijinOptions _options;

    public ClientCredentialsTokenHandler(
        ClientAuthenticator clientAuthenticator,
        IApiResourceRepository apiResourceRepository,
        ITokenGenerator tokenGenerator,
        ZuijinOptions options)
    {
        _clientAuthenticator = clientAuthenticator;
        _apiResourceRepository = apiResourceRepository;
        _tokenGenerator = tokenGenerator;
        _options = options;
    }

    public async Task<TokenIssuanceResult> Handle(
        ClientCredentialsTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await _clientAuthenticator.Authenticate(
            request.ClientId, request.ClientSecret, requireConfidential: true, cancellationToken);

        ClientValidator.ValidateActive(client);
        ClientValidator.ValidateGrantType(client, GrantTypes.ClientCredentials);

        var scopes = ResolveScopes(client, request.RequestedScopes);
        var audiences = await _apiResourceRepository.GetAudiencesForScopes(scopes, cancellationToken);
        var lifetime = TimeSpan.FromSeconds(client.AccessTokenLifetime);

        var accessToken = await _tokenGenerator.GenerateAccessToken(new TokenGenerationRequest
        {
            Issuer = _options.Issuer!,
            // With no resource owner, the client itself is the subject of the token.
            Subject = client.ClientId,
            Audiences = audiences,
            Scopes = scopes,
            Claims = new Dictionary<string, object> { [ClientIdClaim] = client.ClientId },
            Lifetime = lifetime
        }, cancellationToken);

        return new TokenIssuanceResult
        {
            AccessToken = accessToken,
            TokenType = BearerTokenType,
            ExpiresIn = client.AccessTokenLifetime,
            Scope = string.Join(' ', scopes)
        };
    }

    private static IReadOnlyList<string> ResolveScopes(Client client, IReadOnlyList<string> requestedScopes)
    {
        var activeScopes = client.Scopes
            .Where(clientScope => clientScope.Scope.IsActive)
            .Select(clientScope => clientScope.Scope.Name)
            .ToList();

        if (requestedScopes.Count == 0)
        {
            // "openid" promises an ID token describing a user, and this grant has no user,
            // so it is simply not part of what a default grant can deliver.
            return activeScopes
                .Where(scope => !string.Equals(scope, StandardScopes.OpenId, StringComparison.Ordinal))
                .ToList();
        }

        // Asking for it explicitly is a misunderstanding worth failing loudly on.
        if (requestedScopes.Contains(StandardScopes.OpenId, StringComparer.Ordinal))
        {
            throw new OAuthException(OAuthError.InvalidScope(
                "The 'openid' scope requires an authenticated user and cannot be used with the client credentials grant."));
        }

        ClientValidator.ValidateScopes(client, requestedScopes);

        var activeLookup = activeScopes.ToHashSet(StringComparer.Ordinal);
        foreach (var scope in requestedScopes)
        {
            if (!activeLookup.Contains(scope))
            {
                throw new OAuthException(OAuthError.InvalidScope($"The scope '{scope}' is disabled."));
            }
        }

        return requestedScopes;
    }
}
