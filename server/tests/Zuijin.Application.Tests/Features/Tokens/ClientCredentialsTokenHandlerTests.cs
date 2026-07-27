using NSubstitute;
using Shouldly;
using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Application.Features.Tokens;
using Zuijin.Domain.Constants;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Tests.Features.Tokens;

public class ClientCredentialsTokenHandlerTests
{
    private const string ClientId = "test-client";
    private const string ClientSecret = "test-secret";
    private const string GeneratedToken = "generated.access.token";
    private const string ApiScope = "api.read";
    private const string ApiAudience = "https://api.example.com";

    private readonly IClientRepository _clientRepository = Substitute.For<IClientRepository>();
    private readonly IApiResourceRepository _apiResourceRepository = Substitute.For<IApiResourceRepository>();
    private readonly ISecretHasher _secretHasher = Substitute.For<ISecretHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly ClientCredentialsTokenHandler _handler;

    public ClientCredentialsTokenHandlerTests()
    {
        // A deterministic stand-in for the real digest, so verification behaves realistically.
        _secretHasher.Hash(Arg.Any<string>()).Returns(call => $"hash:{call.Arg<string>()}");
        _secretHasher.Verify(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => $"hash:{call.ArgAt<string>(0)}" == call.ArgAt<string>(1));

        _tokenGenerator.GenerateAccessToken(Arg.Any<TokenGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedToken);

        _apiResourceRepository
            .GetAudiencesForScopes(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns([ApiAudience]);

        _handler = new ClientCredentialsTokenHandler(
            new ClientAuthenticator(_clientRepository, _secretHasher),
            _apiResourceRepository,
            _tokenGenerator,
            new ZuijinOptions { Issuer = "https://auth.example.com" });
    }

    private static Client CreateClient(
        ClientType type = ClientType.Confidential,
        bool isActive = true,
        string grantType = GrantTypes.ClientCredentials,
        bool profileScopeActive = true)
    {
        return new Client
        {
            Id = Guid.CreateVersion7(),
            ClientId = ClientId,
            ClientName = "Test Client",
            SecretHash = $"hash:{ClientSecret}",
            Type = type,
            IsActive = isActive,
            AccessTokenLifetime = 1800,
            GrantTypes = [new ClientGrantType { GrantType = grantType }],
            Scopes =
            [
                new ClientScope { Scope = new Scope { Name = StandardScopes.OpenId, IsActive = true } },
                new ClientScope { Scope = new Scope { Name = "profile", IsActive = profileScopeActive } },
                new ClientScope { Scope = new Scope { Name = ApiScope, IsActive = true } }
            ]
        };
    }

    private void GivenClient(Client? client)
    {
        _clientRepository.GetByClientId(ClientId, Arg.Any<CancellationToken>()).Returns(client);
    }

    private static ClientCredentialsTokenRequest CreateRequest(params string[] scopes)
    {
        return new ClientCredentialsTokenRequest
        {
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            RequestedScopes = scopes
        };
    }

    [Fact]
    public async Task Handle_UnknownClient_ThrowsInvalidClient()
    {
        // Arrange
        GivenClient(null);

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidClient);
    }

    [Fact]
    public async Task Handle_PublicClient_ThrowsUnauthorizedClient()
    {
        // Arrange
        GivenClient(CreateClient(type: ClientType.Public));

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.UnauthorizedClient);
    }

    [Theory]
    [InlineData("wrong-secret")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_BadSecret_ThrowsInvalidClient(string? secret)
    {
        // Arrange
        GivenClient(CreateClient());
        var request = new ClientCredentialsTokenRequest { ClientId = ClientId, ClientSecret = secret };

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _handler.Handle(request, TestContext.Current.CancellationToken));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidClient);
    }

    [Fact]
    public async Task Handle_FailedAuthentication_DoesNotIssueAToken()
    {
        // Arrange
        GivenClient(null);

        // Act
        await Should.ThrowAsync<OAuthException>(
            () => _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken));

        // Assert
        await _tokenGenerator.DidNotReceive()
            .GenerateAccessToken(Arg.Any<TokenGenerationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DisabledClient_ThrowsClientDisabled()
    {
        // Arrange
        GivenClient(CreateClient(isActive: false));

        // Act
        var exception = await Should.ThrowAsync<DomainException>(
            () => _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.Code.ShouldBe("client_disabled");
    }

    [Fact]
    public async Task Handle_ClientWithoutTheGrant_ThrowsUnauthorizedGrantType()
    {
        // Arrange
        GivenClient(CreateClient(grantType: GrantTypes.AuthorizationCode));

        // Act
        var exception = await Should.ThrowAsync<DomainException>(
            () => _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.Code.ShouldBe("unauthorized_grant_type");
    }

    [Fact]
    public async Task Handle_NoScopeRequested_GrantsEveryActiveScopeExceptOpenId()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        var result = await _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert: this grant has no user, so openid is not part of what it can deliver.
        result.Scope.ShouldBe($"profile {ApiScope}");
    }

    [Fact]
    public async Task Handle_NoScopeRequested_SkipsDisabledScopes()
    {
        // Arrange
        GivenClient(CreateClient(profileScopeActive: false));

        // Act
        var result = await _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        result.Scope.ShouldBe(ApiScope);
    }

    [Fact]
    public async Task Handle_OpenIdRequestedExplicitly_ThrowsInvalidScope()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _handler.Handle(CreateRequest(StandardScopes.OpenId), TestContext.Current.CancellationToken));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidScope);
    }

    [Fact]
    public async Task Handle_SubsetRequested_GrantsOnlyThatSubset()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        var result = await _handler.Handle(CreateRequest(ApiScope), TestContext.Current.CancellationToken);

        // Assert
        result.Scope.ShouldBe(ApiScope);
    }

    [Fact]
    public async Task Handle_DisabledScopeRequested_ThrowsInvalidScope()
    {
        // Arrange
        GivenClient(CreateClient(profileScopeActive: false));

        // Act
        var exception = await Should.ThrowAsync<OAuthException>(
            () => _handler.Handle(CreateRequest("profile"), TestContext.Current.CancellationToken));

        // Assert
        exception.Error.Error.ShouldBe(OAuthErrorCodes.InvalidScope);
    }

    [Fact]
    public async Task Handle_ScopeTheClientDoesNotOwn_ThrowsInvalidScope()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        var exception = await Should.ThrowAsync<DomainException>(
            () => _handler.Handle(CreateRequest("email"), TestContext.Current.CancellationToken));

        // Assert
        exception.Code.ShouldBe("invalid_scope");
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsTheGeneratedToken()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        var result = await _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        result.AccessToken.ShouldBe(GeneratedToken);
        result.TokenType.ShouldBe("Bearer");
        result.ExpiresIn.ShouldBe(1800);
    }

    [Fact]
    public async Task Handle_ValidRequest_UsesTheClientAsTokenSubject()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        await _handler.Handle(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        await _tokenGenerator.Received(1).GenerateAccessToken(
            Arg.Is<TokenGenerationRequest>(request =>
                request != null
                && request.Subject == ClientId
                && request.Issuer == "https://auth.example.com"
                && request.Lifetime == TimeSpan.FromSeconds(1800)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ResolvesTheAudienceFromTheGrantedScopes()
    {
        // Arrange
        GivenClient(CreateClient());

        // Act
        await _handler.Handle(CreateRequest(ApiScope), TestContext.Current.CancellationToken);

        // Assert
        await _apiResourceRepository.Received(1).GetAudiencesForScopes(
            Arg.Is<IEnumerable<string>>(scopes => scopes != null && scopes.SequenceEqual(new[] { ApiScope })),
            Arg.Any<CancellationToken>());

        await _tokenGenerator.Received(1).GenerateAccessToken(
            Arg.Is<TokenGenerationRequest>(request =>
                request != null && request.Audiences.SequenceEqual(new[] { ApiAudience })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ScopesOwnedByNoApi_IssuesATokenWithoutAudience()
    {
        // Arrange
        GivenClient(CreateClient());
        _apiResourceRepository
            .GetAudiencesForScopes(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        await _handler.Handle(CreateRequest("profile"), TestContext.Current.CancellationToken);

        // Assert: no API claims that scope, so the token must not name one.
        await _tokenGenerator.Received(1).GenerateAccessToken(
            Arg.Is<TokenGenerationRequest>(request => request != null && request.Audiences.Count == 0),
            Arg.Any<CancellationToken>());
    }
}
