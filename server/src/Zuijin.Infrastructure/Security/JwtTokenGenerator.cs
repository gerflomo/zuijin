using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Zuijin.Application.Abstractions;

namespace Zuijin.Infrastructure.Security;

/// <summary>
/// Issues RS256-signed JWTs for access and ID tokens, and opaque random strings
/// for refresh tokens and authorization codes.
/// </summary>
public class JwtTokenGenerator : ITokenGenerator
{
    private const string ScopeClaim = "scope";
    private const int OpaqueTokenEntropyBytes = 32;

    private readonly ISigningKeyService _signingKeyService;
    private readonly IClock _clock;

    public JwtTokenGenerator(ISigningKeyService signingKeyService, IClock clock)
    {
        _signingKeyService = signingKeyService;
        _clock = clock;
    }

    public Task<string> GenerateAccessToken(TokenGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claims = new Dictionary<string, object>(request.Claims);

        if (request.Scopes.Count > 0)
        {
            claims[ScopeClaim] = string.Join(' ', request.Scopes);
        }

        return CreateToken(request, claims, cancellationToken);
    }

    public Task<string> GenerateIdToken(TokenGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claims = new Dictionary<string, object>(request.Claims);

        if (!string.IsNullOrEmpty(request.Nonce))
        {
            claims[JwtRegisteredClaimNames.Nonce] = request.Nonce;
        }

        return CreateToken(request, claims, cancellationToken);
    }

    public string GenerateRefreshToken()
    {
        return CreateOpaqueToken();
    }

    public string GenerateAuthorizationCode()
    {
        return CreateOpaqueToken();
    }

    private async Task<string> CreateToken(
        TokenGenerationRequest request,
        Dictionary<string, object> claims,
        CancellationToken cancellationToken)
    {
        claims[JwtRegisteredClaimNames.Sub] = request.Subject;
        claims[JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString("N");

        var signingKey = await _signingKeyService.GetActiveSigningKey(cancellationToken);
        using var rsa = signingKey.Key;

        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = signingKey.KeyId,
            // The RSA instance is disposed when this method returns, so the signature
            // provider must not be cached and reused against a dead key.
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var issuedAt = _clock.UtcNow.UtcDateTime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = request.Issuer,
            Audience = request.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = issuedAt.Add(request.Lifetime),
            Claims = claims,
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static string CreateOpaqueToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(OpaqueTokenEntropyBytes));
    }
}
