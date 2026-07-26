using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Domain.Errors;

namespace Zuijin.Domain.Services;

/// <summary>
/// Validates OAuth 2.0 client configuration and authorization requests.
/// </summary>
public static class ClientValidator
{
    public static void ValidateRedirectUri(Client client, string redirectUri)
    {
        var isRegistered = client.RedirectUris
            .FirstOrDefault(r => r.Type == RedirectUriType.Redirect &&
                                  string.Equals(r.Uri, redirectUri, StringComparison.OrdinalIgnoreCase)) != null;

        if (!isRegistered)
        {
            throw new DomainException("invalid_redirect_uri", "The redirect URI is not registered for this client.");
        }
    }

    public static void ValidateGrantType(Client client, string grantType)
    {
        var isAuthorized = client.GrantTypes
            .FirstOrDefault(g => string.Equals(g.GrantType, grantType, StringComparison.OrdinalIgnoreCase)) != null;

        if (!isAuthorized)
        {
            throw new DomainException("unauthorized_grant_type", $"The client is not authorized for grant type '{grantType}'.");
        }
    }

    public static void ValidateScopes(Client client, IEnumerable<string> requestedScopes)
    {
        var allowedScopes = client.Scopes.Select(s => s.Scope.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var scope in requestedScopes)
        {
            if (!allowedScopes.Contains(scope))
            {
                throw new DomainException("invalid_scope", $"The client is not authorized for scope '{scope}'.");
            }
        }
    }

    public static void ValidateActive(Client client)
    {
        if (!client.IsActive)
        {
            throw new DomainException("client_disabled", "The client is disabled.");
        }
    }

    public static void ValidatePkceRequired(Client client, string? codeChallenge)
    {
        if (client.RequirePkce && string.IsNullOrEmpty(codeChallenge))
        {
            throw new DomainException("pkce_required", "PKCE is required for this client.");
        }
    }

    public static void ValidateConfidentialClient(Client client, string? clientSecret)
    {
        if (client.Type == ClientType.Confidential && string.IsNullOrEmpty(clientSecret))
        {
            throw new DomainException("client_secret_required", "Client secret is required for confidential clients.");
        }
    }
}
