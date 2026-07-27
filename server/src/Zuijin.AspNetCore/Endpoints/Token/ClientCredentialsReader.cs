using System.Text;
using Microsoft.AspNetCore.Http;
using Zuijin.Domain.Errors;

namespace Zuijin.AspNetCore.Endpoints.Token;

/// <summary>
/// Reads client credentials from either supported authentication method:
/// HTTP Basic (client_secret_basic) or request body (client_secret_post).
/// </summary>
public static class ClientCredentialsReader
{
    private const string BasicPrefix = "Basic ";
    private const string ClientIdParameter = "client_id";
    private const string ClientSecretParameter = "client_secret";

    public static (string ClientId, string? ClientSecret) Read(HttpRequest request, IFormCollection form)
    {
        var authorizationHeader = request.Headers.Authorization.ToString();
        var formClientId = form[ClientIdParameter].ToString();

        if (authorizationHeader.StartsWith(BasicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // RFC 6749 section 2.3: a client must not use more than one authentication method.
            if (!string.IsNullOrEmpty(formClientId))
            {
                throw new OAuthException(OAuthError.InvalidRequest(
                    "The client must authenticate with a single method."));
            }

            return ReadBasicHeader(authorizationHeader);
        }

        if (string.IsNullOrEmpty(formClientId))
        {
            throw new OAuthException(OAuthError.InvalidClient("Client authentication is required."));
        }

        var formSecret = form[ClientSecretParameter].ToString();

        return (formClientId, string.IsNullOrEmpty(formSecret) ? null : formSecret);
    }

    private static (string ClientId, string? ClientSecret) ReadBasicHeader(string authorizationHeader)
    {
        var encoded = authorizationHeader[BasicPrefix.Length..].Trim();

        Span<byte> decoded = new byte[encoded.Length];
        if (!Convert.TryFromBase64String(encoded, decoded, out var bytesWritten))
        {
            throw new OAuthException(OAuthError.InvalidClient("The Basic authorization header is malformed."));
        }

        var credentials = Encoding.UTF8.GetString(decoded[..bytesWritten]);
        var separatorIndex = credentials.IndexOf(':', StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            throw new OAuthException(OAuthError.InvalidClient("The Basic authorization header is malformed."));
        }

        // Both halves are form-urlencoded before being base64 encoded.
        var clientId = Uri.UnescapeDataString(credentials[..separatorIndex]);
        var clientSecret = Uri.UnescapeDataString(credentials[(separatorIndex + 1)..]);

        if (string.IsNullOrEmpty(clientId))
        {
            throw new OAuthException(OAuthError.InvalidClient("Client authentication is required."));
        }

        return (clientId, string.IsNullOrEmpty(clientSecret) ? null : clientSecret);
    }
}
