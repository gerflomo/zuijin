namespace Zuijin.Domain.Errors;

/// <summary>
/// Maps the domain's validation codes onto the closed set of RFC 6749 error codes.
/// Domain codes are deliberately more specific than the protocol allows, so this is
/// the single place where that detail is collapsed into what a client is allowed to see.
/// </summary>
public static class OAuthErrorTranslator
{
    public static OAuthError Translate(DomainException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Code switch
        {
            "client_disabled" => OAuthError.InvalidClient(exception.Message),
            "client_secret_required" => OAuthError.InvalidClient(exception.Message),

            "unauthorized_grant_type" => OAuthError.UnauthorizedClient(exception.Message),

            "invalid_scope" => OAuthError.InvalidScope(exception.Message),

            "code_already_used" => OAuthError.InvalidGrant(exception.Message),
            "code_expired" => OAuthError.InvalidGrant(exception.Message),
            "invalid_code_verifier" => OAuthError.InvalidGrant(exception.Message),
            "redirect_uri_mismatch" => OAuthError.InvalidGrant(exception.Message),

            "invalid_redirect_uri" => OAuthError.InvalidRequest(exception.Message),
            "pkce_required" => OAuthError.InvalidRequest(exception.Message),
            "invalid_challenge_method" => OAuthError.InvalidRequest(exception.Message),

            "unsupported_response_type" => OAuthError.UnsupportedResponseType(exception.Message),

            _ => OAuthError.ServerError()
        };
    }
}
