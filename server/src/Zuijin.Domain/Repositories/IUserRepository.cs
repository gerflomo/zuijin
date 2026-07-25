using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for managing identity users.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetBySubjectId(string subjectId, CancellationToken cancellationToken = default);
    Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmail(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCount(CancellationToken cancellationToken = default);
    Task Add(User user, CancellationToken cancellationToken = default);
    Task Update(User user, CancellationToken cancellationToken = default);
    Task Delete(User user, CancellationToken cancellationToken = default);
}
