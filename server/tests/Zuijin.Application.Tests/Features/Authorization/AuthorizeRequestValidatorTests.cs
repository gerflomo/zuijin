using NSubstitute;
using Shouldly;
using Zuijin.Application.Configuration;
using Zuijin.Application.Features.Authorization;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Tests.Features.Authorization;

public class AuthorizeRequestValidatorTests
{
    private const string ClientId = "code-client";
    private const string RedirectUri = "https://client.example.com/callback";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

    private readonly IClientRepository _clientRepository = Substitute.For<IClientRepository>();
    private readonly AuthorizeRequestValidator _validator;

    public AuthorizeRequestValidatorTests()
    {
        _validator = new AuthorizeRequestValidator(
            _clientRepository,
            new ZuijinOptions { RequirePkce = true, RequireHttpsRedirectUris = true });
    }

    private static Client CreateClient(bool requirePkce = true, bool scopeActive = true)
    {
        return new Client
        {
            Id = Guid.CreateVersion7(),
            ClientId = ClientId,
            ClientName = "Code Client",
            Type = ClientType.Confidential,
            IsActive = true,
            RequirePkce = requirePkce,
            RedirectUris = [new ClientRedirectUri { Uri = RedirectUri, Type = RedirectUriType.Redirect }],
            GrantTypes = [new ClientGrantType { GrantType = GrantTypes.AuthorizationCode }],
            Scopes =
            [
                new ClientScope { Scope = new Scope { Name = StandardScopes.OpenId, IsActive = true } },
                new ClientScope { Scope = new Scope { Name = "profile", IsActive = scopeActive } }
            ]
        };
    }

    private void GivenClient(Client? client)
    {
        _clientRepository.GetByClientId(ClientId, Arg.Any<CancellationToken>()).Returns(client);
    }

    private static AuthorizeRequest CreateRequest(
        string? scope = "openid profile",
        string? challenge = Challenge,
        string? method = "S256",
        string? responseType = "code",
        string? redirectUri = RedirectUri)
    {
        return new AuthorizeRequest
        {
            ClientId = ClientId,
            RedirectUri = redirectUri,
            ResponseType = responseType,
            Scope = scope,
            CodeChallenge = challenge,
            CodeChallengeMethod = method,
            State = "state-value"
        };
    }

    [Fact]
    public async Task ValidateClientAndRedirectUri_RegisteredClientAndUri_Succeeds()
    {
        // Arrange
        var client = CreateClient();
        GivenClient(client);

        // Act
        var (resolved, redirectUri) = await _validator.ValidateClientAndRedirectUri(
            CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        resolved.ClientId.ShouldBe(ClientId);
        redirectUri.ShouldBe(RedirectUri);
    }

    [Fact]
    public async Task ValidateClientAndRedirectUri_UnknownClient_ThrowsInvalidClient()
    {
        // Arrange
        GivenClient(null);

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _validator.ValidateClientAndRedirectUri(CreateRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidClient);
    }

    [Fact]
    public async Task ValidateClientAndRedirectUri_UnregisteredUri_ThrowsInvalidRedirectUri()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        var exception = await Should.ThrowAsync<DomainException>(
            () => _validator.ValidateClientAndRedirectUri(
                CreateRequest(redirectUri: "https://evil.example.com/steal"), TestContext.Current.CancellationToken));

        // Assert: this is the check that stops the endpoint being an open redirector.
        exception.Code.ShouldBe("invalid_redirect_uri");
    }

    [Fact]
    public async Task ValidateClientAndRedirectUri_MissingRedirectUri_ThrowsInvalidRequest()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _validator.ValidateClientAndRedirectUri(
                CreateRequest(redirectUri: null), TestContext.Current.CancellationToken));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidRequest);
    }

    [Fact]
    public async Task ValidateClientAndRedirectUri_PlainHttpUri_IsRejectedWhenHttpsIsRequired()
    {
        // Arrange
        var client = CreateClient();
        client.RedirectUris = [new ClientRedirectUri { Uri = "http://app.example.com/cb", Type = RedirectUriType.Redirect }];
        GivenClient(client);

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _validator.ValidateClientAndRedirectUri(
                CreateRequest(redirectUri: "http://app.example.com/cb"), TestContext.Current.CancellationToken));

        // Assert
        exception.Error.ErrorDescription!.ShouldContain("HTTPS");
    }

    [Fact]
    public async Task ValidateClientAndRedirectUri_LoopbackHttpUri_IsAllowed()
    {
        // Arrange: native and desktop clients redirect to loopback over plain HTTP.
        var client = CreateClient();
        client.RedirectUris = [new ClientRedirectUri { Uri = "http://127.0.0.1:5000/cb", Type = RedirectUriType.Redirect }];
        GivenClient(client);

        // Act & Assert
        await Should.NotThrowAsync(() => _validator.ValidateClientAndRedirectUri(
            CreateRequest(redirectUri: "http://127.0.0.1:5000/cb"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Validate_WellFormedRequest_ReturnsTheParsedScopes()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var validated = _validator.Validate(CreateRequest(), client, RedirectUri);

        // Assert
        validated.Scopes.ShouldBe(["openid", "profile"]);
        validated.State.ShouldBe("state-value");
    }

    [Theory]
    [InlineData("token")]
    [InlineData("id_token")]
    [InlineData("")]
    public void Validate_UnsupportedResponseType_Throws(string responseType)
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<DomainException>(
            () => _validator.Validate(CreateRequest(responseType: responseType), client, RedirectUri));

        // Assert
        exception.Code.ShouldBe("unsupported_response_type");
    }

    [Fact]
    public void Validate_MissingScope_ThrowsInvalidScope()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<OAuthException>(
            () => _validator.Validate(CreateRequest(scope: null), client, RedirectUri));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidScope);
    }

    [Fact]
    public void Validate_DisabledScope_ThrowsInvalidScope()
    {
        // Arrange
        var client = CreateClient(scopeActive: false);

        // Act
        var exception = Should.Throw<OAuthException>(
            () => _validator.Validate(CreateRequest(), client, RedirectUri));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidScope);
    }

    [Fact]
    public void Validate_MissingCodeChallenge_ThrowsWhenPkceIsRequired()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<OAuthException>(
            () => _validator.Validate(CreateRequest(challenge: null), client, RedirectUri));

        // Assert
        exception.Error.ErrorDescription!.ShouldContain("PKCE");
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("S512")]
    [InlineData(null)]
    public void Validate_NonS256ChallengeMethod_IsRejected(string? method)
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<OAuthException>(
            () => _validator.Validate(CreateRequest(method: method), client, RedirectUri));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidRequest);
    }

    [Fact]
    public void Validate_ClientWithPkceOff_StillRequiresItWhenTheServerDoes()
    {
        // Arrange: the server-wide setting is a floor, not a default a client can lower.
        var client = CreateClient(requirePkce: false);

        // Act
        var exception = Should.Throw<OAuthException>(
            () => _validator.Validate(CreateRequest(challenge: null), client, RedirectUri));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidRequest);
    }
}
