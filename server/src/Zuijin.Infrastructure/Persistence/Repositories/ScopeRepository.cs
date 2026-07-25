using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class ScopeRepository : IScopeRepository
{
    private readonly ZuijinDbContext _context;

    public ScopeRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<Scope?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .AsNoTracking()
            .Include(s => s.Claims)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Scope?> GetByName(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .AsNoTracking()
            .Include(s => s.Claims)
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Scope>> GetByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var nameList = names.ToList();
        return await _context.Scopes
            .AsNoTracking()
            .Include(s => s.Claims)
            .Where(s => nameList.Contains(s.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Scope>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Scopes
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCount(CancellationToken cancellationToken = default)
    {
        return await _context.Scopes.CountAsync(cancellationToken);
    }

    public async Task Add(Scope scope, CancellationToken cancellationToken = default)
    {
        await _context.Scopes.AddAsync(scope, cancellationToken);
    }

    public Task Update(Scope scope, CancellationToken cancellationToken = default)
    {
        _context.Scopes.Update(scope);
        return Task.CompletedTask;
    }

    public Task Delete(Scope scope, CancellationToken cancellationToken = default)
    {
        _context.Scopes.Remove(scope);
        return Task.CompletedTask;
    }
}
