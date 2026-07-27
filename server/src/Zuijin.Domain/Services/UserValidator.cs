using Zuijin.Domain.Entities;
using Zuijin.Domain.Errors;

namespace Zuijin.Domain.Services;

/// <summary>
/// Rules governing whether a user may authenticate, and how failed attempts are accounted for.
/// </summary>
public static class UserValidator
{
    public static void ValidateCanAuthenticate(User user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsActive)
        {
            throw new DomainException("user_disabled", "The user account is disabled.");
        }

        if (IsLockedOut(user, now))
        {
            throw new DomainException("user_locked_out", "The user account is temporarily locked.");
        }
    }

    public static bool IsLockedOut(User user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.IsLockedOut && (user.LockoutEnd is null || user.LockoutEnd > now);
    }

    /// <summary>
    /// Records a failed sign-in and locks the account once the threshold is reached.
    /// </summary>
    public static void RegisterFailedLogin(User user, DateTimeOffset now, int maxAttempts, int lockoutMinutes)
    {
        ArgumentNullException.ThrowIfNull(user);

        // A lockout that already elapsed starts the count over rather than compounding.
        if (user.IsLockedOut && user.LockoutEnd is not null && user.LockoutEnd <= now)
        {
            user.IsLockedOut = false;
            user.LockoutEnd = null;
            user.FailedLoginCount = 0;
        }

        user.FailedLoginCount++;

        if (user.FailedLoginCount >= maxAttempts)
        {
            user.IsLockedOut = true;
            user.LockoutEnd = now.AddMinutes(lockoutMinutes);
        }
    }

    public static void RegisterSuccessfulLogin(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.FailedLoginCount = 0;
        user.IsLockedOut = false;
        user.LockoutEnd = null;
    }
}
