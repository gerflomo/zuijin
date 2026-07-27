using Zuijin.Application.Abstractions;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;
using Zuijin.Domain.Services;

namespace Zuijin.Application.Features.Tokens;

/// <summary>
/// Exchanges a refresh token for a fresh token set (RFC 6749 section 6).
/// Every refresh rotates: the presented token is revoked and replaced, so a stolen copy
/// stops working as soon as either party uses it.
/// </summary>
public sealed class RefreshTokenGrantHandler
{
    private readonly ClientAuthenticator _clientAuthenticator;
    private readonly ITokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserTokenIssuer _tokenIssuer;
    private readonly ISecretHasher _secretHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RefreshTokenGrantHandler(
        ClientAuthenticator clientAuthenticator,
        ITokenRepository tokenRepository,
        IUserRepository userRepository,
        UserTokenIssuer tokenIssuer,
        ISecretHasher secretHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _clientAuthenticator = clientAuthenticator;
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _tokenIssuer = tokenIssuer;
        _secretHasher = secretHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<TokenIssuanceResult> Handle(
        RefreshTokenGrantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await _clientAuthenticator.Authenticate(
            request.ClientId, request.ClientSecret, requireConfidential: false, cancellationToken);

        ClientValidator.ValidateActive(client);
        ClientValidator.ValidateGrantType(client, GrantTypes.RefreshToken);

        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw new OAuthException(OAuthError.InvalidRequest("The refresh_token parameter is required."));
        }

        var storedToken = await _tokenRepository.GetByHash(_secretHasher.Hash(request.RefreshToken), cancellationToken);
        var token = await ValidateStoredToken(storedToken, client, cancellationToken);

        var user = await _userRepository.GetById(token.UserId!.Value, cancellationToken)
            ?? throw new OAuthException(OAuthError.InvalidGrant("The refresh token is not valid."));

        UserValidator.ValidateCanAuthenticate(user, _clock.UtcNow);

        var scopes = ResolveScopes(token, request.RequestedScopes);

        // Rotation: the presented token dies here, whether or not the caller is its owner.
        token.IsRevoked = true;
        await _tokenRepository.Update(token, cancellationToken);

        var result = await _tokenIssuer.Issue(
            client, user, scopes, nonce: null, token.AuthorizationGrantId, token.Id, cancellationToken);

        await _unitOfWork.SaveChanges(cancellationToken);

        return result;
    }

    private async Task<Token> ValidateStoredToken(Token? token, Client client, CancellationToken cancellationToken)
    {
        if (token is null || token.Type != TokenType.Refresh || token.UserId is null)
        {
            throw new OAuthException(OAuthError.InvalidGrant("The refresh token is not valid."));
        }

        if (token.ClientId != client.Id)
        {
            throw new OAuthException(OAuthError.InvalidGrant("The refresh token is not valid."));
        }

        // A token that was already rotated away is being replayed. Assume the chain leaked
        // and revoke everything descended from the same authorization.
        if (token.IsRevoked)
        {
            if (token.AuthorizationGrantId is not null)
            {
                await _tokenRepository.RevokeByAuthorizationGrantId(token.AuthorizationGrantId.Value, cancellationToken);
            }
            else
            {
                await _tokenRepository.RevokeByUserId(token.UserId.Value, cancellationToken);
            }

            await _unitOfWork.SaveChanges(cancellationToken);

            throw new OAuthException(OAuthError.InvalidGrant("The refresh token is not valid."));
        }

        if (token.ExpiresAt <= _clock.UtcNow)
        {
            throw new OAuthException(OAuthError.InvalidGrant("The refresh token has expired."));
        }

        return token;
    }

    /// <summary>
    /// RFC 6749 section 6 lets a client narrow the scope on refresh, never widen it.
    /// </summary>
    private static IReadOnlyList<string> ResolveScopes(Token token, IReadOnlyList<string> requestedScopes)
    {
        var originalScopes = token.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (requestedScopes.Count == 0)
        {
            return originalScopes;
        }

        var originalLookup = originalScopes.ToHashSet(StringComparer.Ordinal);
        foreach (var scope in requestedScopes)
        {
            if (!originalLookup.Contains(scope))
            {
                throw new OAuthException(OAuthError.InvalidScope(
                    $"The scope '{scope}' was not part of the original grant."));
            }
        }

        return requestedScopes;
    }
}

/// <summary>
/// A token request using the refresh token grant.
/// </summary>
public sealed class RefreshTokenGrantRequest
{
    public required string ClientId { get; init; }
    public required string? ClientSecret { get; init; }
    public required string? RefreshToken { get; init; }
    public IReadOnlyList<string> RequestedScopes { get; init; } = [];
}
