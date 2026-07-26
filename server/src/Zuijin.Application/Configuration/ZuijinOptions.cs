namespace Zuijin.Application.Configuration;

/// <summary>
/// Configuration options for the Zuijin identity server.
/// All values are required: properties are nullable so that a missing configuration key
/// can be detected and fail validation at startup instead of silently using a default.
/// </summary>
public class ZuijinOptions
{
    /// <summary>Configuration section name expected in the host configuration.</summary>
    public const string SectionName = "Zuijin";

    public string? Issuer { get; set; }

    /// <summary>
    /// Base64-encoded 256-bit master key used to encrypt RSA private keys at rest.
    /// Must come from a secret store (user secrets locally, a key vault in the cloud),
    /// never from appsettings.json.
    /// </summary>
    public string? SigningKeyMasterKey { get; set; }

    public bool? RequirePkce { get; set; }
    public bool? RequireHttpsRedirectUris { get; set; }
    public int? DefaultAccessTokenLifetime { get; set; }
    public int? DefaultRefreshTokenLifetime { get; set; }
    public int? DefaultIdTokenLifetime { get; set; }
    public int? AuthorizationCodeLifetime { get; set; }
    public int? DeviceCodeLifetime { get; set; }
    public int? DeviceCodePollingInterval { get; set; }
    public int? KeyRotationIntervalDays { get; set; }
    public int? MaxFailedLoginAttempts { get; set; }
    public int? LockoutDurationMinutes { get; set; }
}
