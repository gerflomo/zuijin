using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly ZuijinDbContext _context;

    public ClientRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .Include(c => c.RedirectUris)
            .Include(c => c.GrantTypes)
            .Include(c => c.Scopes).ThenInclude(s => s.Scope)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Client?> GetByClientId(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .Include(c => c.RedirectUris)
            .Include(c => c.GrantTypes)
            .Include(c => c.Scopes).ThenInclude(s => s.Scope)
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .OrderBy(c => c.ClientName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCount(CancellationToken cancellationToken = default)
    {
        return await _context.Clients.CountAsync(cancellationToken);
    }

    public async Task Add(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
    }

    public Task Update(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        return Task.CompletedTask;
    }

    public Task Delete(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Remove(client);
        return Task.CompletedTask;
    }
}
