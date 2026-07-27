namespace Zuijin.AspNetCore.Endpoints;

/// <summary>
/// Canonical paths for the OAuth 2.0 / OIDC protocol endpoints.
/// Kept in one place so the discovery document and the route registrations cannot drift apart.
/// </summary>
public static class ZuijinEndpointPaths
{
    public const string Discovery = "/.well-known/openid-configuration";
    public const string Jwks = "/.well-known/jwks";
    public const string Authorize = "/connect/authorize";
    public const string Token = "/connect/token";
    public const string UserInfo = "/connect/userinfo";
    public const string Revocation = "/connect/revoke";
    public const string Introspection = "/connect/introspect";
    public const string DeviceAuthorization = "/connect/deviceauthorize";

    /// <summary>Interactive pages. Not part of the OAuth protocol surface.</summary>
    public const string Login = "/account/login";

    public const string Consent = "/account/consent";
}
