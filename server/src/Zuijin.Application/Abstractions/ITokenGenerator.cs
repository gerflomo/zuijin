namespace Zuijin.Application.Abstractions;

/// <summary>
/// Generates JWT access tokens, ID tokens, refresh tokens, and authorization codes.
/// </summary>
public interface ITokenGenerator
{
    Task<string> GenerateAccessToken(TokenGenerationRequest request, CancellationToken cancellationToken = default);
    Task<string> GenerateIdToken(TokenGenerationRequest request, CancellationToken cancellationToken = default);
    string GenerateRefreshToken();
    string GenerateAuthorizationCode();
}

public class TokenGenerationRequest
{
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; set; } = [];
    public IDictionary<string, object> Claims { get; set; } = new Dictionary<string, object>();
    public TimeSpan Lifetime { get; set; }
    public string? Nonce { get; set; }
}
