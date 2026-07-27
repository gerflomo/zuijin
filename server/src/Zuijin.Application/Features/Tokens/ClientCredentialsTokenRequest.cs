namespace Zuijin.Application.Features.Tokens;

/// <summary>
/// A token request using the client credentials grant (RFC 6749 section 4.4).
/// </summary>
public sealed class ClientCredentialsTokenRequest
{
    public required string ClientId { get; init; }
    public required string? ClientSecret { get; init; }

    /// <summary>Scopes asked for by the client. Empty means "everything this client is entitled to".</summary>
    public IReadOnlyList<string> RequestedScopes { get; init; } = [];
}
