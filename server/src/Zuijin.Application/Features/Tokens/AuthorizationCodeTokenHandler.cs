using Zuijin.Application.Abstractions;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;
using Zuijin.Domain.Services;

namespace Zuijin.Application.Features.Tokens;

/// <summary>
/// Exchanges an authorization code for the token set it stands for (RFC 6749 section 4.1.3).
/// </summary>
public sealed class AuthorizationCodeTokenHandler
{
    private readonly ClientAuthenticator _clientAuthenticator;
    private readonly IAuthorizationGrantRepository _grantRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserTokenIssuer _tokenIssuer;
    private readonly ISecretHasher _secretHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AuthorizationCodeTokenHandler(
        ClientAuthenticator clientAuthenticator,
        IAuthorizationGrantRepository grantRepository,
        ITokenRepository tokenRepository,
        IUserRepository userRepository,
        UserTokenIssuer tokenIssuer,
        ISecretHasher secretHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _clientAuthenticator = clientAuthenticator;
        _grantRepository = grantRepository;
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _tokenIssuer = tokenIssuer;
        _secretHasher = secretHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<TokenIssuanceResult> Handle(
        AuthorizationCodeTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await _clientAuthenticator.Authenticate(
            request.ClientId, request.ClientSecret, requireConfidential: false, cancellationToken);

        ClientValidator.ValidateActive(client);
        ClientValidator.ValidateGrantType(client, GrantTypes.AuthorizationCode);

        if (string.IsNullOrEmpty(request.Code))
        {
            throw new OAuthException(OAuthError.InvalidRequest("The code parameter is required."));
        }

        var grant = await _grantRepository.GetByCodeHash(_secretHasher.Hash(request.Code), cancellationToken)
            ?? throw new OAuthException(OAuthError.InvalidGrant("The authorization code is not valid."));

        // A code issued to another client must never be redeemable here.
        if (grant.ClientId != client.Id)
        {
            throw new OAuthException(OAuthError.InvalidGrant("The authorization code is not valid."));
        }

        AuthorizationValidator.ValidateNotExpired(grant, _clock.UtcNow);
        AuthorizationValidator.ValidateRedirectUri(grant, request.RedirectUri ?? string.Empty);
        AuthorizationValidator.ValidateCodeChallenge(grant, request.CodeVerifier ?? string.Empty);

        await ConsumeOrTreatAsReplay(grant.Id, grant.IsUsed, cancellationToken);

        var user = await _userRepository.GetById(grant.UserId, cancellationToken)
            ?? throw new OAuthException(OAuthError.InvalidGrant("The authorization code is not valid."));

        UserValidator.ValidateCanAuthenticate(user, _clock.UtcNow);

        var scopes = grant.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var result = await _tokenIssuer.Issue(
            client, user, scopes, grant.Nonce, grant.Id, parentTokenId: null, cancellationToken);

        await _unitOfWork.SaveChanges(cancellationToken);

        return result;
    }

    /// <summary>
    /// Burns the code, and treats a second presentation as theft: everything the code
    /// produced is revoked, as RFC 6749 section 4.1.2 recommends.
    /// </summary>
    private async Task ConsumeOrTreatAsReplay(long grantId, bool alreadyUsed, CancellationToken cancellationToken)
    {
        if (!alreadyUsed && await _grantRepository.TryConsume(grantId, cancellationToken))
        {
            return;
        }

        await _tokenRepository.RevokeByAuthorizationGrantId(grantId, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);

        throw new OAuthException(OAuthError.InvalidGrant("The authorization code has already been used."));
    }
}

/// <summary>
/// A token request using the authorization code grant.
/// </summary>
public sealed class AuthorizationCodeTokenRequest
{
    public required string ClientId { get; init; }
    public required string? ClientSecret { get; init; }
    public required string? Code { get; init; }
    public required string? RedirectUri { get; init; }
    public string? CodeVerifier { get; init; }
}
