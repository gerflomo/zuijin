using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
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

    private readonly IClientRepository _clientRepository;
    private readonly IApiResourceRepository _apiResourceRepository;
    private readonly ISecretHasher _secretHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ZuijinOptions _options;

    public ClientCredentialsTokenHandler(
        IClientRepository clientRepository,
        IApiResourceRepository apiResourceRepository,
        ISecretHasher secretHasher,
        ITokenGenerator tokenGenerator,
        ZuijinOptions options)
    {
        _clientRepository = clientRepository;
        _apiResourceRepository = apiResourceRepository;
        _secretHasher = secretHasher;
        _tokenGenerator = tokenGenerator;
        _options = options;
    }

    public async Task<TokenIssuanceResult> Handle(
        ClientCredentialsTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await Authenticate(request, cancellationToken);

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

    /// <summary>
    /// Resolves and authenticates the client. Credential failures deliberately return the
    /// same error whether the client is unknown or the secret is wrong.
    /// </summary>
    private async Task<Client> Authenticate(ClientCredentialsTokenRequest request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByClientId(request.ClientId, cancellationToken)
            ?? throw new OAuthException(OAuthError.InvalidClient("Client authentication failed."));

        if (client.Type != ClientType.Confidential)
        {
            throw new OAuthException(OAuthError.UnauthorizedClient(
                "The client credentials grant requires a confidential client."));
        }

        if (string.IsNullOrEmpty(request.ClientSecret)
            || string.IsNullOrEmpty(client.SecretHash)
            || !_secretHasher.Verify(request.ClientSecret, client.SecretHash))
        {
            throw new OAuthException(OAuthError.InvalidClient("Client authentication failed."));
        }

        return client;
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
