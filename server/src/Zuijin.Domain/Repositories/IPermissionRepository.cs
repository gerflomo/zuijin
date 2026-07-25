using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing RBAC permissions.
/// </summary>
public interface IPermissionRepository
{
    Task<Permission?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Permission?> GetByName(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetByRoleId(Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetByUserId(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetCount(CancellationToken cancellationToken = default);
    Task Add(Permission permission, CancellationToken cancellationToken = default);
    Task Update(Permission permission, CancellationToken cancellationToken = default);
    Task Delete(Permission permission, CancellationToken cancellationToken = default);
}
