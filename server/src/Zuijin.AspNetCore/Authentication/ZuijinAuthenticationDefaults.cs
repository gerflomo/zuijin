namespace Zuijin.AspNetCore.Authentication;

/// <summary>
/// Zuijin signs the end user in under its own scheme rather than the host's default, so an
/// application that embeds Zuijin keeps its own authentication untouched.
/// </summary>
public static class ZuijinAuthenticationDefaults
{
    public const string SessionScheme = "Zuijin.Session";
    public const string SessionCookieName = "zuijin.session";

    /// <summary>Claim carrying the user's stable subject identifier in the session cookie.</summary>
    public const string SubjectClaimType = "sub";
}
