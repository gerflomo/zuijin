using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing OAuth 2.0 / OIDC scopes and their associated claims.
/// </summary>
public interface IScopeRepository
{
    Task<Scope?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Scope?> GetByName(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Scope>> GetByNames(IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Scope>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCount(CancellationToken cancellationToken = default);
    Task Add(Scope scope, CancellationToken cancellationToken = default);
    Task Update(Scope scope, CancellationToken cancellationToken = default);
    Task Delete(Scope scope, CancellationToken cancellationToken = default);
}
