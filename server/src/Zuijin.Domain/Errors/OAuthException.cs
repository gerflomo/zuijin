namespace Zuijin.Domain.Errors;

/// <summary>
/// Raised when a request must be answered with an RFC 6749 error response.
/// Carries the protocol error verbatim so the transport layer only decides the status code.
/// </summary>
public class OAuthException : Exception
{
    public OAuthError Error { get; }

    public OAuthException(OAuthError error)
        : base(error.ErrorDescription ?? error.Error)
    {
        Error = error;
    }
}
