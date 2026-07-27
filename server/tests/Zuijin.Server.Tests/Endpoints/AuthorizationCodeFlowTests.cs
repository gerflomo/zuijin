using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Zuijin.AspNetCore.Endpoints;

namespace Zuijin.Server.Tests.Endpoints;

/// <summary>
/// Walks the authorization code flow the way a browser and a client application would:
/// authorize, sign in, redeem the code, then refresh.
/// </summary>
[Collection(ZuijinTestCollection.Name)]
public class AuthorizationCodeFlowTests
{
    private const string CodeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    private readonly ZuijinApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationCodeFlowTests(ZuijinApplicationFactory factory)
    {
        _factory = factory;

        // Redirects are followed manually: each hop is part of what these tests assert.
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static string CodeChallenge()
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(CodeVerifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string AuthorizeUrl(string clientId, string scope = "openid profile offline_access", string? state = "xyz")
    {
        var parameters = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = ZuijinApplicationFactory.CodeClientRedirectUri,
            ["scope"] = scope,
            ["code_challenge"] = CodeChallenge(),
            ["code_challenge_method"] = "S256"
        };

        if (state is not null)
        {
            parameters["state"] = state;
        }

        return QueryHelpers.AddQueryString(ZuijinEndpointPaths.Authorize, parameters);
    }

    private async Task SignIn(string returnUrl)
    {
        var response = await _client.PostAsync(ZuijinEndpointPaths.Login, new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", ZuijinApplicationFactory.Username),
            new KeyValuePair<string, string>("password", ZuijinApplicationFactory.Password),
            new KeyValuePair<string, string>("returnUrl", returnUrl)
        ]), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Found, "the credentials should have been accepted");
    }

    /// <summary>Signs in and drives the authorization endpoint until it hands back a code.</summary>
    private async Task<string> ObtainAuthorizationCode(string clientId = ZuijinApplicationFactory.CodeClientId)
    {
        var authorizeUrl = AuthorizeUrl(clientId);

        await SignIn(authorizeUrl);

        var response = await _client.GetAsync(authorizeUrl, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(ZuijinApplicationFactory.CodeClientRedirectUri);

        return QueryHelpers.ParseQuery(new Uri(location).Query)["code"].ToString();
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> PostToken(params (string Key, string Value)[] fields)
    {
        var response = await _client.PostAsync(
            ZuijinEndpointPaths.Token,
            new FormUrlEncodedContent(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value))),
            TestContext.Current.CancellationToken);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return (response.StatusCode, body);
    }

    private Task<(HttpStatusCode Status, JsonElement Body)> RedeemCode(string code, string codeVerifier = CodeVerifier)
    {
        return PostToken(
            ("grant_type", "authorization_code"),
            ("code", code),
            ("redirect_uri", ZuijinApplicationFactory.CodeClientRedirectUri),
            ("code_verifier", codeVerifier),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));
    }

    [Fact]
    public async Task Authorize_AnonymousUser_IsSentToTheLoginPage()
    {
        // Act
        var response = await _client.GetAsync(AuthorizeUrl(ZuijinApplicationFactory.CodeClientId),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldStartWith(ZuijinEndpointPaths.Login);
    }

    [Fact]
    public async Task Authorize_UnknownClient_RendersAnErrorInsteadOfRedirecting()
    {
        // Act
        var response = await _client.GetAsync(AuthorizeUrl("no-such-client"), TestContext.Current.CancellationToken);

        // Assert: an unverified client must never cause a redirect.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/html");
    }

    [Fact]
    public async Task Authorize_UnregisteredRedirectUri_RendersAnErrorInsteadOfRedirecting()
    {
        // Arrange
        var url = QueryHelpers.AddQueryString(ZuijinEndpointPaths.Authorize, new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = ZuijinApplicationFactory.CodeClientId,
            ["redirect_uri"] = "https://evil.example.test/steal",
            ["scope"] = "openid",
            ["code_challenge"] = CodeChallenge(),
            ["code_challenge_method"] = "S256"
        });

        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert: this is what stops the endpoint being used as an open redirector.
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Headers.Location.ShouldBeNull();
    }

    [Fact]
    public async Task Authorize_MissingPkceChallenge_RedirectsWithInvalidRequest()
    {
        // Arrange
        var url = QueryHelpers.AddQueryString(ZuijinEndpointPaths.Authorize, new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = ZuijinApplicationFactory.CodeClientId,
            ["redirect_uri"] = ZuijinApplicationFactory.CodeClientRedirectUri,
            ["scope"] = "openid",
            ["state"] = "abc"
        });

        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert: the redirect URI is verified by now, so the error travels back to the client.
        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        var query = QueryHelpers.ParseQuery(new Uri(response.Headers.Location!.ToString()).Query);
        query["error"].ToString().ShouldBe("invalid_request");
        query["state"].ToString().ShouldBe("abc");
    }

    [Fact]
    public async Task Authorize_SignedInUser_RedirectsBackWithCodeAndState()
    {
        // Arrange
        var authorizeUrl = AuthorizeUrl(ZuijinApplicationFactory.CodeClientId);
        await SignIn(authorizeUrl);

        // Act
        var response = await _client.GetAsync(authorizeUrl, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        var query = QueryHelpers.ParseQuery(new Uri(response.Headers.Location!.ToString()).Query);
        query["code"].ToString().ShouldNotBeNullOrWhiteSpace();
        query["state"].ToString().ShouldBe("xyz");
    }

    [Fact]
    public async Task Authorize_ClientThatRequiresConsent_SendsTheUserToTheConsentPage()
    {
        // Arrange
        var authorizeUrl = AuthorizeUrl(ZuijinApplicationFactory.ConsentClientId);
        await SignIn(authorizeUrl);

        // Act
        var response = await _client.GetAsync(authorizeUrl, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldStartWith(ZuijinEndpointPaths.Consent);
    }

    [Fact]
    public async Task Login_WrongPassword_RedisplaysTheFormWithoutSigningIn()
    {
        // Act
        var response = await _client.PostAsync(ZuijinEndpointPaths.Login, new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("username", ZuijinApplicationFactory.Username),
            new KeyValuePair<string, string>("password", "not-the-password"),
            new KeyValuePair<string, string>("returnUrl", "/")
        ]), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }

    [Fact]
    public async Task RedeemCode_ValidCodeAndVerifier_ReturnsAccessIdAndRefreshTokens()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();

        // Act
        var (status, body) = await RedeemCode(code);

        // Assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("id_token").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("refresh_token").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("token_type").GetString().ShouldBe("Bearer");
    }

    [Fact]
    public async Task RedeemCode_IssuedAccessToken_CarriesTheUserSubjectAndRbacClaims()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (_, body) = await RedeemCode(code);

        var keySet = new JsonWebKeySet(
            await _client.GetStringAsync(ZuijinEndpointPaths.Jwks, TestContext.Current.CancellationToken));

        // Act
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            body.GetProperty("access_token").GetString(),
            new TokenValidationParameters
            {
                ValidIssuer = ZuijinApplicationFactory.Issuer,
                IssuerSigningKeys = keySet.GetSigningKeys(),
                ValidateAudience = false
            });

        // Assert
        result.IsValid.ShouldBeTrue(result.Exception?.Message);

        var token = (JsonWebToken)result.SecurityToken;
        token.Subject.ShouldBe(ZuijinApplicationFactory.SubjectId);
        token.GetClaim("role").Value.ShouldContain(ZuijinApplicationFactory.RoleName);
        token.GetClaim("permission").Value.ShouldContain(ZuijinApplicationFactory.PermissionName);
    }

    [Fact]
    public async Task RedeemCode_IssuedIdToken_IsAudiencedToTheClientNotToAnApi()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (_, body) = await RedeemCode(code);

        // Act
        var idToken = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("id_token").GetString());

        // Assert: OpenID Connect addresses the ID token to the requesting client.
        idToken.Audiences.ShouldBe([ZuijinApplicationFactory.CodeClientId]);
        idToken.Subject.ShouldBe(ZuijinApplicationFactory.SubjectId);
    }

    [Fact]
    public async Task RedeemCode_WrongCodeVerifier_IsRejected()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();

        // Act
        var (status, body) = await RedeemCode(code, codeVerifier: "a-completely-different-verifier");

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_grant");
    }

    [Fact]
    public async Task RedeemCode_UsedTwice_IsRejectedTheSecondTime()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (firstStatus, _) = await RedeemCode(code);
        firstStatus.ShouldBe(HttpStatusCode.OK);

        // Act
        var (status, body) = await RedeemCode(code);

        // Assert: an authorization code is good for exactly one redemption.
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_grant");
    }

    [Fact]
    public async Task RedeemCode_ReplayedCode_RevokesTheRefreshTokenItProduced()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (_, first) = await RedeemCode(code);
        var refreshToken = first.GetProperty("refresh_token").GetString()!;

        // Act: the replay is what signals the code leaked.
        await RedeemCode(code);

        var (status, body) = await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", refreshToken),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        // Assert: everything the replayed code produced must be dead.
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_grant");
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsANewRefreshToken()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (_, issued) = await RedeemCode(code);
        var refreshToken = issued.GetProperty("refresh_token").GetString()!;

        // Act
        var (status, body) = await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", refreshToken),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        // Assert
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("refresh_token").GetString().ShouldNotBe(refreshToken);
        body.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_ReusingARotatedToken_IsRejected()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (_, issued) = await RedeemCode(code);
        var refreshToken = issued.GetProperty("refresh_token").GetString()!;

        await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", refreshToken),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        // Act: presenting the superseded token is the signature of a stolen copy.
        var (status, body) = await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", refreshToken),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_grant");
    }

    [Fact]
    public async Task Refresh_AfterReuseIsDetected_TheWholeChainIsDead()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (_, issued) = await RedeemCode(code);
        var firstRefreshToken = issued.GetProperty("refresh_token").GetString()!;

        var (_, rotated) = await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", firstRefreshToken),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        var secondRefreshToken = rotated.GetProperty("refresh_token").GetString()!;

        // Act: replaying the first token should also invalidate the legitimate successor.
        await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", firstRefreshToken),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        var (status, body) = await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", secondRefreshToken),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        // Assert
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_grant");
    }

    [Fact]
    public async Task Refresh_RequestingAWiderScope_IsRejected()
    {
        // Arrange
        var code = await ObtainAuthorizationCode();
        var (_, issued) = await RedeemCode(code);

        // Act
        var (status, body) = await PostToken(
            ("grant_type", "refresh_token"),
            ("refresh_token", issued.GetProperty("refresh_token").GetString()!),
            ("scope", $"openid {ZuijinApplicationFactory.ApiScopeName}"),
            ("client_id", ZuijinApplicationFactory.CodeClientId),
            ("client_secret", ZuijinApplicationFactory.ConfidentialClientSecret));

        // Assert: a refresh may narrow the original grant, never widen it.
        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().ShouldBe("invalid_scope");
    }
}
