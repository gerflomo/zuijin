using Zuijin.Application.Abstractions;
using Zuijin.Application.Configuration;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Errors;
using Zuijin.Domain.Repositories;
using Zuijin.Domain.Services;

namespace Zuijin.Application.Features.Users;

/// <summary>
/// Verifies a username and password, maintaining the lockout counters that blunt
/// online guessing attacks.
/// </summary>
public sealed class UserAuthenticator
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ZuijinOptions _options;

    public UserAuthenticator(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        ZuijinOptions options)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options;
    }

    /// <summary>
    /// Returns the authenticated user, or null when the credentials are not valid.
    /// The caller must not tell the end user which half was wrong.
    /// </summary>
    public async Task<User?> Authenticate(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var user = await _userRepository.GetByUsername(username, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var now = _clock.UtcNow;

        // A locked or disabled account is rejected before the password is even considered.
        if (!user.IsActive || UserValidator.IsLockedOut(user, now))
        {
            return null;
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            UserValidator.RegisterFailedLogin(
                user,
                now,
                _options.MaxFailedLoginAttempts!.Value,
                _options.LockoutDurationMinutes!.Value);

            await Persist(user, cancellationToken);
            return null;
        }

        // Cost parameters may have been raised since this hash was written.
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.HashPassword(password);
        }

        UserValidator.RegisterSuccessfulLogin(user);
        await Persist(user, cancellationToken);

        return user;
    }

    private async Task Persist(User user, CancellationToken cancellationToken)
    {
        await _userRepository.Update(user, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
    }
}
