namespace Zuijin.Domain.Errors;

public class OAuthError
{
    public string Error { get; }
    public string? ErrorDescription { get; }

    private OAuthError(string error, string? errorDescription = null)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    // RFC 6749 error codes
    public static OAuthError InvalidRequest(string? description = null) =>
        new(OAuthErrorCodes.InvalidRequest, description);

    public static OAuthError InvalidClient(string? description = null) =>
        new(OAuthErrorCodes.InvalidClient, description);

    public static OAuthError InvalidGrant(string? description = null) =>
        new(OAuthErrorCodes.InvalidGrant, description);

    public static OAuthError UnauthorizedClient(string? description = null) =>
        new(OAuthErrorCodes.UnauthorizedClient, description);

    public static OAuthError UnsupportedGrantType(string? description = null) =>
        new(OAuthErrorCodes.UnsupportedGrantType, description);

    public static OAuthError InvalidScope(string? description = null) =>
        new(OAuthErrorCodes.InvalidScope, description);

    public static OAuthError UnsupportedResponseType(string? description = null) =>
        new(OAuthErrorCodes.UnsupportedResponseType, description);

    public static OAuthError ServerError(string? description = null) =>
        new(OAuthErrorCodes.ServerError, description);

    public static OAuthError AccessDenied(string? description = null) =>
        new(OAuthErrorCodes.AccessDenied, description);

    // RFC 8628 device code errors
    public static OAuthError AuthorizationPending(string? description = null) =>
        new(OAuthErrorCodes.AuthorizationPending, description);

    public static OAuthError SlowDown(string? description = null) =>
        new(OAuthErrorCodes.SlowDown, description);

    public static OAuthError ExpiredToken(string? description = null) =>
        new(OAuthErrorCodes.ExpiredToken, description);
}
