using Microsoft.EntityFrameworkCore;
using Zuijin.Domain.Entities;
using Zuijin.Domain.Repositories;

namespace Zuijin.Infrastructure.Persistence.Repositories;

public class AuthorizationGrantRepository : IAuthorizationGrantRepository
{
    private readonly ZuijinDbContext _context;

    public AuthorizationGrantRepository(ZuijinDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorizationGrant?> GetByCodeHash(string codeHash, CancellationToken cancellationToken = default)
    {
        return await _context.AuthorizationGrants
            .AsNoTracking()
            .Include(g => g.Client)
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.AuthorizationCode == codeHash, cancellationToken);
    }

    public async Task Add(AuthorizationGrant grant, CancellationToken cancellationToken = default)
    {
        await _context.AuthorizationGrants.AddAsync(grant, cancellationToken);
    }

    public Task Update(AuthorizationGrant grant, CancellationToken cancellationToken = default)
    {
        _context.AuthorizationGrants.Update(grant);
        return Task.CompletedTask;
    }

    public async Task DeleteExpired(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        await _context.AuthorizationGrants
            .Where(g => g.ExpiresAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
