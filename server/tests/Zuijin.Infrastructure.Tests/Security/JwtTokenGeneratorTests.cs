using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Shouldly;
using Zuijin.Application.Abstractions;
using Zuijin.Infrastructure.Security;

namespace Zuijin.Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests : IDisposable
{
    private const string KeyId = "test-key-id";
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RSA _publicRsa = RSA.Create();
    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        _publicRsa.ImportParameters(_rsa.ExportParameters(includePrivateParameters: false));

        var signingKeyService = Substitute.For<ISigningKeyService>();

        // A fresh RSA per call: the generator owns and disposes what it receives.
        signingKeyService.GetActiveSigningKey(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new SigningKeyInfo
            {
                KeyId = KeyId,
                Algorithm = "RS256",
                Key = ClonePrivateKey()
            }));

        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        _generator = new JwtTokenGenerator(signingKeyService, clock);
    }

    public void Dispose()
    {
        _rsa.Dispose();
        _publicRsa.Dispose();
        GC.SuppressFinalize(this);
    }

    private RSA ClonePrivateKey()
    {
        var clone = RSA.Create();
        clone.ImportRSAPrivateKey(_rsa.ExportRSAPrivateKey(), out _);
        return clone;
    }

    private static TokenGenerationRequest CreateRequest()
    {
        return new TokenGenerationRequest
        {
            Issuer = "https://auth.example.com",
            Subject = "user-subject-id",
            Audience = "api.example.com",
            Scopes = ["openid", "profile"],
            Lifetime = TimeSpan.FromHours(1)
        };
    }

    private async Task<JsonWebToken> ValidateAndRead(string token)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "https://auth.example.com",
            ValidAudience = "api.example.com",
            // The kid must match the one the generator stamps on the header. Provider
            // caching is off because the cache is keyed by key material, not by instance.
            IssuerSigningKey = new RsaSecurityKey(_publicRsa)
            {
                KeyId = KeyId,
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            },
            ValidateLifetime = false
        });

        result.IsValid.ShouldBeTrue(result.Exception?.Message);
        return (JsonWebToken)result.SecurityToken;
    }

    [Fact]
    public async Task GenerateAccessToken_ValidRequest_ProducesTokenSignedWithActiveKey()
    {
        // Act
        var token = await _generator.GenerateAccessToken(CreateRequest());

        // Assert
        var parsed = await ValidateAndRead(token);
        parsed.Alg.ShouldBe(SecurityAlgorithms.RsaSha256);
        parsed.Kid.ShouldBe(KeyId);
    }

    [Fact]
    public async Task GenerateAccessToken_ValidRequest_SetsRegisteredClaims()
    {
        // Act
        var token = await _generator.GenerateAccessToken(CreateRequest());

        // Assert
        var parsed = await ValidateAndRead(token);
        parsed.Subject.ShouldBe("user-subject-id");
        parsed.Issuer.ShouldBe("https://auth.example.com");
        parsed.Audiences.ShouldContain("api.example.com");
        parsed.GetClaim(JwtRegisteredClaimNames.Jti).Value.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GenerateAccessToken_MultipleScopes_JoinsThemWithSpaces()
    {
        // Act
        var token = await _generator.GenerateAccessToken(CreateRequest());

        // Assert
        var parsed = await ValidateAndRead(token);
        parsed.GetClaim("scope").Value.ShouldBe("openid profile");
    }

    [Fact]
    public async Task GenerateAccessToken_CustomClaims_AreIncluded()
    {
        // Arrange
        var request = CreateRequest();
        request.Claims["role"] = "admin";

        // Act
        var token = await _generator.GenerateAccessToken(request);

        // Assert
        var parsed = await ValidateAndRead(token);
        parsed.GetClaim("role").Value.ShouldBe("admin");
    }

    [Fact]
    public async Task GenerateAccessToken_Lifetime_SetsExpiryFromTheClock()
    {
        // Arrange
        var request = CreateRequest();
        request.Lifetime = TimeSpan.FromMinutes(30);

        // Act
        var token = await _generator.GenerateAccessToken(request);

        // Assert
        var parsed = await ValidateAndRead(token);
        parsed.ValidTo.ShouldBe(Now.AddMinutes(30).UtcDateTime, TimeSpan.FromSeconds(1));
        parsed.ValidFrom.ShouldBe(Now.UtcDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GenerateAccessToken_TwoCalls_ProduceDifferentTokenIds()
    {
        // Act
        var first = await ValidateAndRead(await _generator.GenerateAccessToken(CreateRequest()));
        var second = await ValidateAndRead(await _generator.GenerateAccessToken(CreateRequest()));

        // Assert
        first.GetClaim(JwtRegisteredClaimNames.Jti).Value
            .ShouldNotBe(second.GetClaim(JwtRegisteredClaimNames.Jti).Value);
    }

    [Fact]
    public async Task GenerateIdToken_WithNonce_IncludesNonceClaim()
    {
        // Arrange
        var request = CreateRequest();
        request.Nonce = "n-0S6_WzA2Mj";

        // Act
        var token = await _generator.GenerateIdToken(request);

        // Assert
        var parsed = await ValidateAndRead(token);
        parsed.GetClaim(JwtRegisteredClaimNames.Nonce).Value.ShouldBe("n-0S6_WzA2Mj");
    }

    [Fact]
    public async Task GenerateIdToken_WithoutScopes_OmitsScopeClaim()
    {
        // Arrange
        var request = CreateRequest();
        request.Scopes = [];

        // Act
        var token = await _generator.GenerateIdToken(request);

        // Assert
        var parsed = await ValidateAndRead(token);
        parsed.TryGetClaim("scope", out _).ShouldBeFalse();
    }

    [Fact]
    public void GenerateRefreshToken_TwoCalls_ProduceDifferentOpaqueValues()
    {
        // Act
        var first = _generator.GenerateRefreshToken();
        var second = _generator.GenerateRefreshToken();

        // Assert
        first.ShouldNotBe(second);
        first.ShouldNotContain(".");
    }

    [Fact]
    public void GenerateAuthorizationCode_TwoCalls_ProduceDifferentOpaqueValues()
    {
        // Act
        var first = _generator.GenerateAuthorizationCode();
        var second = _generator.GenerateAuthorizationCode();

        // Assert
        first.ShouldNotBe(second);
        first.ShouldNotContain(".");
    }
}
