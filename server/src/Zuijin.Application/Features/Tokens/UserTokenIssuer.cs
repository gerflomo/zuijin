using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Application.Features.Users;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Features.Tokens;

/// <summary>
/// Issues the token set that represents a user's delegated authorization.
/// Shared by the authorization code and refresh token grants so both produce identical tokens.
/// </summary>
public sealed class UserTokenIssuer
{
    private const string BearerTokenType = "Bearer";
    private const string ClientIdClaim = "client_id";

    private readonly IApiResourceRepository _apiResourceRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly UserClaimsResolver _claimsResolver;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ISecretHasher _secretHasher;
    private readonly IClock _clock;
    private readonly ZuijinOptions _options;

    public UserTokenIssuer(
        IApiResourceRepository apiResourceRepository,
        ITokenRepository tokenRepository,
        UserClaimsResolver claimsResolver,
        ITokenGenerator tokenGenerator,
        ISecretHasher secretHasher,
        IClock clock,
        ZuijinOptions options)
    {
        _apiResourceRepository = apiResourceRepository;
        _tokenRepository = tokenRepository;
        _claimsResolver = claimsResolver;
        _tokenGenerator = tokenGenerator;
        _secretHasher = secretHasher;
        _clock = clock;
        _options = options;
    }

    public async Task<TokenIssuanceResult> Issue(
        Client client,
        User user,
        IReadOnlyList<string> scopes,
        string? nonce,
        long? authorizationGrantId,
        long? parentTokenId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(user);

        var accessToken = await IssueAccessToken(client, user, scopes, cancellationToken);
        var idToken = await IssueIdToken(client, user, scopes, nonce, cancellationToken);
        var refreshToken = await IssueRefreshToken(client, user, scopes, authorizationGrantId, parentTokenId, cancellationToken);

        return new TokenIssuanceResult
        {
            AccessToken = accessToken,
            TokenType = BearerTokenType,
            ExpiresIn = client.AccessTokenLifetime,
            Scope = string.Join(' ', scopes),
            IdToken = idToken,
            RefreshToken = refreshToken
        };
    }

    private async Task<string> IssueAccessToken(
        Client client,
        User user,
        IReadOnlyList<string> scopes,
        CancellationToken cancellationToken)
    {
        var audiences = await _apiResourceRepository.GetAudiencesForScopes(scopes, cancellationToken);
        var claims = await _claimsResolver.ResolveAuthorizationClaims(user.Id, cancellationToken);
        claims[ClientIdClaim] = client.ClientId;

        return await _tokenGenerator.GenerateAccessToken(new TokenGenerationRequest
        {
            Issuer = _options.Issuer!,
            Subject = user.SubjectId,
            Audiences = audiences,
            Scopes = scopes,
            Claims = claims,
            Lifetime = TimeSpan.FromSeconds(client.AccessTokenLifetime)
        }, cancellationToken);
    }

    private async Task<string?> IssueIdToken(
        Client client,
        User user,
        IReadOnlyList<string> scopes,
        string? nonce,
        CancellationToken cancellationToken)
    {
        if (!scopes.Contains(StandardScopes.OpenId, StringComparer.Ordinal))
        {
            return null;
        }

        var claims = await _claimsResolver.ResolveIdentityClaims(user, scopes, cancellationToken);

        return await _tokenGenerator.GenerateIdToken(new TokenGenerationRequest
        {
            Issuer = _options.Issuer!,
            Subject = user.SubjectId,
            // An ID token is addressed to the client that requested it, not to an API.
            Audiences = [client.ClientId],
            Claims = claims,
            Nonce = nonce,
            Lifetime = TimeSpan.FromSeconds(client.IdTokenLifetime)
        }, cancellationToken);
    }

    private async Task<string?> IssueRefreshToken(
        Client client,
        User user,
        IReadOnlyList<string> scopes,
        long? authorizationGrantId,
        long? parentTokenId,
        CancellationToken cancellationToken)
    {
        var offlineAccessRequested = scopes.Contains(StandardScopes.OfflineAccess, StringComparer.Ordinal);
        if (!offlineAccessRequested || !client.AllowOfflineAccess)
        {
            return null;
        }

        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        // Opaque by design: it has to be looked up to be validated, which is what makes
        // revoking it actually stop future access.
        await _tokenRepository.Add(new Token
        {
            Hash = _secretHasher.Hash(refreshToken),
            Type = TokenType.Refresh,
            ClientId = client.Id,
            UserId = user.Id,
            Scopes = string.Join(' ', scopes),
            ExpiresAt = _clock.UtcNow.AddSeconds(client.RefreshTokenLifetime),
            AuthorizationGrantId = authorizationGrantId,
            ParentTokenId = parentTokenId
        }, cancellationToken);

        return refreshToken;
    }
}
