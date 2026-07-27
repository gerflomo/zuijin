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
            .FirstOrDefaultAsync(g => g.CodeHash == codeHash, cancellationToken);
    }

    public async Task<bool> TryConsume(long grantId, CancellationToken cancellationToken = default)
    {
        // The IsUsed predicate lives in the UPDATE itself, so the database decides the winner
        // when two redemptions of the same code arrive at once.
        var affectedRows = await _context.AuthorizationGrants
            .Where(grant => grant.Id == grantId && !grant.IsUsed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(grant => grant.IsUsed, true), cancellationToken);

        return affectedRows == 1;
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
