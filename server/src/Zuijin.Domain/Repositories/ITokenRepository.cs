using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing OAuth 2.0 access and refresh tokens.
/// </summary>
public interface ITokenRepository
{
    Task<Token?> GetByHash(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Token>> GetByUserId(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Token>> GetByClientId(Guid clientId, CancellationToken cancellationToken = default);
    Task Add(Token token, CancellationToken cancellationToken = default);
    Task Update(Token token, CancellationToken cancellationToken = default);
    Task RevokeByUserId(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeByClientId(Guid clientId, CancellationToken cancellationToken = default);
    Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
