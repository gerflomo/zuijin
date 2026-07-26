using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Zuijin.Application.Abstractions;
using Zuijin.AspNetCore.Endpoints;

namespace Zuijin.Server.Tests.Endpoints;

public class DiscoveryEndpointsTests : IClassFixture<ZuijinApplicationFactory>
{
    private readonly ZuijinApplicationFactory _factory;
    private readonly HttpClient _client;

    public DiscoveryEndpointsTests(ZuijinApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<JsonElement> GetJson(string path)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToList();
    }

    [Fact]
    public async Task GetDiscoveryDocument_ReturnsConfiguredIssuer()
    {
        // Act
        var document = await GetJson(ZuijinEndpointPaths.Discovery);

        // Assert
        document.GetProperty("issuer").GetString().ShouldBe(ZuijinApplicationFactory.Issuer);
    }

    [Fact]
    public async Task GetDiscoveryDocument_DerivesEndpointUrlsFromIssuer()
    {
        // Act
        var document = await GetJson(ZuijinEndpointPaths.Discovery);

        // Assert
        document.GetProperty("token_endpoint").GetString()
            .ShouldBe(ZuijinApplicationFactory.Issuer + ZuijinEndpointPaths.Token);
        document.GetProperty("authorization_endpoint").GetString()
            .ShouldBe(ZuijinApplicationFactory.Issuer + ZuijinEndpointPaths.Authorize);
        document.GetProperty("jwks_uri").GetString()
            .ShouldBe(ZuijinApplicationFactory.Issuer + ZuijinEndpointPaths.Jwks);
    }

    [Fact]
    public async Task GetDiscoveryDocument_ListsTheSeededStandardScopes()
    {
        // Act
        var document = await GetJson(ZuijinEndpointPaths.Discovery);

        // Assert
        var scopes = ReadStringArray(document, "scopes_supported");
        scopes.ShouldContain("openid");
        scopes.ShouldContain("profile");
        scopes.ShouldContain("email");
        scopes.ShouldContain("offline_access");
    }

    [Fact]
    public async Task GetDiscoveryDocument_AdvertisesSupportedProtocolCapabilities()
    {
        // Act
        var document = await GetJson(ZuijinEndpointPaths.Discovery);

        // Assert
        ReadStringArray(document, "response_types_supported").ShouldBe(["code"]);
        ReadStringArray(document, "id_token_signing_alg_values_supported").ShouldBe(["RS256"]);
        ReadStringArray(document, "grant_types_supported").ShouldContain("authorization_code");
        // Only S256: advertising "plain" would invite clients to defeat PKCE.
        ReadStringArray(document, "code_challenge_methods_supported").ShouldBe(["S256"]);
    }

    [Fact]
    public async Task GetJsonWebKeySet_ReturnsTheActiveSigningKey()
    {
        // Act
        var document = await GetJson(ZuijinEndpointPaths.Jwks);

        // Assert: the maintenance service creates a key during host startup.
        var keys = document.GetProperty("keys").EnumerateArray().ToList();
        keys.ShouldNotBeEmpty();

        var key = keys[0];
        key.GetProperty("kty").GetString().ShouldBe("RSA");
        key.GetProperty("use").GetString().ShouldBe("sig");
        key.GetProperty("alg").GetString().ShouldBe("RS256");
        key.GetProperty("kid").GetString().ShouldNotBeNullOrWhiteSpace();
        key.GetProperty("n").GetString().ShouldNotBeNullOrWhiteSpace();
        key.GetProperty("e").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("d")]
    [InlineData("p")]
    [InlineData("q")]
    [InlineData("dp")]
    [InlineData("dq")]
    [InlineData("qi")]
    public async Task GetJsonWebKeySet_NeverExposesPrivateKeyComponents(string privateComponent)
    {
        // Act
        var document = await GetJson(ZuijinEndpointPaths.Jwks);

        // Assert
        foreach (var key in document.GetProperty("keys").EnumerateArray())
        {
            key.TryGetProperty(privateComponent, out _).ShouldBeFalse(
                $"The JWK Set must never publish the '{privateComponent}' private component.");
        }
    }

    [Fact]
    public async Task PublishedJsonWebKeySet_ValidatesATokenSignedByTheServer()
    {
        // Arrange
        const string audience = "api.example.com";
        var tokenGenerator = _factory.Services.GetRequiredService<ITokenGenerator>();

        var token = await tokenGenerator.GenerateAccessToken(new TokenGenerationRequest
        {
            Issuer = ZuijinApplicationFactory.Issuer,
            Subject = "integration-test-subject",
            Audience = audience,
            Scopes = ["openid"],
            Lifetime = TimeSpan.FromMinutes(5)
        });

        // Act
        var keySet = new JsonWebKeySet(await _client.GetStringAsync(ZuijinEndpointPaths.Jwks));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = ZuijinApplicationFactory.Issuer,
            ValidAudience = audience,
            IssuerSigningKeys = keySet.GetSigningKeys()
        });

        // Assert: signing and key publication must agree end to end.
        result.IsValid.ShouldBeTrue(result.Exception?.Message);
    }
}
