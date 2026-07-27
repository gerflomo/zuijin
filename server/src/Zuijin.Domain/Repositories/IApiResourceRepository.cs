using Zuijin.Domain.Entities;

namespace Zuijin.Domain.Repositories;

/// <summary>
/// Repository for the API resources that access tokens can be audienced to.
/// </summary>
public interface IApiResourceRepository
{
    Task<ApiResource?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResource?> GetByName(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiResource>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCount(CancellationToken cancellationToken = default);

    /// <summary>
    /// Names of the active resources that own any of the given scopes.
    /// This is what the token endpoint turns into the audience claim.
    /// </summary>
    Task<IReadOnlyList<string>> GetAudiencesForScopes(
        IEnumerable<string> scopeNames,
        CancellationToken cancellationToken = default);

    Task Add(ApiResource apiResource, CancellationToken cancellationToken = default);
    Task Update(ApiResource apiResource, CancellationToken cancellationToken = default);
    Task Delete(ApiResource apiResource, CancellationToken cancellationToken = default);
}
