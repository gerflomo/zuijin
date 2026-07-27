using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Zuijin.AspNetCore.Endpoints;

namespace Zuijin.Server.Tests.Endpoints;

[Collection(ZuijinTestCollection.Name)]
public class TokenEndpointTests
{
    private readonly HttpClient _client;

    public TokenEndpointTests(ZuijinApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields)
    {
        return new FormUrlEncodedContent(fields.Select(field =>
            new KeyValuePair<string, string>(field.Key, field.Value)));
    }

    private static AuthenticationHeaderValue BasicHeader(string clientId, string secret)
    {
        var credentials = $"{Uri.EscapeDataString(clientId)}:{Uri.EscapeDataString(secret)}";
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> Post(
        HttpContent content,
        AuthenticationHeaderValue? authorization = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ZuijinEndpointPaths.Token)
        {
            Content = content
        };
        request.Headers.Authorization = authorization;

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return (response.StatusCode, body);
    }

    [Fact]
    public async Task Post_ValidClientSecretPost_ReturnsAccessToken()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret)));

        // Assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("token_type").GetString().ShouldBe("Bearer");
        body.GetProperty("expires_in").GetInt32().ShouldBe(3600);
    }

    [Fact]
    public async Task Post_ValidClientSecretBasic_ReturnsAccessToken()
    {
        // Act
        var (status, body) = await Post(
            Form(("grant_type", "client_credentials")),
            BasicHeader(ZuijinApplicationFactory.ConfidentialClientId, ZuijinApplicationFactory.ConfidentialClientSecret));

        // Assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_NoScopeRequested_GrantsEveryApplicableScopeButNotOpenId()
    {
        // Act
        var (_, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret)));

        // Assert: the client owns openid, but this grant has no user to describe.
        var scope = body.GetProperty("scope").GetString()!;
        scope.Split(' ').ShouldBe(["profile", ZuijinApplicationFactory.ApiScopeName], ignoreOrder: true);
    }

    [Fact]
    public async Task Post_SubsetOfScopesRequested_GrantsOnlyThatSubset()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret),
            ("scope", ZuijinApplicationFactory.ApiScopeName)));

        // Assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("scope").GetString().ShouldBe(ZuijinApplicationFactory.ApiScopeName);
    }

    [Fact]
    public async Task Post_OpenIdRequestedExplicitly_ReturnsInvalidScope()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret),
            ("scope", "openid")));

        // Assert: openid promises an ID token, which this grant can never produce.
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_scope");
    }

    [Fact]
    public async Task Post_IssuedToken_IsAudiencedToTheApiThatOwnsTheScope()
    {
        // Arrange
        var (_, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret),
            ("scope", ZuijinApplicationFactory.ApiScopeName)));

        var keySet = new JsonWebKeySet(
            await _client.GetStringAsync(ZuijinEndpointPaths.Jwks, TestContext.Current.CancellationToken));

        // Act
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            body.GetProperty("access_token").GetString(),
            new TokenValidationParameters
            {
                ValidIssuer = ZuijinApplicationFactory.Issuer,
                ValidAudience = ZuijinApplicationFactory.ApiAudience,
                IssuerSigningKeys = keySet.GetSigningKeys()
            });

        // Assert
        result.IsValid.ShouldBeTrue(result.Exception?.Message);

        var token = (JsonWebToken)result.SecurityToken;
        token.Subject.ShouldBe(ZuijinApplicationFactory.ConfidentialClientId);
        token.GetClaim("client_id").Value.ShouldBe(ZuijinApplicationFactory.ConfidentialClientId);
        token.Audiences.ShouldBe([ZuijinApplicationFactory.ApiAudience]);
    }

    [Fact]
    public async Task Post_IdentityScopesOnly_IssuesATokenWithNoAudience()
    {
        // Arrange
        var (_, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret),
            ("scope", "profile")));

        // Act
        var token = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("access_token").GetString());

        // Assert: no API owns "profile", so no resource server should accept this token.
        token.Audiences.ShouldBeEmpty();
    }

    [Fact]
    public async Task Post_SuccessfulResponse_IsNotCacheable()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Post, ZuijinEndpointPaths.Token)
        {
            Content = Form(
                ("grant_type", "client_credentials"),
                ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
                ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret))
        };

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert: RFC 6749 forbids caching token responses.
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Post_WrongSecret_ReturnsUnauthorizedInvalidClient()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", "not-the-secret")));

        // Assert
        status.ShouldBe(HttpStatusCode.Unauthorized);
        body.GetProperty("error").GetString().ShouldBe("invalid_client");
    }

    [Fact]
    public async Task Post_UnknownClient_ReturnsTheSameErrorAsAWrongSecret()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", "no-such-client"),
            ("client_secret", "whatever")));

        // Assert: the response must not reveal whether the client exists.
        status.ShouldBe(HttpStatusCode.Unauthorized);
        body.GetProperty("error").GetString().ShouldBe("invalid_client");
        body.GetProperty("error_description").GetString().ShouldBe("Client authentication failed.");
    }

    [Fact]
    public async Task Post_MissingCredentials_ReturnsUnauthorizedWithBasicChallenge()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Post, ZuijinEndpointPaths.Token)
        {
            Content = Form(("grant_type", "client_credentials"))
        };

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldContain("Basic");
    }

    [Fact]
    public async Task Post_CredentialsInBothHeaderAndBody_IsRejected()
    {
        // Act
        var (status, body) = await Post(
            Form(
                ("grant_type", "client_credentials"),
                ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
                ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret)),
            BasicHeader(ZuijinApplicationFactory.ConfidentialClientId, ZuijinApplicationFactory.ConfidentialClientSecret));

        // Assert: RFC 6749 allows exactly one authentication method per request.
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_request");
    }

    [Fact]
    public async Task Post_PublicClient_IsNotAllowedToUseClientCredentials()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.PublicClientId)));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("unauthorized_client");
    }

    [Fact]
    public async Task Post_DisabledClient_IsRejected()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.DisabledClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret)));

        // Assert
        status.ShouldBe(HttpStatusCode.Unauthorized);
        body.GetProperty("error").GetString().ShouldBe("invalid_client");
    }

    [Fact]
    public async Task Post_ClientWithoutTheGrant_ReturnsUnauthorizedClient()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.WrongGrantClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret)));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("unauthorized_client");
    }

    [Fact]
    public async Task Post_ScopeTheClientDoesNotOwn_ReturnsInvalidScope()
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", "client_credentials"),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret),
            ("scope", $"{ZuijinApplicationFactory.ApiScopeName} email")));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_scope");
    }

    [Theory]
    [InlineData("authorization_code")]
    [InlineData("refresh_token")]
    [InlineData("password")]
    public async Task Post_GrantTypeNotImplemented_ReturnsUnsupportedGrantType(string grantType)
    {
        // Act
        var (status, body) = await Post(Form(
            ("grant_type", grantType),
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret)));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("unsupported_grant_type");
    }

    [Fact]
    public async Task Post_MissingGrantType_ReturnsInvalidRequest()
    {
        // Act
        var (status, body) = await Post(Form(
            ("client_id", ZuijinApplicationFactory.ConfidentialClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret)));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_request");
    }

    [Fact]
    public async Task Post_NonFormBody_ReturnsInvalidRequest()
    {
        // Act
        var (status, body) = await Post(new StringContent("{}", Encoding.UTF8, "application/json"));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_request");
    }
}
