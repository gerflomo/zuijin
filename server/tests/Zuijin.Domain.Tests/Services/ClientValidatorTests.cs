using Shouldly;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Enums;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Services;

namespace Zuijin.Domain.Tests.Services;

public class ClientValidatorTests
{
    private const string RegisteredRedirectUri = "https://app.example.com/callback";

    private static Client CreateClient()
    {
        return new Client
        {
            Id = Guid.NewGuid(),
            ClientId = "test-client",
            ClientName = "Test Client",
            Type = ClientType.Confidential,
            IsActive = true,
            RedirectUris =
            [
                new ClientRedirectUri { Uri = RegisteredRedirectUri, Type = RedirectUriType.Redirect },
                new ClientRedirectUri { Uri = "https://app.example.com/logged-out", Type = RedirectUriType.PostLogout }
            ],
            GrantTypes = [new ClientGrantType { GrantType = "authorization_code" }],
            Scopes =
            [
                new ClientScope { Scope = new Scope { Name = "openid" } },
                new ClientScope { Scope = new Scope { Name = "profile" } }
            ]
        };
    }

    [Fact]
    public void ValidateRedirectUri_RegisteredUri_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        Should.NotThrow(() => ClientValidator.ValidateRedirectUri(client, RegisteredRedirectUri));
    }

    [Fact]
    public void ValidateRedirectUri_UnregisteredUri_ThrowsInvalidRedirectUri()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<DomainException>(
            () => ClientValidator.ValidateRedirectUri(client, "https://evil.example.com/callback"));

        // Assert
        exception.Code.ShouldBe("invalid_redirect_uri");
    }

    [Fact]
    public void ValidateRedirectUri_PostLogoutUriUsedAsRedirect_ThrowsInvalidRedirectUri()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<DomainException>(
            () => ClientValidator.ValidateRedirectUri(client, "https://app.example.com/logged-out"));

        // Assert
        exception.Code.ShouldBe("invalid_redirect_uri");
    }

    [Fact]
    public void ValidateGrantType_AuthorizedGrant_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        Should.NotThrow(() => ClientValidator.ValidateGrantType(client, "authorization_code"));
    }

    [Fact]
    public void ValidateGrantType_UnauthorizedGrant_ThrowsUnauthorizedGrantType()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<DomainException>(
            () => ClientValidator.ValidateGrantType(client, "client_credentials"));

        // Assert
        exception.Code.ShouldBe("unauthorized_grant_type");
    }

    [Fact]
    public void ValidateScopes_AllScopesAllowed_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        Should.NotThrow(() => ClientValidator.ValidateScopes(client, ["openid", "profile"]));
    }

    [Fact]
    public void ValidateScopes_UnknownScope_ThrowsInvalidScope()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var exception = Should.Throw<DomainException>(
            () => ClientValidator.ValidateScopes(client, ["openid", "admin"]));

        // Assert
        exception.Code.ShouldBe("invalid_scope");
    }

    [Fact]
    public void ValidateActive_ActiveClient_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();

        // Act & Assert
        Should.NotThrow(() => ClientValidator.ValidateActive(client));
    }

    [Fact]
    public void ValidateActive_DisabledClient_ThrowsClientDisabled()
    {
        // Arrange
        var client = CreateClient();
        client.IsActive = false;

        // Act
        var exception = Should.Throw<DomainException>(() => ClientValidator.ValidateActive(client));

        // Assert
        exception.Code.ShouldBe("client_disabled");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidatePkceRequired_RequiredAndChallengeMissing_ThrowsPkceRequired(string? codeChallenge)
    {
        // Arrange
        var client = CreateClient();
        client.RequirePkce = true;

        // Act
        var exception = Should.Throw<DomainException>(
            () => ClientValidator.ValidatePkceRequired(client, codeChallenge));

        // Assert
        exception.Code.ShouldBe("pkce_required");
    }

    [Fact]
    public void ValidatePkceRequired_RequiredAndChallengeProvided_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();
        client.RequirePkce = true;

        // Act & Assert
        Should.NotThrow(() => ClientValidator.ValidatePkceRequired(client, "challenge-value"));
    }

    [Fact]
    public void ValidatePkceRequired_NotRequiredAndChallengeMissing_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();
        client.RequirePkce = false;

        // Act & Assert
        Should.NotThrow(() => ClientValidator.ValidatePkceRequired(client, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateConfidentialClient_ConfidentialWithoutSecret_ThrowsClientSecretRequired(string? clientSecret)
    {
        // Arrange
        var client = CreateClient();
        client.Type = ClientType.Confidential;

        // Act
        var exception = Should.Throw<DomainException>(
            () => ClientValidator.ValidateConfidentialClient(client, clientSecret));

        // Assert
        exception.Code.ShouldBe("client_secret_required");
    }

    [Fact]
    public void ValidateConfidentialClient_PublicWithoutSecret_DoesNotThrow()
    {
        // Arrange
        var client = CreateClient();
        client.Type = ClientType.Public;

        // Act & Assert
        Should.NotThrow(() => ClientValidator.ValidateConfidentialClient(client, null));
    }
}
