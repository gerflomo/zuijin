namespace Zuijin.Application.Configuration;

/// <summary>
/// Validates that every <see cref="ZuijinOptions"/> value is present and coherent.
/// Framework-free so it can be reused by any host (ASP.NET Core, tests, embedded).
/// </summary>
public static class ZuijinOptionsValidator
{
    /// <summary>
    /// Returns the list of validation errors; an empty list means the options are valid.
    /// </summary>
    public static IReadOnlyList<string> Validate(ZuijinOptions options)
    {
        var errors = new List<string>();

        ValidateIssuer(options.Issuer, errors);

        RequireValue(options.RequirePkce, nameof(ZuijinOptions.RequirePkce), errors);
        RequireValue(options.RequireHttpsRedirectUris, nameof(ZuijinOptions.RequireHttpsRedirectUris), errors);

        RequirePositive(options.DefaultAccessTokenLifetime, nameof(ZuijinOptions.DefaultAccessTokenLifetime), errors);
        RequirePositive(options.DefaultRefreshTokenLifetime, nameof(ZuijinOptions.DefaultRefreshTokenLifetime), errors);
        RequirePositive(options.DefaultIdTokenLifetime, nameof(ZuijinOptions.DefaultIdTokenLifetime), errors);
        RequirePositive(options.AuthorizationCodeLifetime, nameof(ZuijinOptions.AuthorizationCodeLifetime), errors);
        RequirePositive(options.DeviceCodeLifetime, nameof(ZuijinOptions.DeviceCodeLifetime), errors);
        RequirePositive(options.DeviceCodePollingInterval, nameof(ZuijinOptions.DeviceCodePollingInterval), errors);
        RequirePositive(options.KeyRotationIntervalDays, nameof(ZuijinOptions.KeyRotationIntervalDays), errors);
        RequirePositive(options.MaxFailedLoginAttempts, nameof(ZuijinOptions.MaxFailedLoginAttempts), errors);
        RequirePositive(options.LockoutDurationMinutes, nameof(ZuijinOptions.LockoutDurationMinutes), errors);

        return errors;
    }

    private static void ValidateIssuer(string? issuer, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            errors.Add(MissingKey(nameof(ZuijinOptions.Issuer)));
            return;
        }

        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri))
        {
            errors.Add($"'{ZuijinOptions.SectionName}:{nameof(ZuijinOptions.Issuer)}' must be an absolute URI.");
            return;
        }

        // HTTPS is mandatory for issuers except loopback hosts used during local development.
        var isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        if (!isHttps && !uri.IsLoopback)
        {
            errors.Add($"'{ZuijinOptions.SectionName}:{nameof(ZuijinOptions.Issuer)}' must use HTTPS for non-loopback hosts.");
        }
    }

    private static void RequireValue(bool? value, string name, List<string> errors)
    {
        if (value is null)
        {
            errors.Add(MissingKey(name));
        }
    }

    private static void RequirePositive(int? value, string name, List<string> errors)
    {
        if (value is null)
        {
            errors.Add(MissingKey(name));
        }
        else if (value <= 0)
        {
            errors.Add($"'{ZuijinOptions.SectionName}:{name}' must be greater than zero.");
        }
    }

    private static string MissingKey(string name)
    {
        return $"'{ZuijinOptions.SectionName}:{name}' is not configured. Add it to the host configuration (appsettings.json, user secrets, or environment).";
    }
}
