namespace Zuijin.Application.Features.Tokens;

/// <summary>
/// A successful token response as described by RFC 6749 section 5.1.
/// </summary>
public sealed class TokenIssuanceResult
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public required string Scope { get; init; }

    /// <summary>Present only when the "openid" scope was granted.</summary>
    public string? IdToken { get; init; }

    /// <summary>Present only when the client is allowed offline access and asked for it.</summary>
    public string? RefreshToken { get; init; }
}
