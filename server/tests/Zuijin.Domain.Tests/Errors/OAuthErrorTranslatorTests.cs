using Shouldly;
using Zuijin.Domain.Errors;

namespace Zuijin.Domain.Tests.Errors;

public class OAuthErrorTranslatorTests
{
    [Theory]
    [InlineData("client_disabled", OAuthErrorCodes.InvalidClient)]
    [InlineData("client_secret_required", OAuthErrorCodes.InvalidClient)]
    [InlineData("unauthorized_grant_type", OAuthErrorCodes.UnauthorizedClient)]
    [InlineData("invalid_scope", OAuthErrorCodes.InvalidScope)]
    [InlineData("code_already_used", OAuthErrorCodes.InvalidGrant)]
    [InlineData("code_expired", OAuthErrorCodes.InvalidGrant)]
    [InlineData("invalid_code_verifier", OAuthErrorCodes.InvalidGrant)]
    [InlineData("redirect_uri_mismatch", OAuthErrorCodes.InvalidGrant)]
    [InlineData("invalid_redirect_uri", OAuthErrorCodes.InvalidRequest)]
    [InlineData("pkce_required", OAuthErrorCodes.InvalidRequest)]
    [InlineData("invalid_challenge_method", OAuthErrorCodes.InvalidRequest)]
    [InlineData("unsupported_response_type", OAuthErrorCodes.UnsupportedResponseType)]
    public void Translate_KnownDomainCode_MapsToTheProtocolCode(string domainCode, string expectedError)
    {
        // Arrange
        var exception = new DomainException(domainCode, "Something went wrong.");

        // Act
        var error = OAuthErrorTranslator.Translate(exception);

        // Assert
        error.Error.ShouldBe(expectedError);
    }

    [Fact]
    public void Translate_KnownDomainCode_KeepsTheDescription()
    {
        // Arrange
        var exception = new DomainException("invalid_scope", "The client is not authorized for scope 'email'.");

        // Act
        var error = OAuthErrorTranslator.Translate(exception);

        // Assert
        error.ErrorDescription.ShouldBe("The client is not authorized for scope 'email'.");
    }

    [Fact]
    public void Translate_UnknownDomainCode_FallsBackToServerError()
    {
        // Arrange
        var exception = new DomainException("something_unmapped", "Internal detail that must not leak.");

        // Act
        var error = OAuthErrorTranslator.Translate(exception);

        // Assert: unmapped failures must not expose internal wording to the client.
        error.Error.ShouldBe(OAuthErrorCodes.ServerError);
        error.ErrorDescription.ShouldBeNull();
    }
}
