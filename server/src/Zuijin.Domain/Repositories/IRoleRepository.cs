using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing RBAC roles.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByName(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetByUserId(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetCount(CancellationToken cancellationToken = default);
    Task Add(Role role, CancellationToken cancellationToken = default);
    Task Update(Role role, CancellationToken cancellationToken = default);
    Task Delete(Role role, CancellationToken cancellationToken = default);
}
