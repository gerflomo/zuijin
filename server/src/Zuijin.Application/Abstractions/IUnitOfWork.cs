namespace Zuijin.Application.Abstractions;

/// <summary>
/// Coordinates transactional persistence across multiple repositories.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChanges(CancellationToken cancellationToken = default);
}
