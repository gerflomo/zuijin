using Zuijin.Application.Configuration;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;
using Zuijin.Domain.Services;

namespace Zuijin.Application.Features.Authorization;

/// <summary>
/// Validates authorization requests in two stages. The client and redirect URI are settled
/// first because everything after them may be reported by redirecting the user agent, and
/// redirecting to an unverified URI would turn this endpoint into an open redirector.
/// </summary>
public sealed class AuthorizeRequestValidator
{
    private readonly IClientRepository _clientRepository;
    private readonly ZuijinOptions _options;

    public AuthorizeRequestValidator(IClientRepository clientRepository, ZuijinOptions options)
    {
        _clientRepository = clientRepository;
        _options = options;
    }

    /// <summary>
    /// Stage one. Failures here must be shown to the user, never redirected.
    /// </summary>
    public async Task<(Client Client, string RedirectUri)> ValidateClientAndRedirectUri(
        AuthorizeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new OAuthException(OAuthError.InvalidRequest("The client_id parameter is required."));
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            throw new OAuthException(OAuthError.InvalidRequest("The redirect_uri parameter is required."));
        }

        var client = await _clientRepository.GetByClientId(request.ClientId, cancellationToken)
            ?? throw new OAuthException(OAuthError.InvalidClient("Unknown client."));

        ClientValidator.ValidateActive(client);
        ClientValidator.ValidateRedirectUri(client, request.RedirectUri);
        ValidateRedirectUriTransport(request.RedirectUri);

        return (client, request.RedirectUri);
    }

    /// <summary>
    /// Stage two. Failures here are reported to the client by redirecting with an error.
    /// </summary>
    public ValidatedAuthorizeRequest Validate(AuthorizeRequest request, Client client, string redirectUri)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(client);

        AuthorizationValidator.ValidateResponseType(request.ResponseType ?? string.Empty);
        ClientValidator.ValidateGrantType(client, GrantTypes.AuthorizationCode);

        var scopes = ParseScopes(request.Scope);
        if (scopes.Count == 0)
        {
            throw new OAuthException(OAuthError.InvalidScope("The scope parameter is required."));
        }

        ClientValidator.ValidateScopes(client, scopes);
        ValidateScopesAreActive(client, scopes);
        ValidatePkce(client, request);

        return new ValidatedAuthorizeRequest
        {
            Client = client,
            RedirectUri = redirectUri,
            Scopes = scopes,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Nonce = request.Nonce
        };
    }

    private void ValidateRedirectUriTransport(string redirectUri)
    {
        if (_options.RequireHttpsRedirectUris != true)
        {
            return;
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            throw new OAuthException(OAuthError.InvalidRequest("The redirect_uri is not an absolute URI."));
        }

        // Loopback stays allowed: native and desktop clients redirect to 127.0.0.1.
        var isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        if (!isHttps && !uri.IsLoopback)
        {
            throw new OAuthException(OAuthError.InvalidRequest("The redirect_uri must use HTTPS."));
        }
    }

    private void ValidatePkce(Client client, AuthorizeRequest request)
    {
        var pkceRequired = client.RequirePkce || _options.RequirePkce == true;

        if (pkceRequired && string.IsNullOrEmpty(request.CodeChallenge))
        {
            throw new OAuthException(OAuthError.InvalidRequest("PKCE is required: code_challenge is missing."));
        }

        if (string.IsNullOrEmpty(request.CodeChallenge))
        {
            return;
        }

        if (!string.Equals(request.CodeChallengeMethod, CodeChallengeMethods.S256, StringComparison.Ordinal))
        {
            throw new OAuthException(OAuthError.InvalidRequest(
                $"The only supported code_challenge_method is {CodeChallengeMethods.S256}."));
        }
    }

    private static void ValidateScopesAreActive(Client client, IReadOnlyList<string> scopes)
    {
        var activeScopes = client.Scopes
            .Where(clientScope => clientScope.Scope.IsActive)
            .Select(clientScope => clientScope.Scope.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var scope in scopes)
        {
            if (!activeScopes.Contains(scope))
            {
                throw new OAuthException(OAuthError.InvalidScope($"The scope '{scope}' is disabled."));
            }
        }
    }

    private static IReadOnlyList<string> ParseScopes(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
