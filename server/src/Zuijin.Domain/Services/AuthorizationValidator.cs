using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Errors;

namespace Zuijin.Domain.Services;

public static class AuthorizationValidator
{
    /// <summary>
    /// Single use is deliberately not checked here: reading <see cref="AuthorizationGrant.IsUsed"/>
    /// and acting on it is a race two concurrent redemptions can both win. That guarantee belongs
    /// to the atomic conditional update in IAuthorizationGrantRepository.TryConsume.
    /// </summary>
    public static void ValidateNotExpired(AuthorizationGrant grant, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(grant);

        if (grant.ExpiresAt <= now)
        {
            throw new DomainException("code_expired", "The authorization code has expired.");
        }
    }

    public static void ValidateCodeChallenge(AuthorizationGrant grant, string codeVerifier)
    {
        if (string.IsNullOrEmpty(grant.CodeChallenge))
        {
            return;
        }

        if (string.IsNullOrEmpty(codeVerifier))
        {
            throw new DomainException("pkce_required", "Code verifier is required.");
        }

        var isValid = grant.CodeChallengeMethod switch
        {
            CodeChallengeMethods.S256 => ValidateS256Challenge(grant.CodeChallenge, codeVerifier),
            _ => throw new DomainException("invalid_challenge_method", $"Unsupported code challenge method: {grant.CodeChallengeMethod}")
        };

        if (!isValid)
        {
            throw new DomainException("invalid_code_verifier", "The code verifier does not match the code challenge.");
        }
    }

    public static void ValidateRedirectUri(AuthorizationGrant grant, string redirectUri)
    {
        if (!string.Equals(grant.RedirectUri, redirectUri, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("redirect_uri_mismatch", "The redirect URI does not match the one used in the authorization request.");
        }
    }

    public static void ValidateResponseType(string responseType)
    {
        if (!string.Equals(responseType, ResponseTypes.Code, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("unsupported_response_type", $"Response type '{responseType}' is not supported.");
        }
    }

    private static bool ValidateS256Challenge(string codeChallenge, string codeVerifier)
    {
        var challengeBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        var computed = Base64UrlEncode(challengeBytes);
        return string.Equals(codeChallenge, computed, StringComparison.Ordinal);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
