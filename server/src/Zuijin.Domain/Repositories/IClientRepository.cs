using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing OAuth 2.0 client registrations.
/// </summary>
public interface IClientRepository
{
    Task<Client?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Client?> GetByClientId(string clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCount(CancellationToken cancellationToken = default);
    Task Add(Client client, CancellationToken cancellationToken = default);
    Task Update(Client client, CancellationToken cancellationToken = default);
    Task Delete(Client client, CancellationToken cancellationToken = default);
}
