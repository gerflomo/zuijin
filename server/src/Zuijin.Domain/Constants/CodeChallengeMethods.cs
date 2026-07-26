namespace Zuijin.Domain.Constants;

/// <summary>
/// PKCE code challenge methods (RFC 7636) accepted by Zuijin.
/// The "plain" method is deliberately not supported: it sends the code verifier
/// unhashed in the authorization request, which defeats the purpose of PKCE.
/// </summary>
public static class CodeChallengeMethods
{
    public const string S256 = "S256";
}
