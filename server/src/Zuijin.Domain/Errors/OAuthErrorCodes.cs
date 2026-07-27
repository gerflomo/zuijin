namespace Zuijin.Domain.Errors;

/// <summary>
/// The closed set of error codes a client may receive, as defined by
/// RFC 6749 section 5.2 and RFC 8628 section 3.5.
/// </summary>
public static class OAuthErrorCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string InvalidClient = "invalid_client";
    public const string InvalidGrant = "invalid_grant";
    public const string UnauthorizedClient = "unauthorized_client";
    public const string UnsupportedGrantType = "unsupported_grant_type";
    public const string InvalidScope = "invalid_scope";
    public const string UnsupportedResponseType = "unsupported_response_type";
    public const string ServerError = "server_error";
    public const string AccessDenied = "access_denied";

    public const string AuthorizationPending = "authorization_pending";
    public const string SlowDown = "slow_down";
    public const string ExpiredToken = "expired_token";
}
