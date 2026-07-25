using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing user consent records for OAuth 2.0 clients.
/// </summary>
public interface IConsentRepository
{
    Task<Consent?> GetByUserAndClient(Guid userId, Guid clientId, CancellationToken cancellationToken = default);
    Task Add(Consent consent, CancellationToken cancellationToken = default);
    Task Update(Consent consent, CancellationToken cancellationToken = default);
    Task Delete(Consent consent, CancellationToken cancellationToken = default);
    Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
