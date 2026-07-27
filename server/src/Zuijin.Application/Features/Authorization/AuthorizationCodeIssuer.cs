using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Features.Authorization;

/// <summary>
/// Mints an authorization code and records the grant it stands for.
/// Only the hash is stored: the plaintext code exists solely in the redirect back to the client.
/// </summary>
public sealed class AuthorizationCodeIssuer
{
    private readonly IAuthorizationGrantRepository _grantRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ISecretHasher _secretHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ZuijinOptions _options;

    public AuthorizationCodeIssuer(
        IAuthorizationGrantRepository grantRepository,
        ITokenGenerator tokenGenerator,
        ISecretHasher secretHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        ZuijinOptions options)
    {
        _grantRepository = grantRepository;
        _tokenGenerator = tokenGenerator;
        _secretHasher = secretHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options;
    }

    public async Task<string> Issue(
        ValidatedAuthorizeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = _tokenGenerator.GenerateAuthorizationCode();

        await _grantRepository.Add(new AuthorizationGrant
        {
            CodeHash = _secretHasher.Hash(code),
            ClientId = request.Client.Id,
            UserId = userId,
            RedirectUri = request.RedirectUri,
            Scopes = string.Join(' ', request.Scopes),
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Nonce = request.Nonce,
            ExpiresAt = _clock.UtcNow.AddSeconds(_options.AuthorizationCodeLifetime!.Value)
        }, cancellationToken);

        await _unitOfWork.SaveChanges(cancellationToken);

        return code;
    }
}
