using Shouldly;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Services;

namespace Zuijin.Domain.Tests.Services;

public class UserValidatorTests
{
    private const int MaxAttempts = 3;
    private const int LockoutMinutes = 15;
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.CreateVersion7(),
            SubjectId = "subject",
            Username = "user",
            Email = "user@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
    }

    [Fact]
    public void ValidateCanAuthenticate_ActiveUser_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => UserValidator.ValidateCanAuthenticate(CreateUser(), Now));
    }

    [Fact]
    public void ValidateCanAuthenticate_DisabledUser_ThrowsUserDisabled()
    {
        // Arrange
        var user = CreateUser();
        user.IsActive = false;

        // Act
        var exception = Should.Throw<DomainException>(() => UserValidator.ValidateCanAuthenticate(user, Now));

        // Assert
        exception.Code.ShouldBe("user_disabled");
    }

    [Fact]
    public void ValidateCanAuthenticate_LockedOutUser_ThrowsUserLockedOut()
    {
        // Arrange
        var user = CreateUser();
        user.IsLockedOut = true;
        user.LockoutEnd = Now.AddMinutes(5);

        // Act
        var exception = Should.Throw<DomainException>(() => UserValidator.ValidateCanAuthenticate(user, Now));

        // Assert
        exception.Code.ShouldBe("user_locked_out");
    }

    [Fact]
    public void IsLockedOut_LockoutWindowHasElapsed_ReturnsFalse()
    {
        // Arrange
        var user = CreateUser();
        user.IsLockedOut = true;
        user.LockoutEnd = Now.AddMinutes(-1);

        // Act & Assert: the lock expires on its own, without an administrator.
        UserValidator.IsLockedOut(user, Now).ShouldBeFalse();
    }

    [Fact]
    public void IsLockedOut_LockedWithNoEndDate_ReturnsTrue()
    {
        // Arrange: a lock with no end is an indefinite one.
        var user = CreateUser();
        user.IsLockedOut = true;
        user.LockoutEnd = null;

        // Act & Assert
        UserValidator.IsLockedOut(user, Now).ShouldBeTrue();
    }

    [Fact]
    public void RegisterFailedLogin_BelowTheThreshold_CountsWithoutLocking()
    {
        // Arrange
        var user = CreateUser();

        // Act
        UserValidator.RegisterFailedLogin(user, Now, MaxAttempts, LockoutMinutes);
        UserValidator.RegisterFailedLogin(user, Now, MaxAttempts, LockoutMinutes);

        // Assert
        user.FailedLoginCount.ShouldBe(2);
        user.IsLockedOut.ShouldBeFalse();
    }

    [Fact]
    public void RegisterFailedLogin_ReachingTheThreshold_LocksTheAccount()
    {
        // Arrange
        var user = CreateUser();

        // Act
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            UserValidator.RegisterFailedLogin(user, Now, MaxAttempts, LockoutMinutes);
        }

        // Assert
        user.IsLockedOut.ShouldBeTrue();
        user.LockoutEnd.ShouldBe(Now.AddMinutes(LockoutMinutes));
    }

    [Fact]
    public void RegisterFailedLogin_AfterAnElapsedLockout_StartsCountingAgain()
    {
        // Arrange: a previous lockout that already ran out.
        var user = CreateUser();
        user.IsLockedOut = true;
        user.LockoutEnd = Now.AddMinutes(-1);
        user.FailedLoginCount = MaxAttempts;

        // Act
        UserValidator.RegisterFailedLogin(user, Now, MaxAttempts, LockoutMinutes);

        // Assert: the count restarts instead of locking again on the first miss.
        user.FailedLoginCount.ShouldBe(1);
        user.IsLockedOut.ShouldBeFalse();
    }

    [Fact]
    public void RegisterSuccessfulLogin_ClearsTheFailureState()
    {
        // Arrange
        var user = CreateUser();
        user.FailedLoginCount = 2;
        user.IsLockedOut = true;
        user.LockoutEnd = Now.AddMinutes(5);

        // Act
        UserValidator.RegisterSuccessfulLogin(user);

        // Assert
        user.FailedLoginCount.ShouldBe(0);
        user.IsLockedOut.ShouldBeFalse();
        user.LockoutEnd.ShouldBeNull();
    }
}
