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
        new("invalid_request", description);

    public static OAuthError InvalidClient(string? description = null) =>
        new("invalid_client", description);

    public static OAuthError InvalidGrant(string? description = null) =>
        new("invalid_grant", description);

    public static OAuthError UnauthorizedClient(string? description = null) =>
        new("unauthorized_client", description);

    public static OAuthError UnsupportedGrantType(string? description = null) =>
        new("unsupported_grant_type", description);

    public static OAuthError InvalidScope(string? description = null) =>
        new("invalid_scope", description);

    public static OAuthError UnsupportedResponseType(string? description = null) =>
        new("unsupported_response_type", description);

    public static OAuthError ServerError(string? description = null) =>
        new("server_error", description);

    public static OAuthError AccessDenied(string? description = null) =>
        new("access_denied", description);

    // RFC 8628 device code errors
    public static OAuthError AuthorizationPending(string? description = null) =>
        new("authorization_pending", description);

    public static OAuthError SlowDown(string? description = null) =>
        new("slow_down", description);

    public static OAuthError ExpiredToken(string? description = null) =>
        new("expired_token", description);
}
