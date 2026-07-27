using Zuijin.Application.Abstractions;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Features.Tokens;

/// <summary>
/// Resolves and authenticates the client behind a token request.
/// Confidential clients must prove possession of their secret; public clients cannot hold
/// one, so their requests are protected by PKCE instead.
/// </summary>
public sealed class ClientAuthenticator
{
    private readonly IClientRepository _clientRepository;
    private readonly ISecretHasher _secretHasher;

    public ClientAuthenticator(IClientRepository clientRepository, ISecretHasher secretHasher)
    {
        _clientRepository = clientRepository;
        _secretHasher = secretHasher;
    }

    /// <param name="requireConfidential">
    /// True for grants that have no other proof of client identity, such as client credentials.
    /// </param>
    public async Task<Client> Authenticate(
        string clientId,
        string? clientSecret,
        bool requireConfidential,
        CancellationToken cancellationToken = default)
    {
        // The same error for an unknown client and a bad secret, so the response
        // cannot be used to enumerate registered client identifiers.
        var client = await _clientRepository.GetByClientId(clientId, cancellationToken)
            ?? throw new OAuthException(OAuthError.InvalidClient("Client authentication failed."));

        if (requireConfidential && client.Type != ClientType.Confidential)
        {
            throw new OAuthException(OAuthError.UnauthorizedClient(
                "This grant requires a confidential client."));
        }

        if (client.Type == ClientType.Confidential && !IsSecretValid(client, clientSecret))
        {
            throw new OAuthException(OAuthError.InvalidClient("Client authentication failed."));
        }

        return client;
    }

    private bool IsSecretValid(Client client, string? clientSecret)
    {
        return !string.IsNullOrEmpty(clientSecret)
            && !string.IsNullOrEmpty(client.SecretHash)
            && _secretHasher.Verify(clientSecret, client.SecretHash);
    }
}
