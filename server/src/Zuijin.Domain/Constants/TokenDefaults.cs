namespace Zuijin.Domain.Constants;

/// <summary>
/// Default lifetime and configuration values for OAuth 2.0 / OIDC tokens and security policies.
/// </summary>
public static class TokenDefaults
{
    /// <summary>Access token lifetime in seconds (1 hour).</summary>
    public const int AccessTokenLifetimeSeconds = 3600;

    /// <summary>Refresh token lifetime in seconds (30 days).</summary>
    public const int RefreshTokenLifetimeSeconds = 2592000;

    /// <summary>ID token lifetime in seconds (1 hour).</summary>
    public const int IdTokenLifetimeSeconds = 3600;

    /// <summary>Authorization code lifetime in seconds (5 minutes).</summary>
    public const int AuthorizationCodeLifetimeSeconds = 300;

    /// <summary>Device code lifetime in seconds (5 minutes).</summary>
    public const int DeviceCodeLifetimeSeconds = 300;

    /// <summary>Device code polling interval in seconds.</summary>
    public const int DeviceCodePollingIntervalSeconds = 5;

    /// <summary>Signing key rotation interval in days.</summary>
    public const int KeyRotationIntervalDays = 90;

    /// <summary>Maximum failed login attempts before lockout.</summary>
    public const int MaxFailedLoginAttempts = 5;

    /// <summary>Account lockout duration in minutes.</summary>
    public const int LockoutDurationMinutes = 15;
}
