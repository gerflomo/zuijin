using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing OAuth 2.0 authorization code grants.
/// </summary>
public interface IAuthorizationGrantRepository
{
    Task<AuthorizationGrant?> GetByCodeHash(string codeHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically flips the grant to used, returning false if it was already consumed.
    /// A single conditional update is what makes an authorization code truly single-use:
    /// two concurrent redemptions cannot both succeed.
    /// </summary>
    Task<bool> TryConsume(long grantId, CancellationToken cancellationToken = default);

    Task Add(AuthorizationGrant grant, CancellationToken cancellationToken = default);
    Task Update(AuthorizationGrant grant, CancellationToken cancellationToken = default);
    Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
