using NSubstitute;
using Shouldly;
using Zuijin.Application.Abstractions;
using Zuijin.Application.Features.Authorization;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Application.Tests.Features.Authorization;

public class ConsentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid ClientId = Guid.CreateVersion7();

    private readonly IConsentRepository _consentRepository = Substitute.For<IConsentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConsentService _service;

    public ConsentServiceTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        _service = new ConsentService(_consentRepository, _unitOfWork, clock);
    }

    private void GivenStoredConsent(string? scopes, DateTimeOffset? expiresAt = null)
    {
        var consent = scopes is null
            ? null
            : new Consent { UserId = UserId, ClientId = ClientId, Scopes = scopes, ExpiresAt = expiresAt };

        _consentRepository.GetByUserAndClient(UserId, ClientId, Arg.Any<CancellationToken>()).Returns(consent);
    }

    [Fact]
    public async Task HasConsentFor_NoStoredConsent_ReturnsFalse()
    {
        // Arrange
        GivenStoredConsent(null);

        // Act
        var result = await _service.HasConsentFor(UserId, ClientId, ["openid"], TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasConsentFor_StoredConsentCoversEveryScope_ReturnsTrue()
    {
        // Arrange
        GivenStoredConsent("openid profile email");

        // Act
        var result = await _service.HasConsentFor(
            UserId, ClientId, ["openid", "profile"], TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HasConsentFor_RequestingAScopeNotYetConsented_ReturnsFalse()
    {
        // Arrange
        GivenStoredConsent("openid");

        // Act
        var result = await _service.HasConsentFor(
            UserId, ClientId, ["openid", "profile"], TestContext.Current.CancellationToken);

        // Assert: asking for anything new sends the user back to the consent screen.
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasConsentFor_ExpiredConsent_ReturnsFalse()
    {
        // Arrange
        GivenStoredConsent("openid profile", expiresAt: Now.AddMinutes(-1));

        // Act
        var result = await _service.HasConsentFor(UserId, ClientId, ["openid"], TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Grant_NoPreviousConsent_StoresTheScopes()
    {
        // Arrange
        GivenStoredConsent(null);

        // Act
        await _service.Grant(UserId, ClientId, ["profile", "openid"], TestContext.Current.CancellationToken);

        // Assert
        await _consentRepository.Received(1).Add(
            Arg.Is<Consent>(consent =>
                consent != null
                && consent.UserId == UserId
                && consent.ClientId == ClientId
                && consent.Scopes == "openid profile"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChanges(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grant_ExistingConsent_MergesInsteadOfReplacing()
    {
        // Arrange
        GivenStoredConsent("email");

        // Act
        await _service.Grant(UserId, ClientId, ["openid"], TestContext.Current.CancellationToken);

        // Assert: previously granted scopes must survive a narrower second request.
        await _consentRepository.Received(1).Update(
            Arg.Is<Consent>(consent => consent != null && consent.Scopes == "email openid"),
            Arg.Any<CancellationToken>());
    }
}
