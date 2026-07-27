using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class ApiResourceRepository : IApiResourceRepository
{
    private readonly ZuijinDbContext _context;

    public ApiResourceRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResource?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApiResources
            .AsNoTracking()
            .Include(resource => resource.Scopes).ThenInclude(scope => scope.Scope)
            .FirstOrDefaultAsync(resource => resource.Id == id, cancellationToken);
    }

    public async Task<ApiResource?> GetByName(string name, CancellationToken cancellationToken = default)
    {
        return await _context.ApiResources
            .AsNoTracking()
            .Include(resource => resource.Scopes).ThenInclude(scope => scope.Scope)
            .FirstOrDefaultAsync(resource => resource.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<ApiResource>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.ApiResources
            .AsNoTracking()
            .OrderBy(resource => resource.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCount(CancellationToken cancellationToken = default)
    {
        return await _context.ApiResources.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAudiencesForScopes(
        IEnumerable<string> scopeNames,
        CancellationToken cancellationToken = default)
    {
        var names = scopeNames as IReadOnlyList<string> ?? scopeNames.ToList();

        if (names.Count == 0)
        {
            return [];
        }

        return await _context.ApiResourceScopes
            .AsNoTracking()
            .Where(link => link.ApiResource.IsActive
                        && link.Scope.IsActive
                        && names.Contains(link.Scope.Name))
            .Select(link => link.ApiResource.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    public async Task Add(ApiResource apiResource, CancellationToken cancellationToken = default)
    {
        await _context.ApiResources.AddAsync(apiResource, cancellationToken);
    }

    public Task Update(ApiResource apiResource, CancellationToken cancellationToken = default)
    {
        _context.ApiResources.Update(apiResource);
        return Task.CompletedTask;
    }

    public Task Delete(ApiResource apiResource, CancellationToken cancellationToken = default)
    {
        apiResource.IsDeleted = true;
        _context.ApiResources.Update(apiResource);
        return Task.CompletedTask;
    }
}
