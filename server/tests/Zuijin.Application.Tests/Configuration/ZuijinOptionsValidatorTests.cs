using Shouldly;
using Zuijin.Application.Configuration;

namespace Zuijin.Application.Tests.Configuration;

public class ZuijinOptionsValidatorTests
{
    private static ZuijinOptions CreateValidOptions()
    {
        return new ZuijinOptions
        {
            Issuer = "https://auth.example.com",
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
        errors.Count.ShouldBe(12);
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
