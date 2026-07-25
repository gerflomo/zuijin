using Zuijin.Domain.Constants;

namespace Zuijin.Application.Configuration;

/// <summary>
/// Configuration options for the Zuijin identity server.
/// </summary>
public class ZuijinOptions
{
    public string Issuer { get; set; } = string.Empty;
    public bool RequirePkce { get; set; } = true;
    public bool RequireHttpsRedirectUris { get; set; } = true;
    public int DefaultAccessTokenLifetime { get; set; } = TokenDefaults.AccessTokenLifetimeSeconds;
    public int DefaultRefreshTokenLifetime { get; set; } = TokenDefaults.RefreshTokenLifetimeSeconds;
    public int DefaultIdTokenLifetime { get; set; } = TokenDefaults.IdTokenLifetimeSeconds;
    public int AuthorizationCodeLifetime { get; set; } = TokenDefaults.AuthorizationCodeLifetimeSeconds;
    public int DeviceCodeLifetime { get; set; } = TokenDefaults.DeviceCodeLifetimeSeconds;
    public int DeviceCodePollingInterval { get; set; } = TokenDefaults.DeviceCodePollingIntervalSeconds;
    public int KeyRotationIntervalDays { get; set; } = TokenDefaults.KeyRotationIntervalDays;
    public int MaxFailedLoginAttempts { get; set; } = TokenDefaults.MaxFailedLoginAttempts;
    public int LockoutDurationMinutes { get; set; } = TokenDefaults.LockoutDurationMinutes;
}
