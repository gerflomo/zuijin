using System.Security.Cryptography;
using System.Text;
using Shouldly;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Services;

namespace Zuijin.Domain.Tests.Services;

public class AuthorizationValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    private static AuthorizationGrant CreateGrant()
    {
        return new AuthorizationGrant
        {
            CodeHash = "hash",
            ClientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RedirectUri = "https://app.example.com/callback",
            Scopes = "openid profile",
            IsUsed = false,
            ExpiresAt = Now.AddMinutes(5)
        };
    }

    private static string ComputeS256Challenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    [Fact]
    public void ValidateAuthorizationCode_ValidCode_DoesNotThrow()
    {
        // Arrange
        var grant = CreateGrant();

        // Act & Assert
        Should.NotThrow(() => AuthorizationValidator.ValidateAuthorizationCode(grant, Now));
    }

    [Fact]
    public void ValidateAuthorizationCode_AlreadyUsed_ThrowsCodeAlreadyUsed()
    {
        // Arrange
        var grant = CreateGrant();
        grant.IsUsed = true;

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateAuthorizationCode(grant, Now));

        // Assert
        exception.Code.ShouldBe("code_already_used");
    }

    [Fact]
    public void ValidateAuthorizationCode_Expired_ThrowsCodeExpired()
    {
        // Arrange
        var grant = CreateGrant();
        grant.ExpiresAt = Now.AddMinutes(-1);

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateAuthorizationCode(grant, Now));

        // Assert
        exception.Code.ShouldBe("code_expired");
    }

    [Fact]
    public void ValidateAuthorizationCode_ExpiresExactlyNow_ThrowsCodeExpired()
    {
        // Arrange
        var grant = CreateGrant();
        grant.ExpiresAt = Now;

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateAuthorizationCode(grant, Now));

        // Assert
        exception.Code.ShouldBe("code_expired");
    }

    [Fact]
    public void ValidateCodeChallenge_S256MatchingVerifier_DoesNotThrow()
    {
        // Arrange
        const string codeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var grant = CreateGrant();
        grant.CodeChallenge = ComputeS256Challenge(codeVerifier);
        grant.CodeChallengeMethod = "S256";

        // Act & Assert
        Should.NotThrow(() => AuthorizationValidator.ValidateCodeChallenge(grant, codeVerifier));
    }

    [Fact]
    public void ValidateCodeChallenge_S256WrongVerifier_ThrowsInvalidCodeVerifier()
    {
        // Arrange
        var grant = CreateGrant();
        grant.CodeChallenge = ComputeS256Challenge("the-original-verifier");
        grant.CodeChallengeMethod = "S256";

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateCodeChallenge(grant, "a-different-verifier"));

        // Assert
        exception.Code.ShouldBe("invalid_code_verifier");
    }

    [Fact]
    public void ValidateCodeChallenge_PlainMatchingVerifier_DoesNotThrow()
    {
        // Arrange
        var grant = CreateGrant();
        grant.CodeChallenge = "plain-verifier-value";
        grant.CodeChallengeMethod = "plain";

        // Act & Assert
        Should.NotThrow(() => AuthorizationValidator.ValidateCodeChallenge(grant, "plain-verifier-value"));
    }

    [Fact]
    public void ValidateCodeChallenge_PlainWrongVerifier_ThrowsInvalidCodeVerifier()
    {
        // Arrange
        var grant = CreateGrant();
        grant.CodeChallenge = "plain-verifier-value";
        grant.CodeChallengeMethod = "plain";

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateCodeChallenge(grant, "another-value"));

        // Assert
        exception.Code.ShouldBe("invalid_code_verifier");
    }

    [Fact]
    public void ValidateCodeChallenge_NoChallengeStored_DoesNotThrow()
    {
        // Arrange
        var grant = CreateGrant();
        grant.CodeChallenge = null;

        // Act & Assert
        Should.NotThrow(() => AuthorizationValidator.ValidateCodeChallenge(grant, string.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateCodeChallenge_ChallengeStoredButVerifierMissing_ThrowsPkceRequired(string? codeVerifier)
    {
        // Arrange
        var grant = CreateGrant();
        grant.CodeChallenge = ComputeS256Challenge("some-verifier");
        grant.CodeChallengeMethod = "S256";

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateCodeChallenge(grant, codeVerifier!));

        // Assert
        exception.Code.ShouldBe("pkce_required");
    }

    [Fact]
    public void ValidateCodeChallenge_UnsupportedMethod_ThrowsInvalidChallengeMethod()
    {
        // Arrange
        var grant = CreateGrant();
        grant.CodeChallenge = "challenge";
        grant.CodeChallengeMethod = "S512";

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateCodeChallenge(grant, "verifier"));

        // Assert
        exception.Code.ShouldBe("invalid_challenge_method");
    }

    [Fact]
    public void ValidateRedirectUri_MatchingUri_DoesNotThrow()
    {
        // Arrange
        var grant = CreateGrant();

        // Act & Assert
        Should.NotThrow(() => AuthorizationValidator.ValidateRedirectUri(grant, "https://app.example.com/callback"));
    }

    [Fact]
    public void ValidateRedirectUri_DifferentUri_ThrowsRedirectUriMismatch()
    {
        // Arrange
        var grant = CreateGrant();

        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateRedirectUri(grant, "https://app.example.com/other"));

        // Assert
        exception.Code.ShouldBe("redirect_uri_mismatch");
    }

    [Fact]
    public void ValidateResponseType_Code_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => AuthorizationValidator.ValidateResponseType("code"));
    }

    [Theory]
    [InlineData("token")]
    [InlineData("id_token")]
    [InlineData("code id_token")]
    public void ValidateResponseType_UnsupportedType_ThrowsUnsupportedResponseType(string responseType)
    {
        // Act
        var exception = Should.Throw<DomainException>(
            () => AuthorizationValidator.ValidateResponseType(responseType));

        // Assert
        exception.Code.ShouldBe("unsupported_response_type");
    }
}
