using System.Security.Cryptography;
using Shouldly;
using Zuijin.Application.Configuration;

namespace Zuijin.Application.Tests.Configuration;

public class ZuijinOptionsValidatorTests
{
    private static readonly string ValidMasterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static ZuijinOptions CreateValidOptions()
    {
        return new ZuijinOptions
        {
            Issuer = "https://auth.example.com",
            SigningKeyMasterKey = ValidMasterKey,
            RequirePkce = true,
            RequireHttpsRedirectUris = true,
            DefaultAccessTokenLifetime = 3600,
            DefaultRefreshTokenLifetime = 2592000,
            DefaultIdTokenLifetime = 3600,
            AuthorizationCodeLifetime = 300,
            DeviceCodeLifetime = 300,
            DeviceCodePollingInterval = 5,
            KeyRotationIntervalDays = 90,
            MaxFailedLoginAttempts = 5,
            LockoutDurationMinutes = 15
        };
    }

    [Fact]
    public void Validate_AllValuesPresent_ReturnsNoErrors()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_EmptyOptions_ReturnsOneErrorPerKey()
    {
        // Arrange
        var options = new ZuijinOptions();

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.Count.ShouldBe(13);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingMasterKey_ReturnsError(string? masterKey)
    {
        // Arrange
        var options = CreateValidOptions();
        options.SigningKeyMasterKey = masterKey;

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("Zuijin:SigningKeyMasterKey") && e.Contains("not configured"));
    }

    [Fact]
    public void Validate_MasterKeyNotBase64_ReturnsFormatError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SigningKeyMasterKey = "this is not base64 !!";

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("valid base64"));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(64)]
    public void Validate_MasterKeyWithWrongLength_ReturnsSizeError(int keySizeBytes)
    {
        // Arrange
        var options = CreateValidOptions();
        options.SigningKeyMasterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(keySizeBytes));

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("exactly 32 bytes"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingIssuer_ReturnsError(string? issuer)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Issuer = issuer;

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("Zuijin:Issuer"));
    }

    [Fact]
    public void Validate_RelativeIssuer_ReturnsError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Issuer = "not-a-uri";

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("absolute URI"));
    }

    [Fact]
    public void Validate_HttpIssuerOnPublicHost_ReturnsError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Issuer = "http://auth.example.com";

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("HTTPS"));
    }

    [Theory]
    [InlineData("http://localhost:5190")]
    [InlineData("http://127.0.0.1:5190")]
    [InlineData("https://localhost:7295")]
    public void Validate_LoopbackIssuer_IsAllowed(string issuer)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Issuer = issuer;

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_MissingBooleanFlag_ReturnsError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.RequirePkce = null;

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("Zuijin:RequirePkce"));
    }

    [Fact]
    public void Validate_MissingLifetime_ReturnsError()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultAccessTokenLifetime = null;

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("Zuijin:DefaultAccessTokenLifetime"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveLifetime_ReturnsError(int lifetime)
    {
        // Arrange
        var options = CreateValidOptions();
        options.AuthorizationCodeLifetime = lifetime;

        // Act
        var errors = ZuijinOptionsValidator.Validate(options);

        // Assert
        errors.ShouldContain(e => e.Contains("Zuijin:AuthorizationCodeLifetime") && e.Contains("greater than zero"));
    }
}
